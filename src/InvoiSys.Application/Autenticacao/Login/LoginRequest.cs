namespace InvoiSys.Application.Autenticacao.Login;

public sealed record LoginRequest(
    string Email,
    string Senha);
