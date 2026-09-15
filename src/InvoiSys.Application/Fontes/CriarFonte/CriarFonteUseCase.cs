using FluentValidation;
using InvoiSys.Application.Common.Abstractions;
using InvoiSys.Application.Common.Exceptions;
using InvoiSys.Domain.Entities;
using InvoiSys.Domain.Repositories;

namespace InvoiSys.Application.Fontes.CriarFonte;

public sealed class CriarFonteUseCase(
    IValidator<CriarFonteRequest> validator,
    ICurrentUser currentUser,
    IFonteRepository fonteRepository,
    IUnitOfWork unitOfWork) : ICriarFonteUseCase
{
    public async Task<FonteResponse> ExecutarAsync(
        CriarFonteRequest request,
        CancellationToken cancellationToken)
    {
        await validator.ValidateAndThrowAsync(request, cancellationToken);

        if (currentUser.UsuarioId is not Guid usuarioId)
            throw new UnauthorizedException("Usuário não autenticado.");

        var fonte = new Fonte(
            request.Nome,
            request.Url,
            request.Tipo,
            request.PeriodicidadeMinutos,
            usuarioId);

        await fonteRepository.AdicionarAsync(fonte, cancellationToken);
        await unitOfWork.CommitAsync(cancellationToken);

        return FonteResponse.FromEntity(fonte);
    }
}
