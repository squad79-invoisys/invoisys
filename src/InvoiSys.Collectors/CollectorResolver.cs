using InvoiSys.Application.Common.Collectors;
using InvoiSys.Application.Common.Exceptions;
using InvoiSys.Domain.Enums;

namespace InvoiSys.Collectors;

public sealed class CollectorResolver(
    IEnumerable<IContentCollector> collectors) : ICollectorResolver
{
    public IContentCollector Resolver(TipoFonte tipo)
    {
        var collector = collectors.FirstOrDefault(item => item.Suporta(tipo));

        return collector
            ?? throw new BusinessException($"Não existe coletor configurado para o tipo {tipo}.");
    }
}
