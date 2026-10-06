using FluentValidation;

namespace InvoiSys.Application.Historico;

internal static class FiltroHistoricoValidator
{
    public static void AplicarRegrasDeFiltro<T>(this AbstractValidator<T> validator)
        where T : FiltroHistoricoRequest
    {
        validator.RuleFor(x => x.Tipo)
            .IsInEnum()
            .When(x => x.Tipo is not null);

        validator.RuleFor(x => x.Busca)
            .MaximumLength(100);

        validator.RuleFor(x => x)
            .Must(x => x.De is null || x.Ate is null || x.De <= x.Ate)
            .WithName("Periodo")
            .WithMessage("A data inicial deve ser anterior ou igual à data final.");
    }
}
