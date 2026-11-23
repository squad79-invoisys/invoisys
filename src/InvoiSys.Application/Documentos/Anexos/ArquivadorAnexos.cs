using System.Security.Cryptography;
using InvoiSys.Application.Common.Anexos;
using InvoiSys.Domain.Entities;
using InvoiSys.Domain.Repositories;
using Microsoft.Extensions.Logging;

namespace InvoiSys.Application.Documentos.Anexos;

/// <summary>
/// Entrega o arquivo de um anexo, preferindo a cópia própria quando ela existe.
/// No primeiro acesso o arquivo é copiado da origem para o armazenamento, de
/// modo que os acessos seguintes não dependam mais do site externo.
/// </summary>
public sealed class ArquivadorAnexos(
    IAnexoDownloader downloader,
    IArmazenamentoAnexos armazenamento,
    IAnexoArquivadoRepository repositorio,
    ILogger<ArquivadorAnexos> logger)
{
    public const long TamanhoMaximoArquivamentoBytes = 20L * 1024 * 1024;

    private const string Prefixo = "anexos/";

    public static string ChaveDe(string hashConteudo) => $"{Prefixo}{hashConteudo}";

    public async Task<ArquivoAnexo> ObterAsync(
        string url,
        string nomeSugerido,
        CancellationToken cancellationToken)
    {
        var arquivado = await TentarServirDoArmazenamentoAsync(url, cancellationToken);
        if (arquivado is not null)
            return arquivado;

        var daOrigem = await downloader.BaixarAsync(url, nomeSugerido, cancellationToken);

        if (!PodeArquivar(daOrigem))
            return daOrigem;

        try
        {
            return await ArquivarAsync(url, daOrigem, cancellationToken);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            // Arquivar é melhoria, não requisito: se falhar, o usuário ainda
            // precisa receber o arquivo.
            logger.LogWarning(exception, "Não foi possível arquivar o anexo {Url}.", url);
            await daOrigem.DisposeAsync();

            return await downloader.BaixarAsync(url, nomeSugerido, cancellationToken);
        }
    }

    private async Task<ArquivoAnexo?> TentarServirDoArmazenamentoAsync(
        string url,
        CancellationToken cancellationToken)
    {
        if (!armazenamento.Habilitado)
            return null;

        var registro = await repositorio.ObterPorUrlAsync(url, cancellationToken);
        if (registro is null)
            return null;

        var conteudo = await armazenamento.AbrirAsync(registro.ChaveObjeto, cancellationToken);
        if (conteudo is null)
            return null;

        return new ArquivoAnexo(
            conteudo, registro.TipoConteudo, registro.NomeArquivo, registro.Tamanho);
    }

    // Só arquiva o que a origem declarou o tamanho e cabe no limite: sem isso
    // seria preciso consumir o fluxo para descobrir, e aí não haveria como
    // devolvê-lo ao usuário se passasse do teto.
    private bool PodeArquivar(ArquivoAnexo arquivo) =>
        armazenamento.Habilitado &&
        arquivo.Tamanho is > 0 and <= TamanhoMaximoArquivamentoBytes;

    private async Task<ArquivoAnexo> ArquivarAsync(
        string url,
        ArquivoAnexo daOrigem,
        CancellationToken cancellationToken)
    {
        // O arquivo passa por disco para que o envio tenha tamanho conhecido e
        // para que o mesmo conteúdo seja devolvido ao usuário sem baixar duas vezes.
        var temporario = new FileStream(
            Path.Combine(Path.GetTempPath(), $"invoisys-anexo-{Guid.NewGuid():N}"),
            FileMode.CreateNew,
            FileAccess.ReadWrite,
            FileShare.None,
            bufferSize: 81920,
            FileOptions.DeleteOnClose | FileOptions.Asynchronous);

        try
        {
            string hash;
            await using (daOrigem)
            {
                await daOrigem.Conteudo.CopyToAsync(temporario, cancellationToken);
                temporario.Position = 0;
                hash = Convert.ToHexString(
                    await SHA256.HashDataAsync(temporario, cancellationToken)).ToLowerInvariant();
            }

            var chave = ChaveDe(hash);
            temporario.Position = 0;

            if (!await armazenamento.ExisteAsync(chave, cancellationToken))
            {
                await armazenamento.GravarAsync(
                    chave, temporario, daOrigem.TipoConteudo, cancellationToken);
                temporario.Position = 0;
            }

            var registro = new AnexoArquivado(
                url, hash, chave, daOrigem.TipoConteudo, daOrigem.NomeArquivo, temporario.Length);

            if (await repositorio.AdicionarAsync(registro, cancellationToken))
                logger.LogInformation(
                    "Anexo arquivado: {Url} em {Chave} ({Tamanho} bytes).",
                    url, chave, temporario.Length);

            return new ArquivoAnexo(
                temporario, daOrigem.TipoConteudo, daOrigem.NomeArquivo, temporario.Length);
        }
        catch
        {
            await temporario.DisposeAsync();
            throw;
        }
    }
}
