using FluentValidation;

namespace InvoiSys.Application.Fontes.ListarFontes;

public sealed class ListarFontesValidator : AbstractValidator<ListarFontesRequest>
{
    public ListarFontesValidator()
    {
        RuleFor(x => x.Pagina).GreaterThan(0);
        RuleFor(x => x.TamanhoPagina).InclusiveBetween(1, 100);
        RuleFor(x => x.Busca).MaximumLength(150);
    }
}
