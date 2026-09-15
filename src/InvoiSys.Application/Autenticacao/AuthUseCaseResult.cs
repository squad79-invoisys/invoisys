namespace InvoiSys.Application.Autenticacao;

public sealed record AuthUseCaseResult(
    Login.LoginResponse Response,
    string RefreshToken,
    DateTimeOffset RefreshTokenExpiraEm);
