using InvoiSys.Domain.Enums;

namespace InvoiSys.Application.Fontes.CriarFonte;

public sealed record CriarFonteRequest(
    string Nome,
    string Url,
    TipoFonte Tipo,
    int PeriodicidadeMinutos);
