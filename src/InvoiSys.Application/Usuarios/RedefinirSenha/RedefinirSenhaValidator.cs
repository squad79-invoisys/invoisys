using FluentValidation;

namespace InvoiSys.Application.Usuarios.RedefinirSenha;

public sealed class RedefinirSenhaValidator : AbstractValidator<RedefinirSenhaRequest>
{
    public RedefinirSenhaValidator()
    {
        RuleFor(x => x.NovaSenha)
            .NotEmpty()
            .MinimumLength(10)
            .MaximumLength(128)
            .Matches("[A-Z]").WithMessage("A senha deve possuir letra maiúscula.")
            .Matches("[a-z]").WithMessage("A senha deve possuir letra minúscula.")
            .Matches("[0-9]").WithMessage("A senha deve possuir número.");
    }
}
