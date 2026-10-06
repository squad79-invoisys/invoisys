using FluentValidation;

namespace InvoiSys.Application.Common.Validation;

public static class SenhaRuleExtensions
{
    public static IRuleBuilderOptions<T, string> SenhaForte<T>(
        this IRuleBuilder<T, string> ruleBuilder) =>
        ruleBuilder
            .NotEmpty().WithMessage("A senha é obrigatória.")
            .MinimumLength(10).WithMessage("A senha deve possuir pelo menos 10 caracteres.")
            .MaximumLength(128).WithMessage("A senha deve possuir no máximo 128 caracteres.")
            .Matches("[A-Z]").WithMessage("A senha deve possuir letra maiúscula.")
            .Matches("[a-z]").WithMessage("A senha deve possuir letra minúscula.")
            .Matches("[0-9]").WithMessage("A senha deve possuir número.")
            .Matches("[^a-zA-Z0-9]").WithMessage("A senha deve possuir caractere especial.");
}
