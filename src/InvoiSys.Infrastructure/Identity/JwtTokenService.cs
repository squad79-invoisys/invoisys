using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace InvoiSys.Infrastructure.Identity;

internal sealed class JwtTokenService(
    IOptions<JwtOptions> options) : IJwtTokenService
{
    private readonly JwtOptions _options = options.Value;

    public (string Token, DateTimeOffset ExpiraEm) CriarAccessToken(
        ApplicationUser usuario,
        IEnumerable<string> perfis,
        Guid sessaoId)
    {
        var agora = DateTimeOffset.UtcNow;
        var expiraEm = agora.AddMinutes(_options.AccessTokenMinutes);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, usuario.Id.ToString()),
            new(ClaimTypes.NameIdentifier, usuario.Id.ToString()),
            new(JwtRegisteredClaimNames.Email, usuario.Email ?? string.Empty),
            new(ClaimTypes.Email, usuario.Email ?? string.Empty),
            new(ClaimTypes.Name, usuario.Nome),
            new("sid", sessaoId.ToString())
        };

        claims.AddRange(perfis.Select(perfil =>
            new Claim(ClaimTypes.Role, perfil)));

        var key = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(_options.Key));

        var credentials = new SigningCredentials(
            key,
            SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _options.Issuer,
            audience: _options.Audience,
            claims: claims,
            notBefore: agora.UtcDateTime,
            expires: expiraEm.UtcDateTime,
            signingCredentials: credentials);

        return (
            new JwtSecurityTokenHandler().WriteToken(token),
            expiraEm);
    }

    public (string Token, string Hash, DateTimeOffset ExpiraEm) CriarRefreshToken()
    {
        var bytes = RandomNumberGenerator.GetBytes(64);
        var token = Convert.ToBase64String(bytes);
        var hash = GerarHash(token);
        var expiraEm = DateTimeOffset.UtcNow.AddDays(_options.RefreshTokenDays);

        return (token, hash, expiraEm);
    }

    public static string GerarHash(string token) =>
        Convert.ToHexString(
            SHA256.HashData(Encoding.UTF8.GetBytes(token)));
}
