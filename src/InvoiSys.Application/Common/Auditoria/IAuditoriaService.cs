using InvoiSys.Domain.Enums;

namespace InvoiSys.Application.Common.Auditoria;

/// <summary>
/// Adiciona eventos de auditoria à unidade de trabalho atual.
/// O registro só é gravado quando o use case chama <c>IUnitOfWork.CommitAsync</c>.
/// </summary>
public interface IAuditoriaService
{
    /// <summary>Registra o evento em nome do usuário autenticado.</summary>
    Task RegistrarAsync(
        TipoEventoAuditoria tipo,
        string atividade,
        string objeto,
        Guid? objetoId,
        CancellationToken cancellationToken);

    /// <summary>Registra o evento informando o responsável explicitamente (ex.: login).</summary>
    Task RegistrarAsync(
        TipoEventoAuditoria tipo,
        string atividade,
        string objeto,
        Guid? objetoId,
        Guid? usuarioId,
        string responsavel,
        CancellationToken cancellationToken);
}
