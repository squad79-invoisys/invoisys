using InvoiSys.Domain.Entities;
using InvoiSys.Domain.Enums;

namespace InvoiSys.Application.Historico;

public sealed record RegistroAuditoriaResponse(
    Guid Id,
    TipoEventoAuditoria Tipo,
    string Atividade,
    string Objeto,
    Guid? ObjetoId,
    Guid? UsuarioId,
    string Responsavel,
    string? EnderecoIp,
    DateTimeOffset OcorridoEm)
{
    public static RegistroAuditoriaResponse FromEntity(RegistroAuditoria registro) =>
        new(
            registro.Id,
            registro.Tipo,
            registro.Atividade,
            registro.Objeto,
            registro.ObjetoId,
            registro.UsuarioId,
            registro.Responsavel,
            registro.EnderecoIp,
            registro.OcorridoEm);
}
