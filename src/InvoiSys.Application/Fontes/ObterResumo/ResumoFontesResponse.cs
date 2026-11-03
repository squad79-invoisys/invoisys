namespace InvoiSys.Application.Fontes.ObterResumo;

public sealed record ResumoFontesResponse(
    int TotalFontes,
    int FontesAtivas,
    int FontesInativas);
