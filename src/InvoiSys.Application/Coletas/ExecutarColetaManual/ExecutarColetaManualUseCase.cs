using InvoiSys.Application.Coletas.ExecutarColeta;
using InvoiSys.Application.Common.Abstractions;
using InvoiSys.Application.Common.Exceptions;

namespace InvoiSys.Application.Coletas.ExecutarColetaManual;

public sealed class ExecutarColetaManualUseCase(
    ICurrentUser currentUser,
    IColetaExecutor executor) : IExecutarColetaManualUseCase
{
    public Task<ExecucaoColetaResponse> ExecutarAsync(
        Guid fonteId,
        CancellationToken cancellationToken)
    {
        if (currentUser.UsuarioId is not Guid usuarioId)
            throw new UnauthorizedException("Usuário não autenticado.");

        return executor.ExecutarManualAsync(fonteId, usuarioId, cancellationToken);
    }
}
