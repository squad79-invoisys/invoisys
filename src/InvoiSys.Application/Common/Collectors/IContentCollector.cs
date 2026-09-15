using InvoiSys.Domain.Enums;

namespace InvoiSys.Application.Common.Collectors;

public interface IContentCollector
{
    bool Suporta(TipoFonte tipo);

    Task<IReadOnlyList<CollectedDocument>> ColetarAsync(
        string url,
        CancellationToken cancellationToken);
}
