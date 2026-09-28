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
            30);

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
            30);

        var result = await _validator.ValidateAsync(
            request,
            TestContext.Current.CancellationToken);

        result.Errors.Should().ContainSingle(error =>
            error.PropertyName == nameof(request.Url) &&
            error.ErrorMessage == "A URL deve usar http ou https");
    }
}
