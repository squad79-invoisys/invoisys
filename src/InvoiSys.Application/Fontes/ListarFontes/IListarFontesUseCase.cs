using InvoiSys.Application.Common.Responses;

namespace InvoiSys.Application.Fontes.ListarFontes;

public interface IListarFontesUseCase
{
    Task<PagedResponse<FonteResponse>> Execute(
        ListarFontesRequest request,
        CancellationToken cancellationToken);
}
