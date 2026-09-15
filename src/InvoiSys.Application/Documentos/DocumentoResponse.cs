using InvoiSys.Domain.Entities;

namespace InvoiSys.Application.Documentos;

public sealed record DocumentoResponse(
    Guid Id,
    Guid ExecucaoColetaId,
    string Titulo,
    string UrlOriginal,
    string Tipo,
    string Origem,
    DateTimeOffset? DataPublicacao,
    DateTimeOffset DataColeta,
    string? ConteudoTextual,
    string? Metadados,
    string Hash)
{
    public static DocumentoResponse FromEntity(Documento documento) =>
        new(
            documento.Id,
            documento.ExecucaoColetaId,
            documento.Titulo,
            documento.UrlOriginal,
            documento.Tipo,
            documento.Origem,
            documento.DataPublicacao,
            documento.DataColeta,
            documento.ConteudoTextual,
            documento.Metadados,
            documento.Hash);
}
