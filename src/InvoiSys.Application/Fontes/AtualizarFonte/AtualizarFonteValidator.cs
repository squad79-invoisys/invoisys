using FluentValidation;
using InvoiSys.Domain.Enums;

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

        RuleFor(x => x.Contexto)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
            .Must(contexto => !string.IsNullOrWhiteSpace(contexto))
            .WithMessage("O contexto da fonte é obrigatório.")
            .Must(contexto => contexto.Trim().Length <= 100)
            .WithMessage("O contexto da fonte deve ter no máximo 100 caracteres.");

        RuleFor(x => x.SeletorConteudo)
            .MaximumLength(200)
            .When(x => !string.IsNullOrWhiteSpace(x.SeletorConteudo));

        RuleFor(x => x.SeletorConteudo)
            .Empty()
            .When(x => x.Tipo != TipoFonte.WebHtml)
            .WithMessage("O seletor de conteúdo só pode ser usado em fontes do tipo WebHtml.");
    }
}
