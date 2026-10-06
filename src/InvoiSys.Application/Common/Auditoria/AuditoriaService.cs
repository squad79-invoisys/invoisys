using InvoiSys.Application.Common.Abstractions;
using InvoiSys.Domain.Entities;
using InvoiSys.Domain.Enums;
using InvoiSys.Domain.Repositories;

namespace InvoiSys.Application.Common.Auditoria;

public sealed class AuditoriaService(
    ICurrentUser currentUser,
    IContextoRequisicao contextoRequisicao,
    IRegistroAuditoriaRepository registroAuditoriaRepository) : IAuditoriaService
{
    public const string ResponsavelSistema = "Sistema";

    public Task RegistrarAsync(
        TipoEventoAuditoria tipo,
        string atividade,
        string objeto,
        Guid? objetoId,
        CancellationToken cancellationToken) =>
        RegistrarAsync(
            tipo,
            atividade,
            objeto,
            objetoId,
            currentUser.UsuarioId,
            string.IsNullOrWhiteSpace(currentUser.Email)
                ? ResponsavelSistema
                : currentUser.Email,
            cancellationToken);

    public Task RegistrarAsync(
        TipoEventoAuditoria tipo,
        string atividade,
        string objeto,
        Guid? objetoId,
        Guid? usuarioId,
        string responsavel,
        CancellationToken cancellationToken)
    {
        var registro = new RegistroAuditoria(
            tipo,
            atividade,
            objeto,
            objetoId,
            usuarioId,
            responsavel,
            contextoRequisicao.EnderecoIp);

        return registroAuditoriaRepository.AdicionarAsync(registro, cancellationToken);
    }
}
