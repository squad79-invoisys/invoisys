namespace InvoiSys.Application.Autenticacao.Login;

public sealed record LoginResponse(
    string AccessToken,
    DateTimeOffset AccessTokenExpiraEm,
    Guid UsuarioId,
    string Nome,
    string Email,
    IReadOnlyList<string> Perfis);
