using InvoiSys.Domain.Exceptions;

namespace InvoiSys.Domain.Entities;

/// <summary>
/// Registro de um anexo cujo arquivo já foi copiado para o armazenamento
/// próprio. A chave do objeto é o hash do conteúdo, então o mesmo arquivo
/// referenciado por vários documentos ocupa espaço uma única vez.
/// </summary>
public sealed class AnexoArquivado
{
    private AnexoArquivado()
    {
    }

    public AnexoArquivado(
        string url,
        string hashConteudo,
        string chaveObjeto,
        string tipoConteudo,
        string nomeArquivo,
        long tamanho)
    {
        if (!Uri.TryCreate(url, UriKind.Absolute, out _))
            throw new DomainException("A URL do anexo é inválida.");

        if (string.IsNullOrWhiteSpace(hashConteudo))
            throw new DomainException("O hash do conteúdo é obrigatório.");

        if (string.IsNullOrWhiteSpace(chaveObjeto))
            throw new DomainException("A chave do objeto é obrigatória.");

        if (tamanho <= 0)
            throw new DomainException("O tamanho do anexo deve ser positivo.");

        Id = Guid.NewGuid();
        Url = url.Trim();
        HashConteudo = hashConteudo.Trim();
        ChaveObjeto = chaveObjeto.Trim();
        TipoConteudo = tipoConteudo.Trim();
        NomeArquivo = nomeArquivo.Trim();
        Tamanho = tamanho;
        ArquivadoEm = DateTimeOffset.UtcNow;
    }

    public Guid Id { get; private set; }
    public string Url { get; private set; } = string.Empty;
    public string HashConteudo { get; private set; } = string.Empty;
    public string ChaveObjeto { get; private set; } = string.Empty;
    public string TipoConteudo { get; private set; } = string.Empty;
    public string NomeArquivo { get; private set; } = string.Empty;
    public long Tamanho { get; private set; }
    public DateTimeOffset ArquivadoEm { get; private set; }
}
