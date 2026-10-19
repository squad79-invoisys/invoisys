using System.Security.Claims;

namespace InvoiSys.Web.Authentication;

public static class UserDisplay
{
    public static string Identificacao(ClaimsPrincipal usuario) =>
        usuario.Identity?.Name
        ?? usuario.FindFirst(ClaimTypes.Email)?.Value
        ?? string.Empty;

    public static string Perfil(ClaimsPrincipal usuario) =>
        usuario.FindFirst(ClaimTypes.Role)?.Value ?? string.Empty;

    public static string Iniciais(string identificacao)
    {
        var partes = identificacao
            .Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        return partes.Length switch
        {
            0 => "?",
            1 => char.ToUpperInvariant(partes[0][0]).ToString(),
            _ => string.Concat(
                char.ToUpperInvariant(partes[0][0]),
                char.ToUpperInvariant(partes[^1][0]))
        };
    }
}
