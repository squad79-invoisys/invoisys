using FluentAssertions;
using InvoiSys.Application.Documentos.ListarDocumentos;

namespace InvoiSys.UnitTests.Application.Documentos;

public sealed class ListarDocumentosValidatorTests
{
    private readonly ListarDocumentosValidator _validator = new();

    [Fact]
    public async Task Validate_DeveAceitar_QuandoPeriodoValido()
    {
        var request = new ListarDocumentosRequest(
            Tipo: "WEB",
            DataInicio: new DateTimeOffset(2026, 10, 1, 0, 0, 0, TimeSpan.Zero),
            DataFim: new DateTimeOffset(2026, 10, 1, 0, 0, 0, TimeSpan.Zero));

        var result = await _validator.ValidateAsync(request, TestContext.Current.CancellationToken);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task Validate_DeveRejeitar_QuandoTipoExcedeLimite()
    {
        var request = new ListarDocumentosRequest(Tipo: new string('A', 51));

        var result = await _validator.ValidateAsync(request, TestContext.Current.CancellationToken);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(erro => erro.PropertyName == nameof(ListarDocumentosRequest.Tipo));
    }

    [Fact]
    public async Task Validate_DeveRejeitar_QuandoDataFimAnteriorADataInicio()
    {
        var request = new ListarDocumentosRequest(
            DataInicio: new DateTimeOffset(2026, 10, 15, 0, 0, 0, TimeSpan.Zero),
            DataFim: new DateTimeOffset(2026, 10, 14, 0, 0, 0, TimeSpan.Zero));

        var result = await _validator.ValidateAsync(request, TestContext.Current.CancellationToken);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(erro => erro.PropertyName == nameof(ListarDocumentosRequest.DataFim));
    }
}
