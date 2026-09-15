using InvoiSys.Application.Common.Exceptions;
using InvoiSys.Domain.Repositories;

namespace InvoiSys.Application.Coletas.ConsultarColeta;

public sealed class ConsultarColetaUseCase(
    IExecucaoColetaRepository execucaoRepository) : IConsultarColetaUseCase
{
    public async Task<ExecucaoColetaResponse> Execute(
        Guid id,
        CancellationToken cancellationToken)
    {
        var execucao = await execucaoRepository.ObterPorIdAsync(id, cancellationToken)
            ?? throw new NotFoundException("Execução de coleta não encontrada.");

        return ExecucaoColetaResponse.FromEntity(execucao);
    }
}
