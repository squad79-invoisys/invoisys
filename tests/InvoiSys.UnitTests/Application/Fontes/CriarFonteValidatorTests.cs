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
            30,
            "Legislação");

        var result = await _validator.ValidateAsync(
            request,
            TestContext.Current.CancellationToken);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task Validate_DeveSerInvalido_QuandoUrlNaoForHttpOuHttps()
    {
        var request = new CriarFonteRequest(
            "Portal Fiscal",
            "ftp://exemplo.com/feed.xml",
            TipoFonte.Rss,
            30,
            "Legislação");

        var result = await _validator.ValidateAsync(
            request,
            TestContext.Current.CancellationToken);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(error =>
            error.PropertyName == nameof(request.Url) &&
            error.ErrorMessage == "A URL deve usar http ou https");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task Validate_DeveRejeitarContextoVazio(string contexto)
    {
        var request = new CriarFonteRequest("Portal", "https://exemplo.com", TipoFonte.Rss, 30, contexto);
        (await _validator.ValidateAsync(request, TestContext.Current.CancellationToken)).IsValid.Should().BeFalse();
    }

    [Fact]
    public async Task Validate_DeveMedirContextoAposTrim()
    {
        var valido = new CriarFonteRequest("Portal", "https://exemplo.com", TipoFonte.Rss, 30, $"  {new string('A', 100)}  ");
        var invalido = valido with { Contexto = new string('A', 101) };
        (await _validator.ValidateAsync(valido, TestContext.Current.CancellationToken)).IsValid.Should().BeTrue();
        (await _validator.ValidateAsync(invalido, TestContext.Current.CancellationToken)).IsValid.Should().BeFalse();
    }

    [Fact]
    public async Task Validate_DeveFalhar_QuandoSeletorEmFontePdf()
    {
        var request = new CriarFonteRequest(
            "Manual NF-e",
            "https://exemplo.com/manual.pdf",
            TipoFonte.Pdf,
            30,
            "Manuais",
            "#conteudo");

        var result = await _validator.ValidateAsync(
            request,
            TestContext.Current.CancellationToken);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(error =>
            error.PropertyName == nameof(request.SeletorConteudo) &&
            error.ErrorMessage == "O seletor de conteúdo só pode ser usado em fontes do tipo WebHtml.");
    }

    [Fact]
    public async Task Validate_DeveSerValido_QuandoFontePdfSemSeletor()
    {
        var request = new CriarFonteRequest(
            "Manual NF-e",
            "https://exemplo.com/manual.pdf",
            TipoFonte.Pdf,
            30,
            "Manuais");

        var result = await _validator.ValidateAsync(
            request,
            TestContext.Current.CancellationToken);

        result.IsValid.Should().BeTrue();
    }
}
