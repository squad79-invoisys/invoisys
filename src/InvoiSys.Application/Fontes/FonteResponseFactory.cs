using InvoiSys.Domain.Entities;
using InvoiSys.Domain.Repositories;

namespace InvoiSys.Application.Fontes;

internal static class FonteResponseFactory
{
    /// <summary>Monta a resposta de uma fonte incluindo a última e a próxima coleta.</summary>
    public static async Task<FonteResponse> CriarAsync(
        IFonteRepository fonteRepository,
        Fonte fonte,
        CancellationToken cancellationToken)
    {
        var ultimas = await fonteRepository.ObterUltimasColetasAsync([fonte.Id], cancellationToken);
        return FonteResponse.FromEntity(
            fonte,
            ultimas.TryGetValue(fonte.Id, out var ultima) ? ultima : null);
    }
}
