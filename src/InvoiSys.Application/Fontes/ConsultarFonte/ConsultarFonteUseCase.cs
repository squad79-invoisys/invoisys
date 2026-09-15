using InvoiSys.Application.Common.Exceptions;
using InvoiSys.Domain.Repositories;

namespace InvoiSys.Application.Fontes.ConsultarFonte;

public sealed class ConsultarFonteUseCase(
    IFonteRepository fonteRepository) : IConsultarFonteUseCase
{
    public async Task<FonteResponse> Execute(
        Guid id,
        CancellationToken cancellationToken)
    {
        var fonte = await fonteRepository.ObterPorIdAsync(id, cancellationToken)
            ?? throw new NotFoundException("Fonte não encontrada.");

        return FonteResponse.FromEntity(fonte);
    }
}
