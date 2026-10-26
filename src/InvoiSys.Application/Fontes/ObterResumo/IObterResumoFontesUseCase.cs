namespace InvoiSys.Application.Fontes.ObterResumo;

public interface IObterResumoFontesUseCase
{
    Task<ResumoFontesResponse> Execute(CancellationToken cancellationToken);
}
