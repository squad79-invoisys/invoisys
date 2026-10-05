using InvoiSys.Domain.Enums;

namespace InvoiSys.Application.Common.Fontes;

public interface IFonteUrlValidator
{
    Task ValidarAsync(
        string url,
        TipoFonte tipo,
        CancellationToken cancellationToken);
}
