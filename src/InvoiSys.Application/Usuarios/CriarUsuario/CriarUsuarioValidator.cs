using FluentValidation;

namespace InvoiSys.Application.Usuarios.CriarUsuario;

public sealed class CriarUsuarioValidator : AbstractValidator<CriarUsuarioRequest>
{
    private static readonly string[] PerfisPermitidos =
        ["Administrador", "Operador", "Consulta"];

    public CriarUsuarioValidator()
    {
        RuleFor(x => x.Nome).NotEmpty().MaximumLength(150);
        RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(256);
        RuleFor(x => x.Senha)
            .NotEmpty()
            .MinimumLength(10)
            .MaximumLength(128)
            .Matches("[A-Z]").WithMessage("A senha deve possuir letra maiúscula.")
            .Matches("[a-z]").WithMessage("A senha deve possuir letra minúscula.")
            .Matches("[0-9]").WithMessage("A senha deve possuir número.");

        RuleFor(x => x.Perfil)
            .Must(perfil => PerfisPermitidos.Contains(perfil, StringComparer.OrdinalIgnoreCase))
            .WithMessage("Perfil inválido.");
    }
}
