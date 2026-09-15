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
    DateTimeOffset CriadoEm,
    Guid CriadoPorUsuarioId,
    DateTimeOffset? AlteradoEm,
    Guid? AlteradoPorUsuarioId)
{
    public static FonteResponse FromEntity(Fonte fonte) =>
        new(
            fonte.Id,
            fonte.Nome,
            fonte.Url,
            fonte.Tipo,
            fonte.Status,
            fonte.PeriodicidadeMinutos,
            fonte.CriadoEm,
            fonte.CriadoPorUsuarioId,
            fonte.AlteradoEm,
            fonte.AlteradoPorUsuarioId);
}
