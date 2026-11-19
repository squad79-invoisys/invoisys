using FluentAssertions;
using InvoiSys.Application.Usuarios.AlterarPropriaSenha;

namespace InvoiSys.UnitTests.Application.Usuarios;

public sealed class AlterarPropriaSenhaValidatorTests
{
    private readonly AlterarPropriaSenhaValidator _validator = new();

    [Fact]
    public async Task Validate_DeveSerValido_QuandoSenhaNovaAtenderPolitica()
    {
        var request = new AlterarPropriaSenhaRequest("Senha@Atual1", "Senha@Nova22");

        var result = await _validator.ValidateAsync(
            request,
            TestContext.Current.CancellationToken);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task Validate_DeveExigirSenhaAtual()
    {
        var request = new AlterarPropriaSenhaRequest("", "Senha@Nova22");

        var result = await _validator.ValidateAsync(
            request,
            TestContext.Current.CancellationToken);

        result.Errors.Should().Contain(error =>
            error.PropertyName == nameof(request.SenhaAtual) &&
            error.ErrorMessage == "Informe a senha atual.");
    }

    [Fact]
    public async Task Validate_DeveRecusarSenhaNovaIgualAAtual()
    {
        var request = new AlterarPropriaSenhaRequest("Senha@Atual1", "Senha@Atual1");

        var result = await _validator.ValidateAsync(
            request,
            TestContext.Current.CancellationToken);

        result.Errors.Should().Contain(error =>
            error.PropertyName == nameof(request.NovaSenha) &&
            error.ErrorMessage == "A nova senha deve ser diferente da atual.");
    }

    [Theory]
    [InlineData("Sen@1a", "A senha deve possuir pelo menos 10 caracteres.")]
    [InlineData("SenhaForte1", "A senha deve possuir caractere especial.")]
    public async Task Validate_DeveAplicarPoliticaDeSenha_NaSenhaNova(
        string senhaNova,
        string mensagemEsperada)
    {
        var request = new AlterarPropriaSenhaRequest("Senha@Atual1", senhaNova);

        var result = await _validator.ValidateAsync(
            request,
            TestContext.Current.CancellationToken);

        result.Errors.Should().Contain(error =>
            error.PropertyName == nameof(request.NovaSenha) &&
            error.ErrorMessage == mensagemEsperada);
    }
}
