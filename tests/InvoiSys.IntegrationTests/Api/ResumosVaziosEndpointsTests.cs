using System.Net;
using FluentAssertions;
using InvoiSys.Application.Coletas.ObterResumo;
using InvoiSys.Application.Documentos.ObterResumo;
using InvoiSys.Application.Fontes.ObterResumo;
using InvoiSys.IntegrationTests.Support;

namespace InvoiSys.IntegrationTests.Api;

[Collection("Aspire")]
public sealed class ResumosVaziosEndpointsTests(ApiFixture api) : IClassFixture<ApiFixture>
{
    [Fact]
    public async Task Resumos_DevemRetornarZeros_QuandoNaoExistiremRegistros()
    {
        var cancellationToken = TestContext.Current.CancellationToken;

        using var fontesResponse = await api.Consulta.GetAsync(
            "/api/fontes/resumo", cancellationToken);
        fontesResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var fontes = await fontesResponse.LerApiAsync<ResumoFontesResponse>(cancellationToken);
        fontes.Sucesso.Should().BeTrue();
        fontes.Dados.Should().Be(new ResumoFontesResponse(0, 0, 0));

        using var coletasResponse = await api.Consulta.GetAsync(
            "/api/coletas/resumo", cancellationToken);
        coletasResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var coletas = await coletasResponse.LerApiAsync<ResumoColetasResponse>(cancellationToken);
        coletas.Sucesso.Should().BeTrue();
        coletas.Dados.Should().Be(new ResumoColetasResponse(0, 0, 0, 0));

        using var documentosResponse = await api.Consulta.GetAsync(
            "/api/documentos/resumo", cancellationToken);
        documentosResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var documentos = await documentosResponse.LerApiAsync<ResumoDocumentosResponse>(cancellationToken);
        documentos.Sucesso.Should().BeTrue();
        documentos.Dados.Should().Be(new ResumoDocumentosResponse(0));
    }
}
