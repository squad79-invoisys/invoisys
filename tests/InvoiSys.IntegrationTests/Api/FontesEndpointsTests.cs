using System.Net;
using FluentAssertions;
using InvoiSys.Application.Common.Responses;
using InvoiSys.Application.Fontes;
using InvoiSys.Application.Fontes.AtualizarFonte;
using InvoiSys.Application.Fontes.CriarFonte;
using InvoiSys.Domain.Enums;
using InvoiSys.IntegrationTests.Support;

namespace InvoiSys.IntegrationTests.Api;

[Collection(ApiCollection.Name)]
public sealed class FontesEndpointsTests(ApiFixture api)
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    private static CriarFonteRequest NovaFonte(
        string nome,
        string url,
        TipoFonte tipo = TipoFonte.Rss,
        int periodicidadeMinutos = 60,
        string? seletor = null) =>
        new(nome, url, tipo, periodicidadeMinutos, seletor);

    [Fact]
    public async Task Criar_DeveRetornar201_QuandoFeedRssValido()
    {
        var token = ApiFixture.NovoToken();
        var url = api.Feed.RssUrl(token);

        using var response = await api.Admin.PostAsync(
            "/api/fontes",
            NovaFonte($"Fonte {token}", url),
            Ct);

        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var fonte = await response.LerDadosAsync<FonteResponse>(Ct);
        response.Headers.Location.Should().NotBeNull();
        response.Headers.Location!.ToString().Should().EndWith($"/api/fontes/{fonte.Id}");

        fonte.Id.Should().NotBeEmpty();
        fonte.Nome.Should().Be($"Fonte {token}");
        fonte.Url.Should().Be(url);
        fonte.Tipo.Should().Be(TipoFonte.Rss);
        fonte.Status.Should().Be(StatusFonte.Ativa);
        fonte.PeriodicidadeMinutos.Should().Be(60);
        fonte.CriadoPorUsuarioId.Should().Be(api.AdminId);
        fonte.AlteradoEm.Should().BeNull();
    }

    [Fact]
    public async Task Criar_DeveRetornar201_QuandoFeedAtomValido()
    {
        var token = ApiFixture.NovoToken();

        using var response = await api.Admin.PostAsync(
            "/api/fontes",
            NovaFonte($"Fonte {token}", api.Feed.AtomUrl(token), TipoFonte.Atom),
            Ct);

        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var fonte = await response.LerDadosAsync<FonteResponse>(Ct);
        fonte.Tipo.Should().Be(TipoFonte.Atom);
    }

    [Fact]
    public async Task Criar_DeveRetornar201_QuandoFonteWebHtmlTiverSeletor()
    {
        var token = ApiFixture.NovoToken();

        using var response = await api.Admin.PostAsync(
            "/api/fontes",
            NovaFonte(
                $"Fonte {token}",
                api.Feed.HtmlUrl(),
                TipoFonte.WebHtml,
                seletor: "article"),
            Ct);

        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var fonte = await response.LerDadosAsync<FonteResponse>(Ct);
        fonte.Tipo.Should().Be(TipoFonte.WebHtml);
        fonte.SeletorConteudo.Should().Be("article");
    }

    [Fact]
    public async Task Criar_DeveRetornar201_QuandoUsuarioForOperador()
    {
        var token = ApiFixture.NovoToken();

        using var response = await api.Operador.PostAsync(
            "/api/fontes",
            NovaFonte($"Fonte {token}", api.Feed.RssUrl(token)),
            Ct);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
    }

    [Fact]
    public async Task Criar_DeveRetornar400_QuandoUrlNaoForValida()
    {
        using var response = await api.Admin.PostAsync(
            "/api/fontes",
            NovaFonte("Fonte inválida", "isto-nao-e-uma-url"),
            Ct);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var corpo = await response.LerApiAsync<object>(Ct);
        corpo.Sucesso.Should().BeFalse();
        corpo.Mensagem.Should().Contain("URL");
    }

    [Fact]
    public async Task Criar_DeveRetornar400_QuandoUrlNaoUsarHttp()
    {
        using var response = await api.Admin.PostAsync(
            "/api/fontes",
            NovaFonte("Fonte inválida", "ftp://exemplo.invoisys.test/feed.xml"),
            Ct);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Criar_DeveRetornar400_QuandoUrlRetornarErroHttp()
    {
        using var response = await api.Admin.PostAsync(
            "/api/fontes",
            NovaFonte("Fonte inexistente", api.Feed.InexistenteUrl()),
            Ct);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var corpo = await response.LerApiAsync<object>(Ct);
        corpo.Mensagem.Should().Contain("resposta HTTP sem sucesso");
    }

    [Fact]
    public async Task Criar_DeveRetornar400_QuandoTipoDivergirDoFeed()
    {
        var token = ApiFixture.NovoToken();

        using var response = await api.Admin.PostAsync(
            "/api/fontes",
            NovaFonte($"Fonte {token}", api.Feed.RssUrl(token), TipoFonte.Atom),
            Ct);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var corpo = await response.LerApiAsync<object>(Ct);
        corpo.Mensagem.Should().Contain("RSS");
    }

    [Theory]
    [InlineData("", 60, null)]
    [InlineData("Fonte válida", 0, null)]
    [InlineData("Fonte válida", 43_201, null)]
    [InlineData("Fonte válida", 60, "article")]
    public async Task Criar_DeveRetornar400_QuandoDadosForemInvalidos(
        string nome,
        int periodicidadeMinutos,
        string? seletor)
    {
        var token = ApiFixture.NovoToken();

        using var response = await api.Admin.PostAsync(
            "/api/fontes",
            NovaFonte(
                nome,
                api.Feed.RssUrl(token),
                TipoFonte.Rss,
                periodicidadeMinutos,
                seletor),
            Ct);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await response.LerApiAsync<object>(Ct)).Sucesso.Should().BeFalse();
    }

    [Fact]
    public async Task Consultar_DeveRetornar200_QuandoFonteExistir()
    {
        var token = ApiFixture.NovoToken();
        var criada = await api.CriarFonteAsync(token, TipoFonte.Rss, Ct);

        using var response = await api.Consulta.GetAsync($"/api/fontes/{criada.Id}", Ct);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var fonte = await response.LerDadosAsync<FonteResponse>(Ct);
        fonte.Id.Should().Be(criada.Id);
        fonte.Nome.Should().Be(criada.Nome);
        fonte.Url.Should().Be(criada.Url);
    }

    [Fact]
    public async Task Consultar_DeveRetornar404_QuandoFonteNaoExistir()
    {
        using var response = await api.Admin.GetAsync(
            $"/api/fontes/{Guid.NewGuid()}",
            Ct);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);

        var corpo = await response.LerApiAsync<object>(Ct);
        corpo.Mensagem.Should().Contain("Fonte não encontrada");
    }

    [Fact]
    public async Task Listar_DeveFiltrarPorBusca_QuandoNomeContiverOTermo()
    {
        var tokenA = ApiFixture.NovoToken();
        var tokenB = ApiFixture.NovoToken();
        var fonteA = await api.CriarFonteAsync(tokenA, TipoFonte.Rss, Ct);
        await api.CriarFonteAsync(tokenB, TipoFonte.Rss, Ct);

        using var response = await api.Admin.GetAsync(
            $"/api/fontes?Busca={tokenA}",
            Ct);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var pagina = await response.LerDadosAsync<PagedResponse<FonteResponse>>(Ct);
        pagina.Total.Should().Be(1);
        pagina.Itens.Should().ContainSingle().Which.Id.Should().Be(fonteA.Id);
    }

    [Fact]
    public async Task Listar_DeveFiltrarPorTipoEStatus()
    {
        var token = ApiFixture.NovoToken();
        var rss = await api.CriarFonteAsync(token, TipoFonte.Rss, Ct);
        var atom = await api.CriarFonteAsync(token, TipoFonte.Atom, Ct);
        await api.DesativarFonteAsync(atom.Id, Ct);

        var porTipo = await ListarAsync($"Busca={token}&Tipo=Atom");
        porTipo.Itens.Should().ContainSingle().Which.Id.Should().Be(atom.Id);

        var inativas = await ListarAsync($"Busca={token}&Status=Inativa");
        inativas.Itens.Should().ContainSingle().Which.Id.Should().Be(atom.Id);

        var ativas = await ListarAsync($"Busca={token}&Status=Ativa");
        ativas.Itens.Should().ContainSingle().Which.Id.Should().Be(rss.Id);
    }

    [Fact]
    public async Task Listar_DevePaginarResultados()
    {
        var token = ApiFixture.NovoToken();

        for (var numero = 1; numero <= 3; numero++)
        {
            await api.CriarFonteComUrlAsync(
                $"Fonte {token} {numero}",
                api.Feed.RssUrl(token),
                TipoFonte.Rss,
                Ct);
        }

        var segundaPagina = await ListarAsync(
            $"Busca={token}&Pagina=2&TamanhoPagina=2");

        segundaPagina.Total.Should().Be(3);
        segundaPagina.Pagina.Should().Be(2);
        segundaPagina.TamanhoPagina.Should().Be(2);
        segundaPagina.TotalPaginas.Should().Be(2);
        segundaPagina.Itens.Should().ContainSingle()
            .Which.Nome.Should().Be($"Fonte {token} 3");
    }

    [Theory]
    [InlineData("Pagina=0")]
    [InlineData("TamanhoPagina=0")]
    [InlineData("TamanhoPagina=101")]
    public async Task Listar_DeveRetornar400_QuandoPaginacaoForInvalida(string query)
    {
        using var response = await api.Admin.GetAsync($"/api/fontes?{query}", Ct);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Atualizar_DeveRetornar200_QuandoDadosValidos()
    {
        var token = ApiFixture.NovoToken();
        var criada = await api.CriarFonteAsync(token, TipoFonte.Rss, Ct);

        var request = new AtualizarFonteRequest(
            $"Fonte renomeada {token}",
            criada.Url,
            TipoFonte.Rss,
            120);

        using var response = await api.Operador.PutAsync(
            $"/api/fontes/{criada.Id}",
            request,
            Ct);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var atualizada = await response.LerDadosAsync<FonteResponse>(Ct);
        atualizada.Nome.Should().Be($"Fonte renomeada {token}");
        atualizada.PeriodicidadeMinutos.Should().Be(120);
        atualizada.AlteradoEm.Should().NotBeNull();
        atualizada.AlteradoPorUsuarioId.Should().Be(api.OperadorId);

        using var consulta = await api.Admin.GetAsync($"/api/fontes/{criada.Id}", Ct);
        var persistida = await consulta.LerDadosAsync<FonteResponse>(Ct);
        persistida.Nome.Should().Be($"Fonte renomeada {token}");
        persistida.PeriodicidadeMinutos.Should().Be(120);
    }

    [Fact]
    public async Task Atualizar_DeveRetornar400_QuandoNovaUrlForInvalida()
    {
        var criada = await api.CriarFonteAsync(
            ApiFixture.NovoToken(),
            TipoFonte.Rss,
            Ct);

        var request = new AtualizarFonteRequest(
            criada.Nome,
            api.Feed.InexistenteUrl(),
            TipoFonte.Rss,
            60);

        using var response = await api.Admin.PutAsync(
            $"/api/fontes/{criada.Id}",
            request,
            Ct);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Atualizar_DeveRetornar400_QuandoPeriodicidadeForInvalida()
    {
        var criada = await api.CriarFonteAsync(
            ApiFixture.NovoToken(),
            TipoFonte.Rss,
            Ct);

        var request = new AtualizarFonteRequest(criada.Nome, criada.Url, TipoFonte.Rss, 0);

        using var response = await api.Admin.PutAsync(
            $"/api/fontes/{criada.Id}",
            request,
            Ct);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Atualizar_DeveRetornar404_QuandoFonteNaoExistir()
    {
        var token = ApiFixture.NovoToken();
        var request = new AtualizarFonteRequest(
            "Fonte fantasma",
            api.Feed.RssUrl(token),
            TipoFonte.Rss,
            60);

        using var response = await api.Admin.PutAsync(
            $"/api/fontes/{Guid.NewGuid()}",
            request,
            Ct);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Desativar_DeveAlterarStatus_QuandoFonteExistir()
    {
        var criada = await api.CriarFonteAsync(
            ApiFixture.NovoToken(),
            TipoFonte.Rss,
            Ct);

        using (var desativar = await api.Operador.PatchAsync(
                   $"/api/fontes/{criada.Id}/desativar",
                   null,
                   Ct))
        {
            desativar.StatusCode.Should().Be(HttpStatusCode.OK);
            (await desativar.LerApiAsync<object>(Ct)).Sucesso.Should().BeTrue();
        }

        (await ConsultarAsync(criada.Id)).Status.Should().Be(StatusFonte.Inativa);

        using (var ativar = await api.Operador.PatchAsync(
                   $"/api/fontes/{criada.Id}/ativar",
                   null,
                   Ct))
        {
            ativar.StatusCode.Should().Be(HttpStatusCode.OK);
        }

        var reativada = await ConsultarAsync(criada.Id);
        reativada.Status.Should().Be(StatusFonte.Ativa);
        reativada.AlteradoEm.Should().NotBeNull();
    }

    [Theory]
    [InlineData("ativar")]
    [InlineData("desativar")]
    public async Task AlterarStatus_DeveRetornar404_QuandoFonteNaoExistir(string acao)
    {
        using var response = await api.Admin.PatchAsync(
            $"/api/fontes/{Guid.NewGuid()}/{acao}",
            null,
            Ct);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    private async Task<PagedResponse<FonteResponse>> ListarAsync(string query)
    {
        using var response = await api.Admin.GetAsync($"/api/fontes?{query}", Ct);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        return await response.LerDadosAsync<PagedResponse<FonteResponse>>(Ct);
    }

    private async Task<FonteResponse> ConsultarAsync(Guid id)
    {
        using var response = await api.Admin.GetAsync($"/api/fontes/{id}", Ct);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        return await response.LerDadosAsync<FonteResponse>(Ct);
    }
}
