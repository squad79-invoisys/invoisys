using FluentValidation;

namespace InvoiSys.Application.Fontes.ListarFontes;

public sealed class ListarFontesValidator : AbstractValidator<ListarFontesRequest>
{
    public ListarFontesValidator()
    {
        RuleFor(x => x.Pagina).GreaterThan(0);
        RuleFor(x => x.TamanhoPagina).InclusiveBetween(1, 100);
        RuleFor(x => x.Busca).MaximumLength(150);
        RuleFor(x => x.Contexto)
            .Must(contexto => contexto is null || contexto.Trim().Length <= 100)
            .WithMessage("O contexto deve ter no máximo 100 caracteres.");
    }
}
