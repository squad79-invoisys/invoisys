using MudBlazor;

namespace InvoiSys.Web.Theme;

public static class AppTheme
{
    public static MudTheme Default { get; } = new()
    {
        PaletteLight = new PaletteLight
        {
            Primary = "#0a9e40",
            Success = "#16A34A",
            Info = "#3B82F6",
            Error = "#EF4444",
            Background = "#f7fafb",
            Surface = "#ffffff",
            TextPrimary = "#141c29",
            TextSecondary = "#5e697a",
            Divider = "#e3e8ed"
        },
        LayoutProperties = new LayoutProperties { DefaultBorderRadius = "8px" },
        Typography = new Typography
        {
            Default = new DefaultTypography { FontFamily = ["Inter", "Arial", "sans-serif"] }
        }
    };
}