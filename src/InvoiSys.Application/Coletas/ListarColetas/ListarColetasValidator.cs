using FluentValidation;

namespace InvoiSys.Application.Coletas.ListarColetas;

public sealed class ListarColetasValidator : AbstractValidator<ListarColetasRequest>
{
    public ListarColetasValidator()
    {
        RuleFor(x => x.Pagina).GreaterThan(0);
        RuleFor(x => x.TamanhoPagina).InclusiveBetween(1, 100);
    }
}
