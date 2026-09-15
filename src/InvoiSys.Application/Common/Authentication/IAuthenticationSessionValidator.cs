namespace InvoiSys.Application.Common.Authentication;

public interface IAuthenticationSessionValidator
{
    Task<bool> SessaoValidaAsync(
        Guid usuarioId,
        Guid sessaoId,
        CancellationToken cancellationToken);
}
