using MudBlazor;

namespace InvoiSys.Web.Theme;

public static class AppTheme
{
    public static MudTheme Default { get; } = new()
    {
        PaletteLight = new PaletteLight
        {
            Primary = "#16A34A",      // verde principal (botões, links, ícones ativos)
            Success = "#16A34A",      // mesmo verde para os chips/status "Sucesso"
            Info = "#3B82F6",         // azul, mantido para status "Em andamento"
            Error = "#EF4444",        // vermelho para status "Erro"
        }
    };
}