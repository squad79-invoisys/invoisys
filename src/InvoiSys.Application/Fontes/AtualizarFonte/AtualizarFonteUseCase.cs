using FluentValidation;
using InvoiSys.Application.Common.Abstractions;
using InvoiSys.Application.Common.Exceptions;
using InvoiSys.Application.Common.Fontes;
using InvoiSys.Domain.Repositories;

namespace InvoiSys.Application.Fontes.AtualizarFonte;

public sealed class AtualizarFonteUseCase(
    IValidator<AtualizarFonteRequest> validator,
    ICurrentUser currentUser,
    IFonteUrlValidator fonteUrlValidator,
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

        var urlNormalizada = request.Url.Trim();
        var origemAlterada =
            !string.Equals(
                fonte.Url,
                urlNormalizada,
                StringComparison.Ordinal) ||
            fonte.Tipo != request.Tipo;

        if (origemAlterada)
        {
            await fonteUrlValidator.ValidarAsync(
                urlNormalizada,
                request.Tipo,
                cancellationToken);
        }

        fonte.Atualizar(
            request.Nome,
            urlNormalizada,
            request.Tipo,
            request.PeriodicidadeMinutos,
            usuarioId,
            request.SeletorConteudo);

        await unitOfWork.CommitAsync(cancellationToken);

        return FonteResponse.FromEntity(fonte);
    }
}
