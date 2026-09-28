using FluentValidation;

namespace InvoiSys.Application.Fontes.AtualizarFonte;

public sealed class AtualizarFonteValidator : AbstractValidator<AtualizarFonteRequest>
{
    public AtualizarFonteValidator()
    {
        RuleFor(x => x.Nome).NotEmpty().MaximumLength(150);
        RuleFor(x => x.Url)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
            .WithMessage("A URL é obrigatória.")
            .MaximumLength(2048)
            .Must(url => Uri.TryCreate(url.Trim(), UriKind.Absolute, out _))
            .WithMessage("Informe uma URL válida")
            .Must(url =>
                Uri.TryCreate(url.Trim(), UriKind.Absolute, out var uri) &&
                (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps))
            .WithMessage("A URL deve usar http ou https");
        RuleFor(x => x.Tipo).IsInEnum();
        RuleFor(x => x.PeriodicidadeMinutos).GreaterThan(0).LessThanOrEqualTo(43_200);
    }
}
