namespace InvoiSys.Application.Fontes.ImportarFontes;

public sealed record ImportarFontesResponse(
    int TotalLinhas,
    int Cadastradas,
    IReadOnlyList<LinhaRejeitada> Rejeitadas);

public sealed record LinhaRejeitada(int Linha, string Motivo);
