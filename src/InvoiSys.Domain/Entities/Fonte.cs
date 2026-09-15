using InvoiSys.Domain.Enums;
using InvoiSys.Domain.Exceptions;

namespace InvoiSys.Domain.Entities;

public sealed class Fonte
{
    private Fonte()
    {
    }

    public Fonte(
        string nome,
        string url,
        TipoFonte tipo,
        int periodicidadeMinutos,
        Guid criadoPorUsuarioId)
    {
        ValidarNome(nome);
        ValidarUrl(url);
        ValidarPeriodicidade(periodicidadeMinutos);

        if (criadoPorUsuarioId == Guid.Empty)
            throw new DomainException("O usuário responsável pelo cadastro é obrigatório.");

        Id = Guid.NewGuid();
        Nome = nome.Trim();
        Url = url.Trim();
        Tipo = tipo;
        PeriodicidadeMinutos = periodicidadeMinutos;
        Status = StatusFonte.Ativa;
        CriadoEm = DateTimeOffset.UtcNow;
        CriadoPorUsuarioId = criadoPorUsuarioId;
    }

    public Guid Id { get; private set; }
    public string Nome { get; private set; } = string.Empty;
    public string Url { get; private set; } = string.Empty;
    public TipoFonte Tipo { get; private set; }
    public StatusFonte Status { get; private set; }
    public int PeriodicidadeMinutos { get; private set; }
    public DateTimeOffset CriadoEm { get; private set; }
    public Guid CriadoPorUsuarioId { get; private set; }
    public DateTimeOffset? AlteradoEm { get; private set; }
    public Guid? AlteradoPorUsuarioId { get; private set; }

    public void Atualizar(
        string nome,
        string url,
        TipoFonte tipo,
        int periodicidadeMinutos,
        Guid usuarioId)
    {
        ValidarNome(nome);
        ValidarUrl(url);
        ValidarPeriodicidade(periodicidadeMinutos);

        Nome = nome.Trim();
        Url = url.Trim();
        Tipo = tipo;
        PeriodicidadeMinutos = periodicidadeMinutos;
        RegistrarAlteracao(usuarioId);
    }

    public void Ativar(Guid usuarioId)
    {
        Status = StatusFonte.Ativa;
        RegistrarAlteracao(usuarioId);
    }

    public void Desativar(Guid usuarioId)
    {
        Status = StatusFonte.Inativa;
        RegistrarAlteracao(usuarioId);
    }

    private void RegistrarAlteracao(Guid usuarioId)
    {
        if (usuarioId == Guid.Empty)
            throw new DomainException("O usuário responsável pela alteração é obrigatório.");

        AlteradoEm = DateTimeOffset.UtcNow;
        AlteradoPorUsuarioId = usuarioId;
    }

    private static void ValidarNome(string nome)
    {
        if (string.IsNullOrWhiteSpace(nome))
            throw new DomainException("O nome da fonte é obrigatório.");
    }

    private static void ValidarUrl(string url)
    {
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri) ||
            (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
        {
            throw new DomainException("A URL da fonte é inválida.");
        }
    }

    private static void ValidarPeriodicidade(int periodicidadeMinutos)
    {
        if (periodicidadeMinutos <= 0)
            throw new DomainException("A periodicidade deve ser maior que zero.");
    }
}
