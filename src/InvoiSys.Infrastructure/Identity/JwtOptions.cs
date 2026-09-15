namespace InvoiSys.Infrastructure.Identity;

public sealed class JwtOptions
{
    public const string SectionName = "Jwt";

    public string Issuer { get; init; } = "InvoiSys.Api";
    public string Audience { get; init; } = "InvoiSys.Web";
    public string Key { get; init; } = string.Empty;
    public int AccessTokenMinutes { get; init; } = 30;
    public int RefreshTokenDays { get; init; } = 7;
}
