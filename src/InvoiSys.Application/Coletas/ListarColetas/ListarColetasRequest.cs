using InvoiSys.Domain.Enums;

namespace InvoiSys.Application.Coletas.ListarColetas;

public sealed record ListarColetasRequest(
    int Pagina = 1,
    int TamanhoPagina = 20,
    Guid? FonteId = null,
    StatusExecucaoColeta? Status = null);
