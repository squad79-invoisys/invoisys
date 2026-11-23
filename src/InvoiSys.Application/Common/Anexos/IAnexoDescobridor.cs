namespace InvoiSys.Application.Common.Anexos;

public sealed record AnexoDescoberto(string Url, string? Nome, string? TipoConteudo);

/// <param name="Completa">
/// Falso quando a página de origem não pôde ser consultada (indisponível,
/// bloqueada ou com erro); a lista pode então estar incompleta.
/// </param>
public sealed record ResultadoDescoberta(IReadOnlyList<AnexoDescoberto> Anexos, bool Completa);

public interface IAnexoDescobridor
{
    /// <summary>
    /// Consulta a URL do documento e devolve os arquivos que ela oferece: a
    /// própria URL, quando é um arquivo, ou os links para arquivos da página.
    /// </summary>
    Task<ResultadoDescoberta> DescobrirAsync(
        string urlDocumento,
        CancellationToken cancellationToken);
}
