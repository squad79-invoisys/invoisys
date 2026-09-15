using InvoiSys.Domain.Enums;

namespace InvoiSys.Application.Fontes.ListarFontes;

public sealed record ListarFontesRequest(
    int Pagina = 1,
    int TamanhoPagina = 20,
    TipoFonte? Tipo = null,
    StatusFonte? Status = null,
    string? Busca = null);
