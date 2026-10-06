using InvoiSys.Application.Common.Auditoria;
using InvoiSys.Application.Common.Authentication;
using InvoiSys.Domain.Enums;
using InvoiSys.Domain.Repositories;

namespace InvoiSys.Application.Autenticacao.Logout;

public sealed class LogoutUseCase(
    IAuthenticationService authenticationService,
    IAuditoriaService auditoriaService,
    IUnitOfWork unitOfWork) : ILogoutUseCase
{
    public async Task ExecutarAsync(
        string refreshToken,
        CancellationToken cancellationToken)
    {
        var sessao = await authenticationService.LogoutAsync(
            refreshToken,
            cancellationToken);

        if (sessao is null)
            return;

        await auditoriaService.RegistrarAsync(
            TipoEventoAuditoria.Autenticacao,
            "Logout realizado",
            sessao.Email,
            sessao.UsuarioId,
            sessao.UsuarioId,
            sessao.Email,
            cancellationToken);
        await unitOfWork.CommitAsync(cancellationToken);
    }
}
