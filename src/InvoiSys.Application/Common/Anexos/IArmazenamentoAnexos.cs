namespace InvoiSys.Application.Common.Anexos;

/// <summary>
/// Armazenamento próprio dos arquivos de anexo. A aplicação não sabe se por
/// trás existe um bucket, um disco ou outra coisa.
/// </summary>
public interface IArmazenamentoAnexos
{
    /// <summary>
    /// Falso quando não há credenciais configuradas. Nesse caso os anexos
    /// continuam sendo servidos direto do site de origem.
    /// </summary>
    bool Habilitado { get; }

    Task<bool> ExisteAsync(string chave, CancellationToken cancellationToken);

    Task GravarAsync(
        string chave,
        Stream conteudo,
        string tipoConteudo,
        CancellationToken cancellationToken);

    /// <summary>Nulo quando o objeto não existe mais no armazenamento.</summary>
    Task<Stream?> AbrirAsync(string chave, CancellationToken cancellationToken);
}
