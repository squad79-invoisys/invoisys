using FluentValidation;

namespace InvoiSys.Application.Autenticacao.Login;

public sealed class LoginValidator : AbstractValidator<LoginRequest>
{
    public LoginValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty()
            .EmailAddress()
            .MaximumLength(256);

        RuleFor(x => x.Senha)
            .NotEmpty()
            .MaximumLength(128);
    }
}
