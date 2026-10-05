using InvoiSys.Domain.Enums;

namespace InvoiSys.Application.Common.Collectors;

public interface IContentCollector
{
    bool Suporta(TipoFonte tipo);

    /// <param name="seletorConteudo">
    /// Seletor CSS opcional que delimita o conteúdo principal. Usado apenas pelos
    /// coletores de página web; os demais ignoram o valor.
    /// </param>
    Task<IReadOnlyList<CollectedDocument>> ColetarAsync(
        string url,
        string? seletorConteudo,
        CancellationToken cancellationToken);
}
