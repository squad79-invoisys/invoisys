using InvoiSys.Domain.Enums;
using InvoiSys.Domain.Exceptions;

namespace InvoiSys.Domain.Entities;

public sealed class ExecucaoColeta
{
    private ExecucaoColeta()
    {
    }

    private ExecucaoColeta(
        Guid fonteId,
        TipoExecucao tipoExecucao,
        Guid? solicitadaPorUsuarioId)
    {
        if (fonteId == Guid.Empty)
            throw new DomainException("A fonte é obrigatória.");

        if (tipoExecucao == TipoExecucao.Manual &&
            (solicitadaPorUsuarioId is null || solicitadaPorUsuarioId == Guid.Empty))
        {
            throw new DomainException("A coleta manual precisa identificar o usuário solicitante.");
        }

        if (tipoExecucao == TipoExecucao.Automatica && solicitadaPorUsuarioId is not null)
            throw new DomainException("A coleta automática não deve possuir usuário solicitante.");

        Id = Guid.NewGuid();
        FonteId = fonteId;
        TipoExecucao = tipoExecucao;
        SolicitadaPorUsuarioId = solicitadaPorUsuarioId;
        Inicio = DateTimeOffset.UtcNow;
        Status = StatusExecucaoColeta.EmAndamento;
    }

    public Guid Id { get; private set; }
    public Guid FonteId { get; private set; }
    public DateTimeOffset Inicio { get; private set; }
    public DateTimeOffset? Fim { get; private set; }
    public StatusExecucaoColeta Status { get; private set; }
    public TipoExecucao TipoExecucao { get; private set; }
    public int QuantidadeDocumentos { get; private set; }
    public string? MensagemErro { get; private set; }
    public Guid? SolicitadaPorUsuarioId { get; private set; }

    public static ExecucaoColeta IniciarManual(Guid fonteId, Guid usuarioId) =>
        new(fonteId, TipoExecucao.Manual, usuarioId);

    public static ExecucaoColeta IniciarAutomatica(Guid fonteId) =>
        new(fonteId, TipoExecucao.Automatica, null);

    public void Concluir(int quantidadeDocumentos)
    {
        if (quantidadeDocumentos < 0)
            throw new DomainException("A quantidade de documentos não pode ser negativa.");

        QuantidadeDocumentos = quantidadeDocumentos;
        Status = StatusExecucaoColeta.Concluida;
        Fim = DateTimeOffset.UtcNow;
        MensagemErro = null;
    }

    public void RegistrarFalha(string mensagem)
    {
        if (string.IsNullOrWhiteSpace(mensagem))
            throw new DomainException("A mensagem da falha é obrigatória.");

        Status = StatusExecucaoColeta.Falhou;
        Fim = DateTimeOffset.UtcNow;
        MensagemErro = mensagem.Trim();
    }
}
