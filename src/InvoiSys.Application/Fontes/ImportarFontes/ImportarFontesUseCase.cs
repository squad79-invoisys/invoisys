using System.Globalization;
using System.Text;
using FluentValidation;
using InvoiSys.Application.Common.Abstractions;
using InvoiSys.Application.Common.Exceptions;
using InvoiSys.Application.Fontes.CriarFonte;
using InvoiSys.Domain.Enums;
using InvoiSys.Domain.Exceptions;

namespace InvoiSys.Application.Fontes.ImportarFontes;

public sealed class ImportarFontesUseCase(
    ICriarFonteUseCase criarFonteUseCase,
    ICurrentUser currentUser) : IImportarFontesUseCase
{
    public const int TamanhoMaximoBytes = 1_048_576;
    public const int LimiteLinhas = 100;
    public const string ContextoPadrao = "Outro";

    private static readonly string[] ColunasObrigatorias =
        ["nome", "url", "tipo", "periodicidade"];

    private const string ColunaContexto = "contexto";

    public async Task<ImportarFontesResponse> ExecutarAsync(
        Stream arquivo,
        CancellationToken cancellationToken)
    {
        if (currentUser.UsuarioId is null)
            throw new UnauthorizedException("Usuário não autenticado.");

        var texto = await LerTextoAsync(arquivo, cancellationToken);
        var conteudo = FontesCsvReader.Ler(texto);
        var indices = MapearColunas(conteudo.Cabecalho);

        if (conteudo.Registros.Count == 0)
            throw new BusinessException("O arquivo não tem linhas de dados.");

        if (conteudo.Registros.Count > LimiteLinhas)
            throw new BusinessException(
                $"O arquivo deve ter no máximo {LimiteLinhas} linhas de dados.");

        var rejeitadas = new List<LinhaRejeitada>();
        var urlsCadastradas = new Dictionary<string, int>(StringComparer.Ordinal);
        var cadastradas = 0;

        foreach (var registro in conteudo.Registros)
        {
            var motivo = await ProcessarRegistroAsync(
                registro,
                conteudo.Cabecalho.Count,
                indices,
                urlsCadastradas,
                cancellationToken);

            if (motivo is null)
                cadastradas++;
            else
                rejeitadas.Add(new LinhaRejeitada(registro.Linha, motivo));
        }

        return new ImportarFontesResponse(
            conteudo.Registros.Count,
            cadastradas,
            rejeitadas);
    }

    private async Task<string?> ProcessarRegistroAsync(
        RegistroCsv registro,
        int totalColunas,
        Dictionary<string, int> indices,
        Dictionary<string, int> urlsCadastradas,
        CancellationToken cancellationToken)
    {
        if (registro.Campos.Count != totalColunas)
            return $"A linha tem {registro.Campos.Count} colunas, mas o cabeçalho tem {totalColunas}.";

        var nome = registro.Campos[indices["nome"]];
        var url = registro.Campos[indices["url"]].Trim();
        var tipoTexto = registro.Campos[indices["tipo"]].Trim();
        var periodicidadeTexto = registro.Campos[indices["periodicidade"]].Trim();

        var tipoNome = Enum.GetNames<TipoFonte>().FirstOrDefault(
            n => string.Equals(n, tipoTexto, StringComparison.OrdinalIgnoreCase));
        if (tipoNome is null)
            return $"Tipo inválido: '{tipoTexto}'. Use Rss, Atom ou WebHtml.";

        var tipo = Enum.Parse<TipoFonte>(tipoNome);

        if (!int.TryParse(
                periodicidadeTexto,
                NumberStyles.Integer,
                CultureInfo.InvariantCulture,
                out var periodicidade))
            return $"Periodicidade inválida: '{periodicidadeTexto}'. Informe um número inteiro de minutos.";

        var contexto = indices.TryGetValue(ColunaContexto, out var indiceContexto)
            ? registro.Campos[indiceContexto].Trim()
            : string.Empty;
        if (string.IsNullOrWhiteSpace(contexto))
            contexto = ContextoPadrao;

        if (urlsCadastradas.TryGetValue(url, out var linhaAnterior))
            return $"URL repetida no arquivo (já cadastrada pela linha {linhaAnterior}).";

        try
        {
            await criarFonteUseCase.ExecutarAsync(
                new CriarFonteRequest(nome, url, tipo, periodicidade, contexto),
                cancellationToken);
        }
        catch (ValidationException ex)
        {
            return string.Join(
                " ",
                ex.Errors.Select(e => e.ErrorMessage).Distinct());
        }
        catch (InvoiSysException ex) when (ex is not UnauthorizedException)
        {
            return string.Join(" ", ex.GetErrors());
        }
        catch (DomainException ex)
        {
            return ex.Message;
        }

        urlsCadastradas[url] = registro.Linha;
        return null;
    }

    private static Dictionary<string, int> MapearColunas(IReadOnlyList<string> cabecalho)
    {
        var indices = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

        for (var i = 0; i < cabecalho.Count; i++)
        {
            var coluna = cabecalho[i];
            if (!ColunasObrigatorias.Contains(coluna, StringComparer.OrdinalIgnoreCase) &&
                !string.Equals(coluna, ColunaContexto, StringComparison.OrdinalIgnoreCase))
                continue;

            if (!indices.TryAdd(coluna, i))
                throw new BusinessException(
                    $"A coluna '{coluna.ToLowerInvariant()}' aparece mais de uma vez no cabeçalho.");
        }

        var ausentes = ColunasObrigatorias.Where(c => !indices.ContainsKey(c)).ToList();
        if (ausentes.Count > 0)
            throw new BusinessException(
                $"Colunas obrigatórias ausentes: {string.Join(", ", ausentes)}.");

        return indices;
    }

    private static async Task<string> LerTextoAsync(
        Stream arquivo,
        CancellationToken cancellationToken)
    {
        using var memoria = new MemoryStream();
        var buffer = new byte[8192];
        long total = 0;
        int lidos;

        while ((lidos = await arquivo.ReadAsync(buffer, cancellationToken)) > 0)
        {
            total += lidos;
            if (total > TamanhoMaximoBytes)
                throw new BusinessException("O arquivo deve ter no máximo 1 MB.");

            memoria.Write(buffer, 0, lidos);
        }

        if (total == 0)
            throw new BusinessException("O arquivo CSV está vazio.");

        var utf8 = new UTF8Encoding(
            encoderShouldEmitUTF8Identifier: false,
            throwOnInvalidBytes: true);

        try
        {
            return utf8.GetString(memoria.GetBuffer(), 0, (int)memoria.Length)
                .TrimStart('﻿');
        }
        catch (DecoderFallbackException)
        {
            throw new BusinessException("O arquivo deve estar em UTF-8.");
        }
    }
}
