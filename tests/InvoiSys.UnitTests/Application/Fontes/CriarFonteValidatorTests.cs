using FluentAssertions;
using InvoiSys.Application.Fontes.CriarFonte;
using InvoiSys.Domain.Enums;

namespace InvoiSys.UnitTests.Application.Fontes;

public sealed class CriarFonteValidatorTests
{
    private readonly CriarFonteValidator _validator = new();

    [Fact]
    public async Task Validate_DeveSerValido_QuandoRequestForCorreto()
    {
        var request = new CriarFonteRequest(
            "Portal Fiscal",
            "https://exemplo.com/feed.xml",
            TipoFonte.Rss,
            30);

        var result = await _validator.ValidateAsync(request);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task Validate_DeveSerInvalido_QuandoUrlNaoForHttpOuHttps()
    {
        var request = new CriarFonteRequest(
            "Portal Fiscal",
            "ftp://exemplo.com/feed.xml",
            TipoFonte.Rss,
            30);

        var result = await _validator.ValidateAsync(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(error => error.PropertyName == nameof(request.Url));
    }
}
