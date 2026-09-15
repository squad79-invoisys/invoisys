using FluentValidation;

namespace InvoiSys.Application.Fontes.CriarFonte;

public sealed class CriarFonteValidator : AbstractValidator<CriarFonteRequest>
{
    public CriarFonteValidator()
    {
        RuleFor(x => x.Nome)
            .NotEmpty()
            .MaximumLength(150);

        RuleFor(x => x.Url)
            .NotEmpty()
            .MaximumLength(2048)
            .Must(url => Uri.TryCreate(url, UriKind.Absolute, out var uri) &&
                         (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps))
            .WithMessage("Informe uma URL HTTP ou HTTPS válida.");

        RuleFor(x => x.Tipo)
            .IsInEnum();

        RuleFor(x => x.PeriodicidadeMinutos)
            .GreaterThan(0)
            .LessThanOrEqualTo(43_200);
    }
}
