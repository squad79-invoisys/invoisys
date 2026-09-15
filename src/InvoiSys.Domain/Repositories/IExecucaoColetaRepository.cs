using InvoiSys.Domain.Entities;
using InvoiSys.Domain.Enums;

namespace InvoiSys.Domain.Repositories;

public interface IExecucaoColetaRepository
{
    Task AdicionarAsync(ExecucaoColeta execucao, CancellationToken cancellationToken);
    Task<ExecucaoColeta?> ObterPorIdAsync(Guid id, CancellationToken cancellationToken);
    Task<(IReadOnlyList<ExecucaoColeta> Itens, int Total)> ListarAsync(
        int pagina,
        int tamanhoPagina,
        Guid? fonteId,
        StatusExecucaoColeta? status,
        CancellationToken cancellationToken);
}
