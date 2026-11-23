namespace InvoiSys.Application.Common.Anexos;

/// <summary>
/// Arquivo em transferência. Descartar o objeto encerra a conexão com a origem.
/// </summary>
public sealed class ArquivoAnexo(
    Stream conteudo,
    string tipoConteudo,
    string nomeArquivo) : IAsyncDisposable
{
    public Stream Conteudo { get; } = conteudo;
    public string TipoConteudo { get; } = tipoConteudo;
    public string NomeArquivo { get; } = nomeArquivo;

    public ValueTask DisposeAsync() => Conteudo.DisposeAsync();
}

public interface IAnexoDownloader
{
    /// <exception cref="Exceptions.BusinessException">
    /// O destino recusou, não é um arquivo ou excede o tamanho máximo.
    /// </exception>
    Task<ArquivoAnexo> BaixarAsync(
        string url,
        string? nomeSugerido,
        CancellationToken cancellationToken);
}
