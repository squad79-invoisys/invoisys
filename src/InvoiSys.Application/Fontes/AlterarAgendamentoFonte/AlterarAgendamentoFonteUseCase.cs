using InvoiSys.Application.Common.Abstractions;
using InvoiSys.Application.Common.Auditoria;
using InvoiSys.Application.Common.Exceptions;
using InvoiSys.Domain.Enums;
using InvoiSys.Domain.Repositories;

namespace InvoiSys.Application.Fontes.AlterarAgendamentoFonte;

public sealed class AlterarAgendamentoFonteUseCase(
    ICurrentUser currentUser,
    IFonteRepository fonteRepository,
    IAuditoriaService auditoriaService,
    IUnitOfWork unitOfWork) : IAlterarAgendamentoFonteUseCase
{
    public async Task Execute(
        Guid id,
        bool pausado,
        CancellationToken cancellationToken)
    {
        if (currentUser.UsuarioId is not Guid usuarioId)
            throw new UnauthorizedException("Usuário não autenticado.");

        var fonte = await fonteRepository.ObterPorIdAsync(id, cancellationToken)
            ?? throw new NotFoundException("Fonte não encontrada.");

        if (pausado)
            fonte.PausarAgendamento(usuarioId);
        else
            fonte.RetomarAgendamento(usuarioId);

        await auditoriaService.RegistrarAsync(
            TipoEventoAuditoria.Fonte,
            pausado ? "Agendamento pausado" : "Agendamento retomado",
            fonte.Nome,
            fonte.Id,
            cancellationToken);
        await unitOfWork.CommitAsync(cancellationToken);
    }
}
