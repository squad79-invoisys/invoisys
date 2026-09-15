namespace InvoiSys.Application.Common.Authentication;

public sealed record AuthenticationResult(
    string AccessToken,
    DateTimeOffset AccessTokenExpiraEm,
    string RefreshToken,
    DateTimeOffset RefreshTokenExpiraEm,
    AuthenticatedUser Usuario);
