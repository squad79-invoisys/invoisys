namespace InvoiSys.Application.Fontes.ConsultarFonte;

public interface IConsultarFonteUseCase
{
    Task<FonteResponse> Execute(
        Guid id,
        CancellationToken cancellationToken);
}
