using InvoiSys.Domain.Entities;

namespace InvoiSys.Domain.Repositories;

public interface IAnexoArquivadoRepository
{
    Task<AnexoArquivado?> ObterPorUrlAsync(string url, CancellationToken cancellationToken);

    /// <summary>
    /// Grava o registro. Se a mesma URL já tiver sido arquivada em paralelo,
    /// a gravação é ignorada e o método devolve falso.
    /// </summary>
    Task<bool> AdicionarAsync(AnexoArquivado anexo, CancellationToken cancellationToken);
}
