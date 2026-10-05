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
        Guid criadoPorUsuarioId,
        string? seletorConteudo = null)
    {
        ValidarNome(nome);
        ValidarUrl(url);
        ValidarPeriodicidade(periodicidadeMinutos);
        ValidarSeletorConteudo(seletorConteudo, tipo);

        if (criadoPorUsuarioId == Guid.Empty)
            throw new DomainException("O usuário responsável pelo cadastro é obrigatório.");

        Id = Guid.NewGuid();
        Nome = nome.Trim();
        Url = url.Trim();
        Tipo = tipo;
        PeriodicidadeMinutos = periodicidadeMinutos;
        SeletorConteudo = NormalizarSeletor(seletorConteudo);
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

    /// <summary>
    /// Seletor CSS opcional que delimita o conteúdo principal da página.
    /// Vale apenas para fontes do tipo WebHtml. Quando vazio, o coletor usa a
    /// extração padrão de article, main ou body.
    /// </summary>
    public string? SeletorConteudo { get; private set; }

    public DateTimeOffset CriadoEm { get; private set; }
    public Guid CriadoPorUsuarioId { get; private set; }
    public DateTimeOffset? AlteradoEm { get; private set; }
    public Guid? AlteradoPorUsuarioId { get; private set; }

    public void Atualizar(
        string nome,
        string url,
        TipoFonte tipo,
        int periodicidadeMinutos,
        Guid usuarioId,
        string? seletorConteudo = null)
    {
        ValidarNome(nome);
        ValidarUrl(url);
        ValidarPeriodicidade(periodicidadeMinutos);
        ValidarSeletorConteudo(seletorConteudo, tipo);

        Nome = nome.Trim();
        Url = url.Trim();
        Tipo = tipo;
        PeriodicidadeMinutos = periodicidadeMinutos;
        SeletorConteudo = NormalizarSeletor(seletorConteudo);
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

    private static void ValidarSeletorConteudo(string? seletorConteudo, TipoFonte tipo)
    {
        if (string.IsNullOrWhiteSpace(seletorConteudo))
            return;

        if (tipo != TipoFonte.WebHtml)
        {
            throw new DomainException(
                "O seletor de conteúdo só pode ser usado em fontes do tipo WebHtml.");
        }

        if (seletorConteudo.Trim().Length > 200)
            throw new DomainException("O seletor de conteúdo deve ter no máximo 200 caracteres.");
    }

    private static string? NormalizarSeletor(string? seletorConteudo) =>
        string.IsNullOrWhiteSpace(seletorConteudo) ? null : seletorConteudo.Trim();
}
