using InvoiSys.Application.Common.Authentication;
using Refit;
using Responses = InvoiSys.Application.Common.Responses;

namespace InvoiSys.Web.Clients;

public interface IUsuariosClient
{
    [Get("/api/usuarios")]
    Task<Responses.ApiResponse<IReadOnlyList<UserSummary>>> ListarAsync(
        CancellationToken cancellationToken = default);

    [Post("/api/usuarios")]
    Task<Responses.ApiResponse<bool>> CriarAsync(
        [Body] CriarUsuarioRequest request, 
        CancellationToken cancellationToken = default);

    [Patch("/api/usuarios/{id}/status")]
    Task<Responses.ApiResponse<bool>> AlterarStatusAsync(
        Guid id, 
        [Body] AlterarStatusRequest request, 
        CancellationToken cancellationToken = default);

    [Put("/api/usuarios/{id}/senha")]
    Task<Responses.ApiResponse<bool>> RedefinirSenhaAsync(
        Guid id, 
        [Body] RedefinirSenhaRequest request, 
        CancellationToken cancellationToken = default);
}

// DTOs de envio
public record CriarUsuarioRequest(string Nome, string Email, string Senha, string Perfil);
public record AlterarStatusRequest(bool Ativo);
public record RedefinirSenhaRequest(string NovaSenha);