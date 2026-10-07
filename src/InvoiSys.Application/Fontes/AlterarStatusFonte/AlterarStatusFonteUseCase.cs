using InvoiSys.Application.Common.Abstractions;
using InvoiSys.Application.Common.Auditoria;
using InvoiSys.Application.Common.Exceptions;
using InvoiSys.Domain.Enums;
using InvoiSys.Domain.Repositories;

namespace InvoiSys.Application.Fontes.AlterarStatusFonte;

public sealed class AlterarStatusFonteUseCase(
    ICurrentUser currentUser,
    IFonteRepository fonteRepository,
    IAuditoriaService auditoriaService,
    IUnitOfWork unitOfWork) : IAlterarStatusFonteUseCase
{
    public async Task Execute(
        Guid id,
        bool ativa,
        CancellationToken cancellationToken)
    {
        if (currentUser.UsuarioId is not Guid usuarioId)
            throw new UnauthorizedException("Usuário não autenticado.");

        var fonte = await fonteRepository.ObterPorIdAsync(id, cancellationToken)
            ?? throw new NotFoundException("Fonte não encontrada.");

        if (ativa)
            fonte.Ativar(usuarioId);
        else
            fonte.Desativar(usuarioId);

        await auditoriaService.RegistrarAsync(
            TipoEventoAuditoria.Fonte,
            ativa ? "Fonte ativada" : "Fonte desativada",
            fonte.Nome,
            fonte.Id,
            cancellationToken);
        await unitOfWork.CommitAsync(cancellationToken);
    }
}
