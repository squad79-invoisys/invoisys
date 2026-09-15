using InvoiSys.Domain.Enums;

namespace InvoiSys.Application.Common.Collectors;

public interface ICollectorResolver
{
    IContentCollector Resolver(TipoFonte tipo);
}
