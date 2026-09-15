namespace InvoiSys.Application.Common.Responses;

public sealed record PagedResponse<T>(
    IReadOnlyList<T> Itens,
    int Pagina,
    int TamanhoPagina,
    int Total)
{
    public int TotalPaginas =>
        TamanhoPagina <= 0 ? 0 : (int)Math.Ceiling(Total / (double)TamanhoPagina);
}
