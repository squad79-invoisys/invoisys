using InvoiSys.Application.Common.Authentication;
using InvoiSys.Application.Common.Exceptions;
using InvoiSys.Infrastructure.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace InvoiSys.Infrastructure.Identity;

internal sealed class AuthenticationService(
    UserManager<ApplicationUser> userManager,
    ApplicationDbContext dbContext,
    IJwtTokenService jwtTokenService) : IAuthenticationService
{
    public async Task<AuthenticationResult> LoginAsync(
        string email,
        string senha,
        CancellationToken cancellationToken)
    {
        var usuario = await userManager.FindByEmailAsync(email);

        if (usuario is null ||
            !usuario.Ativo ||
            !await userManager.CheckPasswordAsync(usuario, senha))
        {
            throw new UnauthorizedException("E-mail ou senha inválidos.");
        }

        var perfis = await userManager.GetRolesAsync(usuario);
        var sessaoId = Guid.NewGuid();

        var (accessToken, accessExpiraEm) =
            jwtTokenService.CriarAccessToken(usuario, perfis, sessaoId);

        var (refreshToken, refreshHash, refreshExpiraEm) =
            jwtTokenService.CriarRefreshToken();

        dbContext.RefreshTokens.Add(new RefreshToken
        {
            SessionId = sessaoId,
            UserId = usuario.Id,
            TokenHash = refreshHash,
            ExpiraEm = refreshExpiraEm
        });

        await dbContext.SaveChangesAsync(cancellationToken);

        return new AuthenticationResult(
            accessToken,
            accessExpiraEm,
            refreshToken,
            refreshExpiraEm,
            new AuthenticatedUser(
                usuario.Id,
                usuario.Nome,
                usuario.Email ?? string.Empty,
                perfis.ToList()));
    }

    public async Task<AuthenticationResult> RefreshAsync(
        string refreshToken,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(refreshToken))
            throw new UnauthorizedException("Refresh token inválido.");

        var hash = JwtTokenService.GerarHash(refreshToken);

        var sessao = await dbContext.RefreshTokens
            .Include(token => token.Usuario)
            .FirstOrDefaultAsync(
                token => token.TokenHash == hash,
                cancellationToken);

        if (sessao is null ||
            sessao.RevogadoEm is not null ||
            sessao.ExpiraEm <= DateTimeOffset.UtcNow ||
            !sessao.Usuario.Ativo)
        {
            throw new UnauthorizedException("Sessão inválida ou expirada.");
        }

        var perfis = await userManager.GetRolesAsync(sessao.Usuario);

        var (novoAccessToken, accessExpiraEm) =
            jwtTokenService.CriarAccessToken(
                sessao.Usuario,
                perfis,
                sessao.SessionId);

        var (novoRefreshToken, novoHash, refreshExpiraEm) =
            jwtTokenService.CriarRefreshToken();

        sessao.TokenHash = novoHash;
        sessao.ExpiraEm = refreshExpiraEm;

        await dbContext.SaveChangesAsync(cancellationToken);

        return new AuthenticationResult(
            novoAccessToken,
            accessExpiraEm,
            novoRefreshToken,
            refreshExpiraEm,
            new AuthenticatedUser(
                sessao.Usuario.Id,
                sessao.Usuario.Nome,
                sessao.Usuario.Email ?? string.Empty,
                perfis.ToList()));
    }

    public async Task LogoutAsync(
        string refreshToken,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(refreshToken))
            return;

        var hash = JwtTokenService.GerarHash(refreshToken);

        var sessao = await dbContext.RefreshTokens
            .FirstOrDefaultAsync(
                token => token.TokenHash == hash,
                cancellationToken);

        if (sessao is null)
            return;

        sessao.RevogadoEm = DateTimeOffset.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
