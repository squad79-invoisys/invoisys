using System.Security.Claims;
using InvoiSys.Web.Authentication;
using Microsoft.AspNetCore.Components.Authorization;

namespace InvoiSys.Web.Authentication;

public sealed class ApiAuthenticationStateProvider
    : AuthenticationStateProvider, IDisposable
{
    private readonly TokenStore _tokenStore;

    public ApiAuthenticationStateProvider(TokenStore tokenStore)
    {
        _tokenStore = tokenStore;
        _tokenStore.Changed += OnChanged;
    }

    public override Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        var sessao = _tokenStore.Sessao;

        if (sessao is null)
        {
            return Task.FromResult(
                new AuthenticationState(
                    new ClaimsPrincipal(
                        new ClaimsIdentity())));
        }

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, sessao.UsuarioId.ToString()),
            new(ClaimTypes.Name, sessao.Nome),
            new(ClaimTypes.Email, sessao.Email)
        };

        claims.AddRange(sessao.Perfis.Select(
            perfil => new Claim(ClaimTypes.Role, perfil)));

        var identity = new ClaimsIdentity(
            claims,
            authenticationType: "jwt");

        return Task.FromResult(
            new AuthenticationState(
                new ClaimsPrincipal(identity)));
    }

    private void OnChanged() =>
        NotifyAuthenticationStateChanged(
            GetAuthenticationStateAsync());

    public void Dispose() =>
        _tokenStore.Changed -= OnChanged;
}
