using InvoiSys.Domain.Entities;
using InvoiSys.Domain.Enums;

namespace InvoiSys.Application.Fontes;

public sealed record FonteResponse(
    Guid Id,
    string Nome,
    string Url,
    TipoFonte Tipo,
    StatusFonte Status,
    int PeriodicidadeMinutos,
    string Contexto,
    string? SeletorConteudo,
    DateTimeOffset CriadoEm,
    Guid CriadoPorUsuarioId,
    DateTimeOffset? AlteradoEm,
    Guid? AlteradoPorUsuarioId,
    bool AgendamentoPausado = false,
    int FalhasConsecutivas = 0,
    DateTimeOffset? UltimaColetaEm = null,
    DateTimeOffset? ProximaColetaEm = null)
{
    /// <param name="ultimaColetaEm">Fim (ou início) da execução mais recente da fonte, se houver.</param>
    public static FonteResponse FromEntity(Fonte fonte, DateTimeOffset? ultimaColetaEm = null) =>
        new(
            fonte.Id,
            fonte.Nome,
            fonte.Url,
            fonte.Tipo,
            fonte.Status,
            fonte.PeriodicidadeMinutos,
            fonte.Contexto,
            fonte.SeletorConteudo,
            fonte.CriadoEm,
            fonte.CriadoPorUsuarioId,
            fonte.AlteradoEm,
            fonte.AlteradoPorUsuarioId,
            fonte.AgendamentoPausado,
            fonte.FalhasConsecutivas,
            ultimaColetaEm,
            fonte.CalcularProximaColeta(ultimaColetaEm));
}
