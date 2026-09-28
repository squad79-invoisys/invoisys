using InvoiSys.Domain.Entities;
using InvoiSys.Domain.Enums;
using InvoiSys.Domain.Repositories;

namespace InvoiSys.Application.Dashboard.ObterResumo;

public sealed record ExecucaoRecenteResponse(
    Guid Id,
    Guid FonteId,
    string FonteNome,
    DateTimeOffset Inicio,
    DateTimeOffset? Fim,
    StatusExecucaoColeta Status,
    int QuantidadeDocumentos)
{
    public static ExecucaoRecenteResponse FromReadModel(ExecucaoRecente execucao) =>
        new(
            execucao.Id,
            execucao.FonteId,
            execucao.FonteNome,
            execucao.Inicio,
            execucao.Fim,
            execucao.Status,
            execucao.QuantidadeDocumentos);
}

public sealed record DocumentoRecenteResponse(
    Guid Id,
    string Titulo,
    string Origem,
    string Tipo,
    string UrlOriginal,
    DateTimeOffset? DataPublicacao,
    DateTimeOffset DataColeta)
{
    public static DocumentoRecenteResponse FromEntity(Documento documento) =>
        new(
            documento.Id,
            documento.Titulo,
            documento.Origem,
            documento.Tipo,
            documento.UrlOriginal,
            documento.DataPublicacao,
            documento.DataColeta);
}

public sealed record ResumoDashboardResponse(
    int FontesAtivas,
    int FontesInativas,
    int ColetasUltimas24h,
    int ColetasComFalhaUltimas24h,
    int TotalDocumentos,
    IReadOnlyList<ExecucaoRecenteResponse> ExecucoesRecentes,
    IReadOnlyList<DocumentoRecenteResponse> DocumentosRecentes);
