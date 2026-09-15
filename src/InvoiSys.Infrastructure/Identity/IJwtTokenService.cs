using InvoiSys.Application.Common.Authentication;

namespace InvoiSys.Infrastructure.Identity;

internal interface IJwtTokenService
{
    (string Token, DateTimeOffset ExpiraEm) CriarAccessToken(
        ApplicationUser usuario,
        IEnumerable<string> perfis,
        Guid sessaoId);

    (string Token, string Hash, DateTimeOffset ExpiraEm) CriarRefreshToken();
}
