using InvoiSys.Domain.Enums;

namespace InvoiSys.Application.Fontes.AtualizarFonte;

public sealed record AtualizarFonteRequest(
    string Nome,
    string Url,
    TipoFonte Tipo,
    int PeriodicidadeMinutos);
