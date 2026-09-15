using FluentValidation;
using InvoiSys.Application.Common.Abstractions;
using InvoiSys.Application.Common.Exceptions;
using InvoiSys.Domain.Repositories;

namespace InvoiSys.Application.Fontes.AtualizarFonte;

public sealed class AtualizarFonteUseCase(
    IValidator<AtualizarFonteRequest> validator,
    ICurrentUser currentUser,
    IFonteRepository fonteRepository,
    IUnitOfWork unitOfWork) : IAtualizarFonteUseCase
{
    public async Task<FonteResponse> Execute(
        Guid id,
        AtualizarFonteRequest request,
        CancellationToken cancellationToken)
    {
        await validator.ValidateAndThrowAsync(request, cancellationToken);

        if (currentUser.UsuarioId is not Guid usuarioId)
            throw new UnauthorizedException("Usuário não autenticado.");

        var fonte = await fonteRepository.ObterPorIdAsync(id, cancellationToken)
            ?? throw new NotFoundException("Fonte não encontrada.");

        fonte.Atualizar(
            request.Nome,
            request.Url,
            request.Tipo,
            request.PeriodicidadeMinutos,
            usuarioId);

        await unitOfWork.CommitAsync(cancellationToken);

        return FonteResponse.FromEntity(fonte);
    }
}
