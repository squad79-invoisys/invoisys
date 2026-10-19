using FluentAssertions;
using InvoiSys.Application.Fontes.AtualizarFonte;
using InvoiSys.Domain.Enums;

namespace InvoiSys.UnitTests.Application.Fontes;

public sealed class AtualizarFonteValidatorTests
{
    private readonly AtualizarFonteValidator _validator = new();

    [Theory]
    [InlineData("http://exemplo.com/feed.xml")]
    [InlineData("https://exemplo.com/feed.xml")]
    public async Task Validate_DeveAceitarUrlHttpOuHttps(string url)
    {
        var request = new AtualizarFonteRequest(
            "Portal Fiscal",
            url,
            TipoFonte.Rss,
            30,
            "Legislação");

        var result = await _validator.ValidateAsync(
            request,
            TestContext.Current.CancellationToken);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task Validate_DeveUsarMensagemObrigatoria_QuandoEsquemaNaoForHttp()
    {
        var request = new AtualizarFonteRequest(
            "Portal Fiscal",
            "ftp://exemplo.com/feed.xml",
            TipoFonte.Rss,
            30,
            "Legislação");

        var result = await _validator.ValidateAsync(
            request,
            TestContext.Current.CancellationToken);

        result.Errors.Should().ContainSingle(error =>
            error.PropertyName == nameof(request.Url) &&
            error.ErrorMessage == "A URL deve usar http ou https");
    }

    [Fact]
    public async Task Validate_DeveMedirContextoAposTrim()
    {
        var valido = new AtualizarFonteRequest("Portal", "https://exemplo.com", TipoFonte.Rss, 30, $"  {new string('A', 100)}  ");
        var invalido = valido with { Contexto = new string('A', 101) };
        (await _validator.ValidateAsync(valido, TestContext.Current.CancellationToken)).IsValid.Should().BeTrue();
        (await _validator.ValidateAsync(invalido, TestContext.Current.CancellationToken)).IsValid.Should().BeFalse();
        (await _validator.ValidateAsync(valido with { Contexto = "   " }, TestContext.Current.CancellationToken)).IsValid.Should().BeFalse();
    }
}
