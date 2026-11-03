using System.Net;
using System.Security.Cryptography;
using System.Text;
using FluentAssertions;
using InvoiSys.Application.Common.Exceptions;
using InvoiSys.Collectors.Pdf;
using InvoiSys.Domain.Enums;
using UglyToad.PdfPig.Content;
using UglyToad.PdfPig.Core;
using UglyToad.PdfPig.Fonts.Standard14Fonts;
using UglyToad.PdfPig.Writer;

namespace InvoiSys.UnitTests.Collectors;

public sealed class PdfCollectorTests
{
    private const string Url = "https://www.nfe.fazenda.gov.br/arquivos/Manual%20NF-e%207.0.pdf";

    [Theory]
    [InlineData(TipoFonte.Pdf, true)]
    [InlineData(TipoFonte.Rss, false)]
    [InlineData(TipoFonte.Atom, false)]
    [InlineData(TipoFonte.WebHtml, false)]
    public void Suporta_DeveAtenderApenasPdf(TipoFonte tipo, bool esperado) =>
        CriarCollector(CriarPdf("x")).Suporta(tipo).Should().Be(esperado);

    [Fact]
    public async Task ColetarAsync_DeveExtrairTextoDoPdf_QuandoHaTexto()
    {
        var collector = CriarCollector(CriarPdf("Nota tecnica 2026.001", "Leiaute da NF-e"));

        var documentos = await collector.ColetarAsync(Url, null, TestContext.Current.CancellationToken);

        documentos.Should().ContainSingle();
        documentos[0].ConteudoTextual.Should().Contain("Nota tecnica 2026.001");
        documentos[0].ConteudoTextual.Should().Contain("Leiaute da NF-e");
        documentos[0].Tipo.Should().Be("PDF");
        documentos[0].UrlOriginal.Should().Be(Url);
        documentos[0].Metadados.Should().Contain("\"paginas\":2");
    }

    [Fact]
    public async Task ColetarAsync_DeveUsarTituloDosMetadados_QuandoInformado()
    {
        var collector = CriarCollector(CriarPdfComTitulo("Manual de Orientacao do Contribuinte", "Conteudo"));

        var documentos = await collector.ColetarAsync(Url, null, TestContext.Current.CancellationToken);

        documentos[0].Titulo.Should().Be("Manual de Orientacao do Contribuinte");
    }

    [Fact]
    public async Task ColetarAsync_DeveUsarNomeDoArquivo_QuandoSemTituloNosMetadados()
    {
        var collector = CriarCollector(CriarPdf("Conteudo"));

        var documentos = await collector.ColetarAsync(Url, null, TestContext.Current.CancellationToken);

        documentos[0].Titulo.Should().Be("Manual NF-e 7.0");
    }

    [Fact]
    public async Task ColetarAsync_DeveGerarHashSha256_ParaOConteudo()
    {
        var collector = CriarCollector(CriarPdfComTitulo("Manual", "Mesmo texto"));

        var documentos = await collector.ColetarAsync(Url, null, TestContext.Current.CancellationToken);

        var documento = documentos[0];
        var esperado = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(
            $"{documento.Titulo}|{Url}|{documento.ConteudoTextual}")));
        documento.Hash.Should().Be(esperado);
    }

    [Fact]
    public async Task ColetarAsync_DeveLancarBusinessException_QuandoPdfSemTexto()
    {
        var collector = CriarCollector(CriarPdf());

        var acao = async () =>
            await collector.ColetarAsync(Url, null, TestContext.Current.CancellationToken);

        (await acao.Should().ThrowAsync<BusinessException>())
            .WithMessage("*não possui texto extraível*escaneado*");
    }

    [Fact]
    public async Task ColetarAsync_DeveLancarBusinessException_QuandoConteudoNaoEPdf()
    {
        var collector = CriarCollector(Encoding.UTF8.GetBytes("<html><body>Página</body></html>"));

        var acao = async () =>
            await collector.ColetarAsync(Url, null, TestContext.Current.CancellationToken);

        (await acao.Should().ThrowAsync<BusinessException>())
            .WithMessage("O conteúdo da URL não é um arquivo PDF válido.");
    }

    [Fact]
    public async Task ColetarAsync_DeveLancarHttpRequestException_QuandoRespostaSemSucesso()
    {
        var collector = CriarCollector(CriarPdf("Conteudo"), HttpStatusCode.NotFound);

        var acao = async () =>
            await collector.ColetarAsync(Url, null, TestContext.Current.CancellationToken);

        await acao.Should().ThrowAsync<HttpRequestException>();
    }

    [Fact]
    public async Task ColetarAsync_DeveLancarBusinessException_QuandoContentLengthExcedeLimite()
    {
        var conteudo = new ByteArrayContent(CriarPdf("Conteudo"));
        conteudo.Headers.ContentLength = PdfCollector.TamanhoMaximoBytes + 1;
        var collector = new PdfCollector(new HttpClient(new ConteudoFixoHandler(conteudo)));

        var acao = async () =>
            await collector.ColetarAsync(Url, null, TestContext.Current.CancellationToken);

        (await acao.Should().ThrowAsync<BusinessException>())
            .WithMessage("O PDF excede o tamanho máximo de 50 MB.");
    }

    [Fact]
    public async Task ColetarAsync_DeveLancarBusinessException_QuandoConteudoSemTamanhoExcedeLimite()
    {
        var conteudo = new StreamContent(new FluxoDeZeros(PdfCollector.TamanhoMaximoBytes + 1));
        var collector = new PdfCollector(new HttpClient(new ConteudoFixoHandler(conteudo)));

        var acao = async () =>
            await collector.ColetarAsync(Url, null, TestContext.Current.CancellationToken);

        (await acao.Should().ThrowAsync<BusinessException>())
            .WithMessage("O PDF excede o tamanho máximo de 50 MB.");
    }

    private static byte[] CriarPdf(params string[] paginas) =>
        CriarPdfComTitulo(null, paginas);

    private static byte[] CriarPdfComTitulo(string? titulo, params string[] paginas)
    {
        var builder = new PdfDocumentBuilder();
        builder.DocumentInformation.Title = titulo;
        var fonte = builder.AddStandard14Font(Standard14Font.Helvetica);

        if (paginas.Length == 0)
            builder.AddPage(PageSize.A4);

        foreach (var texto in paginas)
            builder.AddPage(PageSize.A4).AddText(texto, 12, new PdfPoint(25, 700), fonte);

        return builder.Build();
    }

    private static PdfCollector CriarCollector(
        byte[] conteudo,
        HttpStatusCode status = HttpStatusCode.OK) =>
        new(new HttpClient(new RespostaFixaHandler(conteudo, status)));

    private sealed class RespostaFixaHandler(
        byte[] conteudo,
        HttpStatusCode status) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken) =>
            Task.FromResult(new HttpResponseMessage(status)
            {
                Content = new ByteArrayContent(conteudo)
            });
    }

    private sealed class ConteudoFixoHandler(HttpContent conteudo) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken) =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = conteudo
            });
    }

    private sealed class FluxoDeZeros(long tamanho) : Stream
    {
        private long posicao;

        public override bool CanRead => true;
        public override bool CanSeek => false;
        public override bool CanWrite => false;
        public override long Length => throw new NotSupportedException();

        public override long Position
        {
            get => posicao;
            set => throw new NotSupportedException();
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            var lidos = (int)Math.Min(count, tamanho - posicao);
            Array.Clear(buffer, offset, lidos);
            posicao += lidos;
            return lidos;
        }

        public override void Flush()
        {
        }

        public override long Seek(long offset, SeekOrigin origin) =>
            throw new NotSupportedException();

        public override void SetLength(long value) =>
            throw new NotSupportedException();

        public override void Write(byte[] buffer, int offset, int count) =>
            throw new NotSupportedException();
    }
}
