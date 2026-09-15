using InvoiSys.Application.Autenticacao.Login;
using InvoiSys.Web.Authentication;

namespace InvoiSys.Web.Clients;

public sealed class AuthClient(
    IAuthApi authApi,
    TokenStore tokenStore)
{
    public async Task<bool> LoginAsync(
        string email,
        string senha,
        CancellationToken cancellationToken = default)
    {
        var response = await authApi.LoginAsync(
            new LoginRequest(email, senha),
            cancellationToken);

        if (!response.Sucesso || response.Dados is null)
            return false;

        tokenStore.Definir(response.Dados);
        return true;
    }

    public async Task<bool> TentarRestaurarSessaoAsync(
        CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await authApi.RefreshAsync(
                cancellationToken);

            if (!response.Sucesso || response.Dados is null)
            {
                tokenStore.Limpar();
                return false;
            }

            tokenStore.Definir(response.Dados);
            return true;
        }
        catch
        {
            tokenStore.Limpar();
            return false;
        }
    }

    public async Task LogoutAsync(
        CancellationToken cancellationToken = default)
    {
        try
        {
            await authApi.LogoutAsync(cancellationToken);
        }
        finally
        {
            tokenStore.Limpar();
        }
    }
}
