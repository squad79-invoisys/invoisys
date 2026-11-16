using FluentValidation;
using InvoiSys.Application.Common.Validation;

namespace InvoiSys.Application.Usuarios.AlterarPropriaSenha;

public sealed class AlterarPropriaSenhaValidator : AbstractValidator<AlterarPropriaSenhaRequest>
{
    public AlterarPropriaSenhaValidator()
    {
        RuleFor(x => x.SenhaAtual)
            .NotEmpty().WithMessage("Informe a senha atual.");

        RuleFor(x => x.NovaSenha)
            .SenhaForte()
            .NotEqual(x => x.SenhaAtual).WithMessage("A nova senha deve ser diferente da atual.");
    }
}
