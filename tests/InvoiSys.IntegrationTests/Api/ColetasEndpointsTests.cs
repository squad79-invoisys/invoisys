using System.Net;
using FluentAssertions;
using InvoiSys.Application.Coletas;
using InvoiSys.Application.Common.Responses;
using InvoiSys.Domain.Enums;
using InvoiSys.IntegrationTests.Support;

namespace InvoiSys.IntegrationTests.Api;

[Collection(ApiCollection.Name)]
public sealed class ColetasEndpointsTests(ApiFixture api)
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    [Fact]
    public async Task Executar_DeveConcluirColetaManual_QuandoFonteRssAtiva()
    {
        var fonte = await api.CriarFonteAsync(
            ApiFixture.NovoToken(),
            TipoFonte.Rss,
            Ct);

        using var response = await api.Admin.PostAsync(
            $"/api/coletas/fontes/{fonte.Id}/executar",
            null,
            Ct);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var execucao = await response.LerDadosAsync<ExecucaoColetaResponse>(Ct);
        execucao.Id.Should().NotBeEmpty();
        execucao.FonteId.Should().Be(fonte.Id);
        execucao.Status.Should().Be(StatusExecucaoColeta.Concluida);
        execucao.TipoExecucao.Should().Be(TipoExecucao.Manual);
        execucao.QuantidadeDocumentos.Should().Be(2);
        execucao.SolicitadaPorUsuarioId.Should().Be(api.AdminId);
        execucao.Fim.Should().NotBeNull();
        execucao.MensagemErro.Should().BeNull();
    }

    [Fact]
    public async Task Executar_DeveColetarEntradas_QuandoFonteAtom()
    {
        var fonte = await api.CriarFonteAsync(
            ApiFixture.NovoToken(),
            TipoFonte.Atom,
            Ct);

        using var response = await api.Admin.PostAsync(
            $"/api/coletas/fontes/{fonte.Id}/executar",
            null,
            Ct);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var execucao = await response.LerDadosAsync<ExecucaoColetaResponse>(Ct);
        execucao.Status.Should().Be(StatusExecucaoColeta.Concluida);
        execucao.QuantidadeDocumentos.Should().Be(1);
    }

    [Fact]
    public async Task Executar_DevePermitirColeta_QuandoUsuarioForOperador()
    {
        var fonte = await api.CriarFonteAsync(
            ApiFixture.NovoToken(),
            TipoFonte.Rss,
            Ct);

        using var response = await api.Operador.PostAsync(
            $"/api/coletas/fontes/{fonte.Id}/executar",
            null,
            Ct);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var execucao = await response.LerDadosAsync<ExecucaoColetaResponse>(Ct);
        execucao.SolicitadaPorUsuarioId.Should().Be(api.OperadorId);
    }

    [Fact]
    public async Task Executar_DeveRetornar404_QuandoFonteNaoExistir()
    {
        using var response = await api.Admin.PostAsync(
            $"/api/coletas/fontes/{Guid.NewGuid()}/executar",
            null,
            Ct);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Executar_DeveRetornar400_QuandoFonteEstiverInativa()
    {
        var fonte = await api.CriarFonteAsync(
            ApiFixture.NovoToken(),
            TipoFonte.Rss,
            Ct);
        await api.DesativarFonteAsync(fonte.Id, Ct);

        using var response = await api.Admin.PostAsync(
            $"/api/coletas/fontes/{fonte.Id}/executar",
            null,
            Ct);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var corpo = await response.LerApiAsync<object>(Ct);
        corpo.Mensagem.Should().Contain("precisa estar ativa");
    }

    [Fact]
    public async Task Executar_DeveRegistrarFalha_QuandoFeedDeixarDeResponder()
    {
        var token = ApiFixture.NovoToken();
        var fonte = await api.CriarFonteComUrlAsync(
            $"Fonte instável {token}",
            api.Feed.InstavelUrl(token),
            TipoFonte.Rss,
            Ct);

        using var response = await api.Admin.PostAsync(
            $"/api/coletas/fontes/{fonte.Id}/executar",
            null,
            Ct);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var corpo = await response.LerApiAsync<object>(Ct);
        corpo.Sucesso.Should().BeFalse();
        corpo.Mensagem.Should().Contain("A coleta não pôde ser concluída");

        var pagina = await ListarAsync($"FonteId={fonte.Id}");
        var execucao = pagina.Itens.Should().ContainSingle().Subject;
        execucao.Status.Should().Be(StatusExecucaoColeta.Falhou);
        execucao.QuantidadeDocumentos.Should().Be(0);
        execucao.MensagemErro.Should().NotBeNullOrWhiteSpace();
        execucao.Fim.Should().NotBeNull();
    }

    [Fact]
    public async Task Consultar_DeveRetornar200_QuandoColetaExistir()
    {
        var fonte = await api.CriarFonteAsync(
            ApiFixture.NovoToken(),
            TipoFonte.Rss,
            Ct);
        var executada = await api.ExecutarColetaAsync(fonte.Id, Ct);

        using var response = await api.Consulta.GetAsync(
            $"/api/coletas/{executada.Id}",
            Ct);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var execucao = await response.LerDadosAsync<ExecucaoColetaResponse>(Ct);
        execucao.Id.Should().Be(executada.Id);
        execucao.FonteId.Should().Be(fonte.Id);
        execucao.Status.Should().Be(StatusExecucaoColeta.Concluida);
        execucao.QuantidadeDocumentos.Should().Be(2);
    }

    [Fact]
    public async Task Consultar_DeveRetornar404_QuandoColetaNaoExistir()
    {
        using var response = await api.Admin.GetAsync(
            $"/api/coletas/{Guid.NewGuid()}",
            Ct);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);

        var corpo = await response.LerApiAsync<object>(Ct);
        corpo.Mensagem.Should().Contain("Execução de coleta não encontrada");
    }

    [Fact]
    public async Task Listar_DeveFiltrarPorFonteEOrdenarDaMaisRecente()
    {
        var fonte = await api.CriarFonteAsync(
            ApiFixture.NovoToken(),
            TipoFonte.Rss,
            Ct);
        var primeira = await api.ExecutarColetaAsync(fonte.Id, Ct);
        var segunda = await api.ExecutarColetaAsync(fonte.Id, Ct);

        var pagina = await ListarAsync($"FonteId={fonte.Id}");

        pagina.Total.Should().Be(2);
        pagina.Itens.Select(execucao => execucao.Id)
            .Should().Equal(segunda.Id, primeira.Id);
    }

    [Fact]
    public async Task Listar_DeveFiltrarPorStatus()
    {
        var fonte = await api.CriarFonteAsync(
            ApiFixture.NovoToken(),
            TipoFonte.Rss,
            Ct);
        var concluida = await api.ExecutarColetaAsync(fonte.Id, Ct);

        var concluidas = await ListarAsync(
            $"FonteId={fonte.Id}&Status=Concluida");
        concluidas.Itens.Should().ContainSingle().Which.Id.Should().Be(concluida.Id);

        var falhas = await ListarAsync($"FonteId={fonte.Id}&Status=Falhou");
        falhas.Total.Should().Be(0);
        falhas.Itens.Should().BeEmpty();
    }

    [Fact]
    public async Task Listar_DevePaginarResultados()
    {
        var fonte = await api.CriarFonteAsync(
            ApiFixture.NovoToken(),
            TipoFonte.Rss,
            Ct);

        for (var tentativa = 0; tentativa < 3; tentativa++)
            await api.ExecutarColetaAsync(fonte.Id, Ct);

        var pagina = await ListarAsync(
            $"FonteId={fonte.Id}&Pagina=2&TamanhoPagina=2");

        pagina.Total.Should().Be(3);
        pagina.TotalPaginas.Should().Be(2);
        pagina.Itens.Should().HaveCount(1);
    }

    [Theory]
    [InlineData("Pagina=0")]
    [InlineData("TamanhoPagina=0")]
    [InlineData("TamanhoPagina=101")]
    public async Task Listar_DeveRetornar400_QuandoPaginacaoForInvalida(string query)
    {
        using var response = await api.Admin.GetAsync($"/api/coletas?{query}", Ct);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    private async Task<PagedResponse<ExecucaoColetaResponse>> ListarAsync(
        string query)
    {
        using var response = await api.Admin.GetAsync($"/api/coletas?{query}", Ct);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        return await response.LerDadosAsync<PagedResponse<ExecucaoColetaResponse>>(Ct);
    }
}
