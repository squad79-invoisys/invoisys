namespace InvoiSys.Application.Common.Collectors;

public sealed record CollectedDocument(
    string Titulo,
    string UrlOriginal,
    string Tipo,
    string Origem,
    DateTimeOffset? DataPublicacao,
    string? ConteudoTextual,
    string? Metadados,
    string Hash);
