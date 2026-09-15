using InvoiSys.Application.Common.Responses;

namespace InvoiSys.Application.Coletas.ListarColetas;

public interface IListarColetasUseCase
{
    Task<PagedResponse<ExecucaoColetaResponse>> Execute(
        ListarColetasRequest request,
        CancellationToken cancellationToken);
}
