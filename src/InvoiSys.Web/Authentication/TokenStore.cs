using InvoiSys.Application.Autenticacao.Login;

namespace InvoiSys.Web.Authentication;

public sealed class TokenStore
{
    public LoginResponse? Sessao { get; private set; }

    public string? AccessToken => Sessao?.AccessToken;

    public event Action? Changed;

    public void Definir(LoginResponse response)
    {
        Sessao = response;
        Changed?.Invoke();
    }

    public void Limpar()
    {
        Sessao = null;
        Changed?.Invoke();
    }
}
