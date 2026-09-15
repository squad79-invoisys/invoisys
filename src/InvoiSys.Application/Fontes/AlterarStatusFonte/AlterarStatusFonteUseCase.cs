using InvoiSys.Application.Common.Abstractions;
using InvoiSys.Application.Common.Exceptions;
using InvoiSys.Domain.Repositories;

namespace InvoiSys.Application.Fontes.AlterarStatusFonte;

public sealed class AlterarStatusFonteUseCase(
    ICurrentUser currentUser,
    IFonteRepository fonteRepository,
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

        await unitOfWork.CommitAsync(cancellationToken);
    }
}
