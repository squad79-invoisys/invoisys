using FluentValidation;
using InvoiSys.Application.Common.Validation;

namespace InvoiSys.Application.Usuarios.CriarUsuario;

public sealed class CriarUsuarioValidator : AbstractValidator<CriarUsuarioRequest>
{
    private static readonly string[] PerfisPermitidos =
        ["Administrador", "Operador", "Consulta"];

    public CriarUsuarioValidator()
    {
        RuleFor(x => x.Nome).NotEmpty().MaximumLength(150);
        RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(256);
        RuleFor(x => x.Senha).SenhaForte();

        RuleFor(x => x.Perfil)
            .Must(perfil => PerfisPermitidos.Contains(perfil, StringComparer.OrdinalIgnoreCase))
            .WithMessage("Perfil inválido.");
    }
}
