using InvoiSys.Domain.Entities;
using InvoiSys.Domain.Enums;

namespace InvoiSys.Domain.Repositories;

public sealed record ExecucaoRecente(
    Guid Id,
    Guid FonteId,
    string FonteNome,
    DateTimeOffset Inicio,
    DateTimeOffset? Fim,
    StatusExecucaoColeta Status,
    int QuantidadeDocumentos);

public interface IDashboardRepository
{
    Task<int> ContarFontesPorStatusAsync(
        StatusFonte status,
        CancellationToken cancellationToken);

    Task<int> ContarColetasDesdeAsync(
        DateTimeOffset desde,
        StatusExecucaoColeta? status,
        CancellationToken cancellationToken);

    Task<int> ContarDocumentosAsync(CancellationToken cancellationToken);

    Task<IReadOnlyList<ExecucaoRecente>> ListarExecucoesRecentesAsync(
        int quantidade,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<Documento>> ListarDocumentosRecentesAsync(
        int quantidade,
        CancellationToken cancellationToken);
}
