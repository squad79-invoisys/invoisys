using InvoiSys.Application.Common.Anexos;

namespace InvoiSys.Application.Documentos.Anexos;

public interface IBaixarAnexoDocumentoUseCase
{
    Task<ArquivoAnexo> Execute(
        Guid documentoId,
        string anexoId,
        CancellationToken cancellationToken);
}
