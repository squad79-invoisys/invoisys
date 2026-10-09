using System.Net;
using FluentAssertions;
using InvoiSys.Application.Common.Responses;
using InvoiSys.Application.Dashboard.ObterResumo;
using InvoiSys.Domain.Enums;
using InvoiSys.IntegrationTests.Support;

namespace InvoiSys.IntegrationTests.Api;

/// <summary>
/// As asserções comparam o resumo antes e depois de cada ação (deltas), porque o
/// banco é compartilhado entre os testes da coleção.
/// </summary>
[Collection(ApiCollection.Name)]
public sealed class DashboardEndpointsTests(ApiFixture api)
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    [Fact]
    public async Task Resumo_DeveRetornar200ComContagensCoerentes()
    {
        using var response = await api.Consulta.GetAsync("/api/dashboard/resumo", Ct);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var corpo = await response.LerApiAsync<ResumoDashboardResponse>(Ct);
        corpo.Sucesso.Should().BeTrue();

        var resumo = corpo.Dados;
        resumo.Should().NotBeNull();
        resumo!.FontesAtivas.Should().BeGreaterThanOrEqualTo(0);
        resumo.FontesInativas.Should().BeGreaterThanOrEqualTo(0);
        resumo.ColetasComFalhaUltimas24h.Should().BeLessThanOrEqualTo(resumo.ColetasUltimas24h);
        resumo.ExecucoesRecentes.Count.Should().BeLessThanOrEqualTo(5);
        resumo.DocumentosRecentes.Count.Should().BeLessThanOrEqualTo(5);
    }

    [Fact]
    public async Task Resumo_DeveRefletirNovaFonteEColeta_QuandoColetaForConcluida()
    {
        var antes = await api.ObterResumoAsync(Ct);

        var token = ApiFixture.NovoToken();
        var fonte = await api.CriarFonteAsync(token, TipoFonte.Rss, Ct);
        var execucao = await api.ExecutarColetaAsync(fonte.Id, Ct);

        var depois = await api.ObterResumoAsync(Ct);

        depois.FontesAtivas.Should().Be(antes.FontesAtivas + 1);
        depois.FontesInativas.Should().Be(antes.FontesInativas);
        depois.ColetasUltimas24h.Should().Be(antes.ColetasUltimas24h + 1);
        depois.ColetasComFalhaUltimas24h.Should().Be(antes.ColetasComFalhaUltimas24h);
        depois.TotalDocumentos.Should().Be(antes.TotalDocumentos + 2);

        depois.ExecucoesRecentes.Should().Contain(recente =>
            recente.Id == execucao.Id &&
            recente.FonteId == fonte.Id &&
            recente.FonteNome == fonte.Nome &&
            recente.Status == StatusExecucaoColeta.Concluida &&
            recente.QuantidadeDocumentos == 2);

        depois.DocumentosRecentes.Should().Contain(documento =>
            documento.Titulo.Contains(token));
    }

    [Fact]
    public async Task Resumo_DeveMoverFonteParaInativas_QuandoFonteForDesativada()
    {
        var fonte = await api.CriarFonteAsync(
            ApiFixture.NovoToken(),
            TipoFonte.Rss,
            Ct);
        var antes = await api.ObterResumoAsync(Ct);

        await api.DesativarFonteAsync(fonte.Id, Ct);

        var depois = await api.ObterResumoAsync(Ct);

        depois.FontesAtivas.Should().Be(antes.FontesAtivas - 1);
        depois.FontesInativas.Should().Be(antes.FontesInativas + 1);
    }

    [Fact]
    public async Task Resumo_DeveContarColetaComFalha_QuandoFeedDeixarDeResponder()
    {
        var token = ApiFixture.NovoToken();
        var fonte = await api.CriarFonteComUrlAsync(
            $"Fonte instável {token}",
            api.Feed.InstavelUrl(token),
            TipoFonte.Rss,
            Ct);
        var antes = await api.ObterResumoAsync(Ct);

        using (var coleta = await api.Admin.PostAsync(
                   $"/api/coletas/fontes/{fonte.Id}/executar",
                   null,
                   Ct))
        {
            coleta.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }

        var depois = await api.ObterResumoAsync(Ct);

        depois.ColetasUltimas24h.Should().Be(antes.ColetasUltimas24h + 1);
        depois.ColetasComFalhaUltimas24h.Should().Be(antes.ColetasComFalhaUltimas24h + 1);
        depois.TotalDocumentos.Should().Be(antes.TotalDocumentos);
        depois.ExecucoesRecentes.Should().Contain(recente =>
            recente.FonteId == fonte.Id &&
            recente.Status == StatusExecucaoColeta.Falhou);
    }
}
