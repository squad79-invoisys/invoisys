namespace InvoiSys.Application.Fontes.ImportarFontes;

public interface IImportarFontesUseCase
{
    Task<ImportarFontesResponse> ExecutarAsync(
        Stream arquivo,
        CancellationToken cancellationToken);
}
