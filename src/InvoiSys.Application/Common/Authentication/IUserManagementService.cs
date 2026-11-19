namespace InvoiSys.Application.Common.Authentication;

public interface IUserManagementService
{
    Task<UserSummary> CriarAsync(
        string nome,
        string email,
        string senha,
        string perfil,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<UserSummary>> ListarAsync(
        CancellationToken cancellationToken);

    Task<UserSummary> DefinirStatusAsync(
        Guid usuarioId,
        bool ativo,
        CancellationToken cancellationToken);

    Task<UserSummary> RedefinirSenhaAsync(
        Guid usuarioId,
        string novaSenha,
        CancellationToken cancellationToken);

    // Exige a senha atual e encerra as outras sessões do usuário, mantendo a atual.
    Task<UserSummary> AlterarPropriaSenhaAsync(
        Guid usuarioId,
        string senhaAtual,
        string novaSenha,
        Guid? sessaoAtualId,
        CancellationToken cancellationToken);
}
