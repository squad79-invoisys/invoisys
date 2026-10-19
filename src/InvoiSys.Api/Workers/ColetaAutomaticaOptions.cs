namespace InvoiSys.Api.Workers;

public sealed class ColetaAutomaticaOptions
{
    public const string SectionName = "ColetaAutomatica";

    public bool Enabled { get; set; } = true;
    public int IntervaloVerificacaoSegundos { get; set; } = 60;
    public int ParalelismoMaximo { get; set; } = 4;
    public int TimeoutFonteSegundos { get; set; } = 120;
}
