using InvoiSys.Domain.Exceptions;

namespace InvoiSys.Domain.Entities;

public sealed class Documento
{
    private Documento()
    {
    }

    public Documento(
        Guid execucaoColetaId,
        string titulo,
        string urlOriginal,
        string tipo,
        string origem,
        DateTimeOffset? dataPublicacao,
        string? conteudoTextual,
        string? metadados,
        string hash)
    {
        if (execucaoColetaId == Guid.Empty)
            throw new DomainException("A execução da coleta é obrigatória.");

        if (string.IsNullOrWhiteSpace(titulo))
            throw new DomainException("O título do documento é obrigatório.");

        if (!Uri.TryCreate(urlOriginal, UriKind.Absolute, out _))
            throw new DomainException("A URL original do documento é inválida.");

        if (string.IsNullOrWhiteSpace(hash))
            throw new DomainException("O hash do documento é obrigatório.");

        Id = Guid.NewGuid();
        ExecucaoColetaId = execucaoColetaId;
        Titulo = titulo.Trim();
        UrlOriginal = urlOriginal.Trim();
        Tipo = tipo.Trim();
        Origem = origem.Trim();
        DataPublicacao = dataPublicacao;
        DataColeta = DateTimeOffset.UtcNow;
        ConteudoTextual = conteudoTextual;
        Metadados = metadados;
        Hash = hash;
    }

    public Guid Id { get; private set; }
    public Guid ExecucaoColetaId { get; private set; }
    public string Titulo { get; private set; } = string.Empty;
    public string UrlOriginal { get; private set; } = string.Empty;
    public string Tipo { get; private set; } = string.Empty;
    public string Origem { get; private set; } = string.Empty;
    public DateTimeOffset? DataPublicacao { get; private set; }
    public DateTimeOffset DataColeta { get; private set; }
    public string? ConteudoTextual { get; private set; }
    public string? Metadados { get; private set; }
    public string Hash { get; private set; } = string.Empty;
}
