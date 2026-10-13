namespace InvoiSys.Application.Documentos.ListarDocumentos;

public sealed record ListarDocumentosRequest(
    int Pagina = 1,
    int TamanhoPagina = 20,
    string? Contexto = null,
    string? Busca = null);
