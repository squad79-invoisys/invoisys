using System.Text;
using FluentAssertions;
using InvoiSys.Application.Common.Anexos;
using InvoiSys.Application.Common.Exceptions;
using InvoiSys.Application.Documentos.Anexos;
using InvoiSys.Domain.Entities;
using InvoiSys.Domain.Repositories;

namespace InvoiSys.UnitTests.Application.Documentos;

public sealed class AnexosDocumentoTests
{
    private const string UrlDownload =
        "https://www.gov.br/receita/termo-no-01.pdf/@@download/file";

    [Theory]
    [InlineData("https://exemplo.com/a.pdf", true)]
    [InlineData("https://exemplo.com/A.PDF", true)]
    [InlineData("https://exemplo.com/a.pdf/@@download/file", true)]
    [InlineData("https://exemplo.com/a.pdf/@@display-file/file", true)]
    [InlineData("https://exemplo.com/pasta/planilha.xlsx?v=2", true)]
    [InlineData("https://exemplo.com/a.pdf/view", false)]
    [InlineData("https://exemplo.com/pagina.html", false)]
    [InlineData("https://exemplo.com/", false)]
    [InlineData("https://exemplo.com/@@download/file", false)]
    [InlineData("https://exemplo.com/robots.txt", false)]
    public void EhLinkDeArquivo_DeveReconhecerSoLinksQueEntregamArquivo(string url, bool esperado)
    {
        TiposArquivo.EhLinkDeArquivo(new Uri(url)).Should().Be(esperado);
    }

    [Theory]
    [InlineData("application/pdf", true)]
    [InlineData("image/png", true)]
    [InlineData("image/webp", true)]
    [InlineData("application/pdf; charset=binary", true)]
    [InlineData("image/svg+xml", false)]
    [InlineData("text/html", false)]
    [InlineData("application/vnd.openxmlformats-officedocument.wordprocessingml.document", false)]
    public void EhVisualizavel_DeveLiberarSoPdfEImagensSeguras(string tipo, bool esperado)
    {
        TiposArquivo.EhVisualizavel(tipo).Should().Be(esperado);
    }

    [Fact]
    public async Task ListarAsync_DeveMontarOAnexoDoGovBr()
    {
        var catalogo = new AnexoCatalogo(new DescobridorFake(
            new AnexoDescoberto(UrlDownload, "Termo de Apostilamento nº 01 2026.PDF", null)));

        var resposta = await catalogo.ListarAsync(CriarDocumento(), TestContext.Current.CancellationToken);

        resposta.VerificacaoCompleta.Should().BeTrue();
        var anexo = resposta.Itens.Should().ContainSingle().Subject;
        anexo.Nome.Should().Be("Termo de Apostilamento nº 01 2026.PDF");
        anexo.NomeArquivo.Should().Be("Termo de Apostilamento nº 01 2026.PDF");
        anexo.Extensao.Should().Be(".pdf");
        anexo.TipoConteudo.Should().Be("application/pdf");
        anexo.Visualizavel.Should().BeTrue();
        anexo.Url.Should().Be(UrlDownload);
        anexo.Id.Should().Be(AnexoCatalogo.GerarId(UrlDownload)).And.HaveLength(16);
    }

    [Fact]
    public async Task ListarAsync_DeveAcrescentarAExtensaoAoNomeDoArquivoQuandoFalta()
    {
        var catalogo = new AnexoCatalogo(new DescobridorFake(
            new AnexoDescoberto(UrlDownload, "Termo de apostilamento", null)));

        var anexo = (await catalogo.ListarAsync(
            CriarDocumento(), TestContext.Current.CancellationToken)).Itens.Single();

        anexo.Nome.Should().Be("Termo de apostilamento");
        anexo.NomeArquivo.Should().Be("Termo de apostilamento.pdf");
    }

    [Fact]
    public async Task ListarAsync_DeveDerivarONomeDaUrlQuandoNaoHaTexto()
    {
        var catalogo = new AnexoCatalogo(new DescobridorFake(
            new AnexoDescoberto("https://exemplo.com/docs/Relat%C3%B3rio%202026.pdf/@@download/file", null, null)));

        var anexo = (await catalogo.ListarAsync(
            CriarDocumento(), TestContext.Current.CancellationToken)).Itens.Single();

        anexo.Nome.Should().Be("Relatório 2026.pdf");
    }

    [Fact]
    public async Task ListarAsync_DeveTrocarCaracteresInvalidosNoNomeDoArquivo()
    {
        var catalogo = new AnexoCatalogo(new DescobridorFake(
            new AnexoDescoberto("https://exemplo.com/a.pdf", "Anexo: A/B \"final\"", null)));

        var anexo = (await catalogo.ListarAsync(
            CriarDocumento(), TestContext.Current.CancellationToken)).Itens.Single();

        anexo.NomeArquivo.Should().NotContainAny("/", "\\", ":", "\"");
        anexo.NomeArquivo.Should().EndWith(".pdf");
    }

    [Fact]
    public async Task ListarAsync_DeveIncluirEnclosuresDosMetadadosSemDuplicar()
    {
        const string metadados = """
            {"guid":"1","anexos":[
              {"url":"https://exemplo.com/edital.pdf","nome":"Edital","tipo":"application/pdf"},
              {"url":"https://exemplo.com/anexo.docx","nome":null,"tipo":null},
              {"url":"javascript:alert(1)","nome":"malicioso","tipo":null},
              {"url":"ftp://exemplo.com/x.pdf","nome":"ftp","tipo":null}]}
            """;
        var catalogo = new AnexoCatalogo(new DescobridorFake(
            new AnexoDescoberto("https://exemplo.com/edital.pdf", "Edital (repetido)", null)));

        var resposta = await catalogo.ListarAsync(
            CriarDocumento(metadados), TestContext.Current.CancellationToken);

        resposta.Itens.Select(a => a.Url).Should().Equal(
            "https://exemplo.com/edital.pdf",
            "https://exemplo.com/anexo.docx");
        resposta.Itens[0].Nome.Should().Be("Edital");
        resposta.Itens[1].Visualizavel.Should().BeFalse();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("não é json")]
    [InlineData("[1,2,3]")]
    [InlineData("""{"anexos":"texto"}""")]
    [InlineData("""{"anexos":[1,"a",null]}""")]
    public async Task ListarAsync_DeveIgnorarMetadadosForaDoFormato(string? metadados)
    {
        var catalogo = new AnexoCatalogo(new DescobridorFake());

        var resposta = await catalogo.ListarAsync(
            CriarDocumento(metadados), TestContext.Current.CancellationToken);

        resposta.Itens.Should().BeEmpty();
    }

    [Fact]
    public async Task ListarAsync_DeveRepassarQueAVerificacaoFicouIncompleta()
    {
        var catalogo = new AnexoCatalogo(new DescobridorFake(completa: false));

        var resposta = await catalogo.ListarAsync(
            CriarDocumento(), TestContext.Current.CancellationToken);

        resposta.VerificacaoCompleta.Should().BeFalse();
    }

    [Fact]
    public async Task ListarAsync_DeveLimitarAQuantidadeDeAnexos()
    {
        var muitos = Enumerable.Range(0, 50)
            .Select(i => new AnexoDescoberto($"https://exemplo.com/{i}.pdf", null, null))
            .ToArray();
        var catalogo = new AnexoCatalogo(new DescobridorFake(muitos));

        var resposta = await catalogo.ListarAsync(
            CriarDocumento(), TestContext.Current.CancellationToken);

        resposta.Itens.Should().HaveCount(AnexoCatalogo.LimiteAnexos);
    }

    [Fact]
    public async Task ListarAnexos_DeveLancarNotFound_QuandoDocumentoNaoExiste()
    {
        var useCase = new ListarAnexosDocumentoUseCase(
            new DocumentosStub(null),
            new AnexoCatalogo(new DescobridorFake()));

        var act = () => useCase.Execute(Guid.NewGuid(), TestContext.Current.CancellationToken);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task ListarAnexos_DeveConsultarAUrlDoDocumento()
    {
        var documento = CriarDocumento();
        var descobridor = new DescobridorFake(new AnexoDescoberto(UrlDownload, "Termo.pdf", null));
        var useCase = new ListarAnexosDocumentoUseCase(
            new DocumentosStub(documento), new AnexoCatalogo(descobridor));

        var resposta = await useCase.Execute(documento.Id, TestContext.Current.CancellationToken);

        resposta.Itens.Should().ContainSingle();
        descobridor.UrlsConsultadas.Should().Equal(documento.UrlOriginal);
    }

    [Fact]
    public async Task BaixarAnexo_DeveBaixarSoUmAnexoListadoParaODocumento()
    {
        var documento = CriarDocumento();
        var downloader = new DownloaderFake();
        var useCase = new BaixarAnexoDocumentoUseCase(
            new DocumentosStub(documento),
            new AnexoCatalogo(new DescobridorFake(new AnexoDescoberto(UrlDownload, "Termo", null))),
            downloader);

        await using var arquivo = await useCase.Execute(
            documento.Id,
            AnexoCatalogo.GerarId(UrlDownload),
            TestContext.Current.CancellationToken);

        downloader.UrlsBaixadas.Should().Equal(UrlDownload);
        downloader.NomesSugeridos.Should().Equal("Termo.pdf");
        arquivo.NomeArquivo.Should().Be("Termo.pdf");
    }

    [Fact]
    public async Task BaixarAnexo_NaoDeveBaixarIdentificadorQueNaoPertenceAoDocumento()
    {
        var documento = CriarDocumento();
        var downloader = new DownloaderFake();
        var useCase = new BaixarAnexoDocumentoUseCase(
            new DocumentosStub(documento),
            new AnexoCatalogo(new DescobridorFake(new AnexoDescoberto(UrlDownload, "Termo", null))),
            downloader);

        var act = () => useCase.Execute(
            documento.Id,
            AnexoCatalogo.GerarId("http://169.254.169.254/latest/meta-data/"),
            TestContext.Current.CancellationToken);

        await act.Should().ThrowAsync<NotFoundException>();
        downloader.UrlsBaixadas.Should().BeEmpty();
    }

    [Fact]
    public async Task BaixarAnexo_DeveLancarNotFound_QuandoDocumentoNaoExiste()
    {
        var useCase = new BaixarAnexoDocumentoUseCase(
            new DocumentosStub(null),
            new AnexoCatalogo(new DescobridorFake()),
            new DownloaderFake());

        var act = () => useCase.Execute(
            Guid.NewGuid(), "0123456789abcdef", TestContext.Current.CancellationToken);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    private static Documento CriarDocumento(string? metadados = null) =>
        new(Guid.NewGuid(), "Termo de apostilamento", "https://www.gov.br/receita/termo-no-01.pdf/view",
            "RSS", "www.gov.br", "Legislação", null, null, metadados, "hash");

    private sealed class DescobridorFake(
        bool completa,
        params AnexoDescoberto[] anexos) : IAnexoDescobridor
    {
        public DescobridorFake(params AnexoDescoberto[] anexos) : this(true, anexos)
        {
        }

        public List<string> UrlsConsultadas { get; } = [];

        public Task<ResultadoDescoberta> DescobrirAsync(string urlDocumento, CancellationToken cancellationToken)
        {
            UrlsConsultadas.Add(urlDocumento);
            return Task.FromResult(new ResultadoDescoberta(anexos, completa));
        }
    }

    private sealed class DownloaderFake : IAnexoDownloader
    {
        public List<string> UrlsBaixadas { get; } = [];
        public List<string?> NomesSugeridos { get; } = [];

        public Task<ArquivoAnexo> BaixarAsync(string url, string? nomeSugerido, CancellationToken cancellationToken)
        {
            UrlsBaixadas.Add(url);
            NomesSugeridos.Add(nomeSugerido);
            return Task.FromResult(new ArquivoAnexo(
                new MemoryStream(Encoding.ASCII.GetBytes("%PDF-")),
                "application/pdf",
                nomeSugerido ?? "anexo"));
        }
    }

    private sealed class DocumentosStub(Documento? documento) : IDocumentoRepository
    {
        public Task<int> InserirNovosAsync(IReadOnlyCollection<Documento> documentos, CancellationToken token) =>
            Task.FromResult(documentos.Count);

        public Task<Documento?> ObterPorIdAsync(Guid id, CancellationToken token) =>
            Task.FromResult(documento?.Id == id ? documento : null);

        public Task<(IReadOnlyList<Documento> Itens, int Total)> ListarAsync(int pagina, int tamanhoPagina,
            string? contexto, string? busca, string? tipo, DateTimeOffset? dataInicio, DateTimeOffset? dataFim,
            CancellationToken token) =>
            Task.FromResult<(IReadOnlyList<Documento>, int)>(([], 0));
    }
}
