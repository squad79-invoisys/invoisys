using FluentAssertions;
using InvoiSys.Application.Usuarios.RedefinirSenha;

namespace InvoiSys.UnitTests.Application.Usuarios;

public sealed class RedefinirSenhaValidatorTests
{
    private readonly RedefinirSenhaValidator _validator = new();

    [Fact]
    public async Task Validate_DeveSerValido_QuandoSenhaAtenderPolitica()
    {
        var request = new RedefinirSenhaRequest("Senha@Forte1");

        var result = await _validator.ValidateAsync(
            request,
            TestContext.Current.CancellationToken);

        result.IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData("", "A senha é obrigatória.")]
    [InlineData("Sen@1a", "A senha deve possuir pelo menos 10 caracteres.")]
    [InlineData("Senha@Forte", "A senha deve possuir número.")]
    [InlineData("senha@forte1", "A senha deve possuir letra maiúscula.")]
    [InlineData("SENHA@FORTE1", "A senha deve possuir letra minúscula.")]
    [InlineData("SenhaForte1", "A senha deve possuir caractere especial.")]
    public async Task Validate_DeveRetornarMensagemDoRequisito_QuandoSenhaNaoAtenderPolitica(
        string senha,
        string mensagemEsperada)
    {
        var request = new RedefinirSenhaRequest(senha);

        var result = await _validator.ValidateAsync(
            request,
            TestContext.Current.CancellationToken);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(error =>
            error.PropertyName == nameof(request.NovaSenha) &&
            error.ErrorMessage == mensagemEsperada);
    }
}
