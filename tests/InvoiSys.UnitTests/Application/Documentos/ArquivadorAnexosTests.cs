using System.Security.Cryptography;
using System.Text;
using FluentAssertions;
using InvoiSys.Application.Common.Anexos;
using InvoiSys.Application.Documentos.Anexos;
using InvoiSys.Domain.Entities;
using InvoiSys.Domain.Repositories;
using Microsoft.Extensions.Logging.Abstractions;

namespace InvoiSys.UnitTests.Application.Documentos;

public sealed class ArquivadorAnexosTests
{
    private const string Url = "https://www.gov.br/termo.pdf";
    private static readonly byte[] Conteudo = Encoding.ASCII.GetBytes("%PDF-conteudo");

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    [Fact]
    public async Task ObterAsync_DeveBaixarDaOrigem_QuandoArmazenamentoDesligado()
    {
        var downloader = new DownloaderFake();
        var armazenamento = new ArmazenamentoFake { Habilitado = false };
        var arquivador = Criar(downloader, armazenamento, new RepositorioFake());

        await using var arquivo = await arquivador.ObterAsync(Url, "termo.pdf", Ct);

        downloader.Chamadas.Should().Be(1);
        armazenamento.Objetos.Should().BeEmpty();
        arquivo.NomeArquivo.Should().Be("termo.pdf");
    }

    [Fact]
    public async Task ObterAsync_DeveArquivarNoPrimeiroAcesso()
    {
        var downloader = new DownloaderFake();
        var armazenamento = new ArmazenamentoFake();
        var repositorio = new RepositorioFake();
        var arquivador = Criar(downloader, armazenamento, repositorio);

        await using var arquivo = await arquivador.ObterAsync(Url, "termo.pdf", Ct);

        var hash = Convert.ToHexString(SHA256.HashData(Conteudo)).ToLowerInvariant();
        armazenamento.Objetos.Should().ContainKey(ArquivadorAnexos.ChaveDe(hash));
        repositorio.Gravados.Should().ContainSingle()
            .Which.Url.Should().Be(Url);

        using var memoria = new MemoryStream();
        await arquivo.Conteudo.CopyToAsync(memoria, Ct);
        memoria.ToArray().Should().Equal(Conteudo);
    }

    [Fact]
    public async Task ObterAsync_DeveServirDoArmazenamento_SemTocarNaOrigem()
    {
        var hash = Convert.ToHexString(SHA256.HashData(Conteudo)).ToLowerInvariant();
        var chave = ArquivadorAnexos.ChaveDe(hash);
        var armazenamento = new ArmazenamentoFake();
        armazenamento.Objetos[chave] = Conteudo;

        var repositorio = new RepositorioFake();
        repositorio.Gravados.Add(new AnexoArquivado(
            Url, hash, chave, "application/pdf", "termo.pdf", Conteudo.Length));

        var downloader = new DownloaderFake();
        var arquivador = Criar(downloader, armazenamento, repositorio);

        await using var arquivo = await arquivador.ObterAsync(Url, "termo.pdf", Ct);

        downloader.Chamadas.Should().Be(0);
        arquivo.Tamanho.Should().Be(Conteudo.Length);
    }

    [Fact]
    public async Task ObterAsync_NaoDeveArquivar_QuandoAOrigemNaoInformaTamanho()
    {
        var downloader = new DownloaderFake { InformarTamanho = false };
        var armazenamento = new ArmazenamentoFake();
        var arquivador = Criar(downloader, armazenamento, new RepositorioFake());

        await using var arquivo = await arquivador.ObterAsync(Url, "termo.pdf", Ct);

        armazenamento.Objetos.Should().BeEmpty();
    }

    [Fact]
    public async Task ObterAsync_NaoDeveArquivar_QuandoPassaDoTamanhoMaximo()
    {
        var downloader = new DownloaderFake
        {
            TamanhoInformado = ArquivadorAnexos.TamanhoMaximoArquivamentoBytes + 1
        };
        var armazenamento = new ArmazenamentoFake();
        var arquivador = Criar(downloader, armazenamento, new RepositorioFake());

        await using var arquivo = await arquivador.ObterAsync(Url, "termo.pdf", Ct);

        armazenamento.Objetos.Should().BeEmpty();
    }

    [Fact]
    public async Task ObterAsync_DeveEntregarOArquivo_QuandoOArmazenamentoFalha()
    {
        var downloader = new DownloaderFake();
        var armazenamento = new ArmazenamentoFake { ErroAoGravar = true };
        var arquivador = Criar(downloader, armazenamento, new RepositorioFake());

        await using var arquivo = await arquivador.ObterAsync(Url, "termo.pdf", Ct);

        arquivo.NomeArquivo.Should().Be("termo.pdf");
        downloader.Chamadas.Should().Be(2, "a primeira cópia foi consumida na tentativa de arquivar");
    }

    [Fact]
    public async Task ObterAsync_NaoDeveReenviar_QuandoOObjetoJaExiste()
    {
        var hash = Convert.ToHexString(SHA256.HashData(Conteudo)).ToLowerInvariant();
        var armazenamento = new ArmazenamentoFake();
        armazenamento.Objetos[ArquivadorAnexos.ChaveDe(hash)] = Conteudo;

        var arquivador = Criar(new DownloaderFake(), armazenamento, new RepositorioFake());

        await using var arquivo = await arquivador.ObterAsync(Url, "termo.pdf", Ct);

        armazenamento.Gravacoes.Should().Be(0);
    }

    private static ArquivadorAnexos Criar(
        IAnexoDownloader downloader,
        IArmazenamentoAnexos armazenamento,
        IAnexoArquivadoRepository repositorio) =>
        new(downloader, armazenamento, repositorio, NullLogger<ArquivadorAnexos>.Instance);

    private sealed class DownloaderFake : IAnexoDownloader
    {
        public int Chamadas { get; private set; }
        public bool InformarTamanho { get; init; } = true;
        public long? TamanhoInformado { get; init; }

        public Task<ArquivoAnexo> BaixarAsync(
            string url, string? nomeSugerido, CancellationToken cancellationToken)
        {
            Chamadas++;
            var tamanho = InformarTamanho
                ? TamanhoInformado ?? Conteudo.Length
                : (long?)null;

            return Task.FromResult(new ArquivoAnexo(
                new MemoryStream(Conteudo), "application/pdf", nomeSugerido ?? "anexo", tamanho));
        }
    }

    private sealed class ArmazenamentoFake : IArmazenamentoAnexos
    {
        public bool Habilitado { get; init; } = true;
        public bool ErroAoGravar { get; init; }
        public Dictionary<string, byte[]> Objetos { get; } = [];
        public int Gravacoes { get; private set; }

        public Task<bool> ExisteAsync(string chave, CancellationToken cancellationToken) =>
            Task.FromResult(Objetos.ContainsKey(chave));

        public async Task GravarAsync(
            string chave, Stream conteudo, string tipoConteudo, CancellationToken cancellationToken)
        {
            if (ErroAoGravar)
                throw new InvalidOperationException("falha simulada no armazenamento");

            using var memoria = new MemoryStream();
            await conteudo.CopyToAsync(memoria, cancellationToken);
            Objetos[chave] = memoria.ToArray();
            Gravacoes++;
        }

        public Task<Stream?> AbrirAsync(string chave, CancellationToken cancellationToken) =>
            Task.FromResult<Stream?>(Objetos.TryGetValue(chave, out var bytes)
                ? new MemoryStream(bytes)
                : null);
    }

    private sealed class RepositorioFake : IAnexoArquivadoRepository
    {
        public List<AnexoArquivado> Gravados { get; } = [];

        public Task<AnexoArquivado?> ObterPorUrlAsync(
            string url, CancellationToken cancellationToken) =>
            Task.FromResult(Gravados.FirstOrDefault(anexo => anexo.Url == url));

        public Task<bool> AdicionarAsync(
            AnexoArquivado anexo, CancellationToken cancellationToken)
        {
            Gravados.Add(anexo);
            return Task.FromResult(true);
        }
    }
}
