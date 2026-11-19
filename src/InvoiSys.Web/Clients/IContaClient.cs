using InvoiSys.Application.Usuarios.AlterarPropriaSenha;
using Refit;
using Responses = InvoiSys.Application.Common.Responses;

namespace InvoiSys.Web.Clients;

public interface IContaClient
{
    [Put("/api/conta/senha")]
    Task<Responses.ApiResponse<object>> AlterarSenhaAsync(
        [Body] AlterarPropriaSenhaRequest request,
        CancellationToken cancellationToken = default);
}
