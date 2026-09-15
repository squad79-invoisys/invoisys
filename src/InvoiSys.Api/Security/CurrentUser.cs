using System.Security.Claims;
using InvoiSys.Application.Common.Abstractions;

namespace InvoiSys.Api.Security;

public sealed class CurrentUser(
    IHttpContextAccessor httpContextAccessor) : ICurrentUser
{
    private ClaimsPrincipal? Principal =>
        httpContextAccessor.HttpContext?.User;

    public Guid? UsuarioId
    {
        get
        {
            var value = Principal?.FindFirstValue(ClaimTypes.NameIdentifier);
            return Guid.TryParse(value, out var id) ? id : null;
        }
    }

    public string? Email =>
        Principal?.FindFirstValue(ClaimTypes.Email);

    public bool EstaAutenticado =>
        Principal?.Identity?.IsAuthenticated == true;
}
