using InvoiSys.Domain.Enums;
using InvoiSys.Domain.Exceptions;

namespace InvoiSys.Domain.Entities;

/// <summary>
/// Registro imutável de uma ação relevante realizada na plataforma.
/// O nome do responsável e a descrição do objeto são gravados no momento
/// do evento para que o histórico não mude quando a fonte ou o usuário mudar.
/// </summary>
public sealed class RegistroAuditoria
{
    public const int TamanhoMaximoAtividade = 150;
    public const int TamanhoMaximoObjeto = 300;
    public const int TamanhoMaximoResponsavel = 256;
    public const int TamanhoMaximoEnderecoIp = 45;

    private RegistroAuditoria()
    {
    }

    public RegistroAuditoria(
        TipoEventoAuditoria tipo,
        string atividade,
        string objeto,
        Guid? objetoId,
        Guid? usuarioId,
        string responsavel,
        string? enderecoIp)
    {
        if (!Enum.IsDefined(tipo))
            throw new DomainException("O tipo do evento de auditoria é inválido.");

        if (string.IsNullOrWhiteSpace(atividade))
            throw new DomainException("A atividade do evento de auditoria é obrigatória.");

        if (string.IsNullOrWhiteSpace(objeto))
            throw new DomainException("O objeto do evento de auditoria é obrigatório.");

        if (string.IsNullOrWhiteSpace(responsavel))
            throw new DomainException("O responsável pelo evento de auditoria é obrigatório.");

        Id = Guid.NewGuid();
        Tipo = tipo;
        Atividade = Limitar(atividade, TamanhoMaximoAtividade);
        Objeto = Limitar(objeto, TamanhoMaximoObjeto);
        ObjetoId = objetoId == Guid.Empty ? null : objetoId;
        UsuarioId = usuarioId == Guid.Empty ? null : usuarioId;
        Responsavel = Limitar(responsavel, TamanhoMaximoResponsavel);
        EnderecoIp = string.IsNullOrWhiteSpace(enderecoIp)
            ? null
            : Limitar(enderecoIp, TamanhoMaximoEnderecoIp);
        OcorridoEm = DateTimeOffset.UtcNow;
    }

    public Guid Id { get; private set; }
    public TipoEventoAuditoria Tipo { get; private set; }
    public string Atividade { get; private set; } = string.Empty;
    public string Objeto { get; private set; } = string.Empty;
    public Guid? ObjetoId { get; private set; }
    public Guid? UsuarioId { get; private set; }
    public string Responsavel { get; private set; } = string.Empty;
    public string? EnderecoIp { get; private set; }
    public DateTimeOffset OcorridoEm { get; private set; }

    private static string Limitar(string valor, int tamanhoMaximo)
    {
        var normalizado = valor.Trim();
        return normalizado.Length <= tamanhoMaximo
            ? normalizado
            : normalizado[..tamanhoMaximo];
    }
}
