using InvoiSys.Domain.Enums;

namespace InvoiSys.Web.Contracts;

public sealed class NovaFonteFormModel
{
    public string Nome { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public TipoFonte? Tipo { get; set; }
    public int? PeriodicidadeMinutos { get; set; }
}
