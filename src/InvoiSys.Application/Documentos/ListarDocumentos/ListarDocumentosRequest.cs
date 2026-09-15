namespace InvoiSys.Application.Documentos.ListarDocumentos;

public sealed record ListarDocumentosRequest(
    int Pagina = 1,
    int TamanhoPagina = 20,
    string? Busca = null);
