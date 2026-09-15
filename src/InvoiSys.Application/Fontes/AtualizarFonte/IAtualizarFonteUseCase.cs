namespace InvoiSys.Application.Fontes.AtualizarFonte;

public interface IAtualizarFonteUseCase
{
    Task<FonteResponse> Execute(
        Guid id,
        AtualizarFonteRequest request,
        CancellationToken cancellationToken);
}
