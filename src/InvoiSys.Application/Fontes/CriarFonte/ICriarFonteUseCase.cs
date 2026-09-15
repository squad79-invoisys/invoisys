namespace InvoiSys.Application.Fontes.CriarFonte;

public interface ICriarFonteUseCase
{
    Task<FonteResponse> ExecutarAsync(
        CriarFonteRequest request,
        CancellationToken cancellationToken);
}
