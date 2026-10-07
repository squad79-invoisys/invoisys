using FluentAssertions;
using InvoiSys.Application.Usuarios.CriarUsuario;

namespace InvoiSys.UnitTests.Application.Usuarios;

public sealed class CriarUsuarioValidatorTests
{
    private readonly CriarUsuarioValidator _validator = new();

    [Fact]
    public async Task Validate_DeveSerValido_QuandoSenhaAtenderPolitica()
    {
        var request = CriarRequest("Senha@Forte1");

        var result = await _validator.ValidateAsync(
            request,
            TestContext.Current.CancellationToken);

        result.IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData("Sen@1a", "A senha deve possuir pelo menos 10 caracteres.")]
    [InlineData("Senha@Forte", "A senha deve possuir número.")]
    [InlineData("senha@forte1", "A senha deve possuir letra maiúscula.")]
    [InlineData("SENHA@FORTE1", "A senha deve possuir letra minúscula.")]
    [InlineData("SenhaForte1", "A senha deve possuir caractere especial.")]
    public async Task Validate_DeveRetornarMensagemDoRequisito_QuandoSenhaNaoAtenderPolitica(
        string senha,
        string mensagemEsperada)
    {
        var request = CriarRequest(senha);

        var result = await _validator.ValidateAsync(
            request,
            TestContext.Current.CancellationToken);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(error =>
            error.PropertyName == nameof(request.Senha) &&
            error.ErrorMessage == mensagemEsperada);
    }

    private static CriarUsuarioRequest CriarRequest(string senha) =>
        new("Maria Silva", "maria@exemplo.com", senha, "Operador");
}
