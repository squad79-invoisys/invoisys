using InvoiSys.Application.Common.Authentication;
using InvoiSys.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace InvoiSys.Infrastructure.Identity;

internal sealed class AuthenticationSessionValidator(
    ApplicationDbContext dbContext) : IAuthenticationSessionValidator
{
    public Task<bool> SessaoValidaAsync(
        Guid usuarioId,
        Guid sessaoId,
        CancellationToken cancellationToken) =>
        dbContext.RefreshTokens
            .AsNoTracking()
            .AnyAsync(
                token =>
                    token.UserId == usuarioId &&
                    token.SessionId == sessaoId &&
                    token.RevogadoEm == null &&
                    token.ExpiraEm > DateTimeOffset.UtcNow &&
                    token.Usuario.Ativo,
                cancellationToken);
}
