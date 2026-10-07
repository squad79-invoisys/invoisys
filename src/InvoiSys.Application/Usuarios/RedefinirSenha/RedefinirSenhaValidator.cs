using FluentValidation;
using InvoiSys.Application.Common.Validation;

namespace InvoiSys.Application.Usuarios.RedefinirSenha;

public sealed class RedefinirSenhaValidator : AbstractValidator<RedefinirSenhaRequest>
{
    public RedefinirSenhaValidator()
    {
        RuleFor(x => x.NovaSenha).SenhaForte();
    }
}
