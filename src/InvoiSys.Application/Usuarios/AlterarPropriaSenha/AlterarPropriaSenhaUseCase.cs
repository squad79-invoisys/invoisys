using FluentValidation;
using InvoiSys.Application.Common.Abstractions;
using InvoiSys.Application.Common.Auditoria;
using InvoiSys.Application.Common.Authentication;
using InvoiSys.Application.Common.Exceptions;
using InvoiSys.Domain.Enums;
using InvoiSys.Domain.Repositories;

namespace InvoiSys.Application.Usuarios.AlterarPropriaSenha;

public sealed class AlterarPropriaSenhaUseCase(
    IValidator<AlterarPropriaSenhaRequest> validator,
    ICurrentUser currentUser,
    IUserManagementService userManagementService,
    IAuditoriaService auditoriaService,
    IUnitOfWork unitOfWork) : IAlterarPropriaSenhaUseCase
{
    public async Task Execute(
        AlterarPropriaSenhaRequest request,
        Guid? sessaoAtualId,
        CancellationToken cancellationToken)
    {
        if (currentUser.UsuarioId is not Guid usuarioId)
            throw new UnauthorizedException("Usuário não autenticado.");

        await validator.ValidateAndThrowAsync(request, cancellationToken);

        var usuario = await userManagementService.AlterarPropriaSenhaAsync(
            usuarioId,
            request.SenhaAtual,
            request.NovaSenha,
            sessaoAtualId,
            cancellationToken);

        await auditoriaService.RegistrarAsync(
            TipoEventoAuditoria.Usuario,
            "Senha alterada pelo próprio usuário",
            usuario.Email,
            usuario.Id,
            cancellationToken);
        await unitOfWork.CommitAsync(cancellationToken);
    }
}
