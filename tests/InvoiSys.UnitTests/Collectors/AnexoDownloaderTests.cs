using System.Net;
using System.Text;
using FluentAssertions;
using InvoiSys.Application.Common.Exceptions;
using InvoiSys.Collectors.Anexos;

namespace InvoiSys.UnitTests.Collectors;

public sealed class AnexoDownloaderTests
{
    private static readonly byte[] Pdf = Encoding.ASCII.GetBytes("%PDF-1.7 conteudo");

    [Fact]
    public async Task BaixarAsync_DeveEntregarOConteudoComNomeDoServidor()
    {
        var downloader = Criar(_ => RespostasStub.Arquivo(
            Pdf,
            disposicao: "attachment; filename*=UTF-8''Termo%20n%C2%BA%2001.PDF"));

        await using var arquivo = await downloader.BaixarAsync(
            "https://exemplo.com/a.pdf/@@download/file",
            "sugerido.pdf",
            TestContext.Current.CancellationToken);

        arquivo.NomeArquivo.Should().Be("Termo nº 01.PDF");
        arquivo.TipoConteudo.Should().Be("application/pdf");
        (await LerTudoAsync(arquivo.Conteudo)).Should().Equal(Pdf);
    }

    [Fact]
    public async Task BaixarAsync_DeveUsarNomeSugeridoQuandoOServidorNaoInforma()
    {
        var downloader = Criar(_ => RespostasStub.Arquivo(Pdf));

        await using var arquivo = await downloader.BaixarAsync(
            "https://exemplo.com/a.pdf/@@download/file",
            "Relatório anual.pdf",
            TestContext.Current.CancellationToken);

        arquivo.NomeArquivo.Should().Be("Relatório anual.pdf");
    }

    [Fact]
    public async Task BaixarAsync_DeveConfiarNaExtensaoENaoNoTipoInformadoPelaOrigem()
    {
        // Plone e outros servidores entregam PDF como octet-stream.
        var downloader = Criar(_ => RespostasStub.Arquivo(Pdf, "application/octet-stream"));

        await using var arquivo = await downloader.BaixarAsync(
            "https://exemplo.com/a.pdf", "a.pdf", TestContext.Current.CancellationToken);

        arquivo.TipoConteudo.Should().Be("application/pdf");
    }

    [Fact]
    public async Task BaixarAsync_NaoDeveRepassarTiposPerigososDaOrigem()
    {
        var downloader = Criar(_ => RespostasStub.Arquivo(Pdf, "image/svg+xml"));

        await using var arquivo = await downloader.BaixarAsync(
            "https://exemplo.com/desenho", "desenho", TestContext.Current.CancellationToken);

        arquivo.TipoConteudo.Should().Be("application/octet-stream");
    }

    [Fact]
    public async Task BaixarAsync_DeveSanearONomeDoArquivo()
    {
        var downloader = Criar(_ => RespostasStub.Arquivo(
            Pdf, disposicao: "attachment; filename=\"..\\evil/nome.pdf\""));

        await using var arquivo = await downloader.BaixarAsync(
            "https://exemplo.com/a.pdf", null, TestContext.Current.CancellationToken);

        arquivo.NomeArquivo.Should().NotContainAny("/", "\\");
    }

    [Fact]
    public async Task BaixarAsync_DeveRecusarPaginaHtml()
    {
        var downloader = Criar(_ => RespostasStub.Html("<html>login</html>"));

        var act = () => downloader.BaixarAsync(
            "https://exemplo.com/a.pdf", null, TestContext.Current.CancellationToken);

        await act.Should().ThrowAsync<BusinessException>().WithMessage("*página*");
    }

    [Theory]
    [InlineData(HttpStatusCode.Forbidden)]
    [InlineData(HttpStatusCode.NotFound)]
    [InlineData(HttpStatusCode.BadGateway)]
    public async Task BaixarAsync_DeveRecusarRespostaSemSucesso(HttpStatusCode status)
    {
        var downloader = Criar(_ => new HttpResponseMessage(status));

        var act = () => downloader.BaixarAsync(
            "https://exemplo.com/a.pdf", null, TestContext.Current.CancellationToken);

        await act.Should().ThrowAsync<BusinessException>();
    }

    [Fact]
    public async Task BaixarAsync_DeveRecusarArquivoMaiorQueOLimitePeloContentLength()
    {
        var downloader = Criar(_ =>
        {
            var resposta = RespostasStub.Arquivo(Pdf);
            resposta.Content.Headers.ContentLength = AnexoDownloader.TamanhoMaximoBytes + 1;
            return resposta;
        });

        var act = () => downloader.BaixarAsync(
            "https://exemplo.com/a.pdf", null, TestContext.Current.CancellationToken);

        await act.Should().ThrowAsync<BusinessException>().WithMessage("*50 MB*");
    }

    [Fact]
    public async Task BaixarAsync_DeveInterromperQuandoOFluxoPassaDoLimiteSemContentLength()
    {
        var downloader = Criar(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StreamContent(new FluxoSemFim())
            {
                Headers = { ContentType = new("application/pdf") }
            }
        });

        await using var arquivo = await downloader.BaixarAsync(
            "https://exemplo.com/enorme.pdf", null, TestContext.Current.CancellationToken);

        var act = () => LerTudoAsync(arquivo.Conteudo);

        await act.Should().ThrowAsync<BusinessException>().WithMessage("*tamanho máximo*");
    }

    [Theory]
    [InlineData("ftp://exemplo.com/a.pdf")]
    [InlineData("file:///etc/passwd")]
    [InlineData("nao-e-url")]
    public async Task BaixarAsync_DeveRecusarEnderecoNaoHttp(string url)
    {
        var handler = new RespostaStubHandler(_ => RespostasStub.Arquivo(Pdf));
        var downloader = new AnexoDownloader(new HttpClient(handler));

        var act = () => downloader.BaixarAsync(url, null, TestContext.Current.CancellationToken);

        await act.Should().ThrowAsync<BusinessException>();
        handler.Requisicoes.Should().BeEmpty();
    }

    [Fact]
    public async Task BaixarAsync_DeveConverterFalhaDeRedeEmErroDeNegocio()
    {
        var downloader = Criar(_ => throw new HttpRequestException("sem rede"));

        var act = () => downloader.BaixarAsync(
            "https://exemplo.com/a.pdf", null, TestContext.Current.CancellationToken);

        await act.Should().ThrowAsync<BusinessException>();
    }

    [Fact]
    public async Task BaixarAsync_DeveUsarGetENuncaHead()
    {
        var metodos = new List<HttpMethod>();
        var downloader = Criar(requisicao =>
        {
            metodos.Add(requisicao.Method);
            return RespostasStub.Arquivo(Pdf);
        });

        await using var _ = await downloader.BaixarAsync(
            "https://exemplo.com/a.pdf", null, TestContext.Current.CancellationToken);

        metodos.Should().Equal(HttpMethod.Get);
    }

    private static AnexoDownloader Criar(Func<HttpRequestMessage, HttpResponseMessage> responder) =>
        new(new HttpClient(new RespostaStubHandler(responder)));

    private static async Task<byte[]> LerTudoAsync(Stream fluxo)
    {
        using var memoria = new MemoryStream();
        await fluxo.CopyToAsync(memoria, TestContext.Current.CancellationToken);
        return memoria.ToArray();
    }

    private sealed class FluxoSemFim : Stream
    {
        public override bool CanRead => true;
        public override bool CanSeek => false;
        public override bool CanWrite => false;
        public override long Length => throw new NotSupportedException();
        public override long Position
        {
            get => throw new NotSupportedException();
            set => throw new NotSupportedException();
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            Array.Fill(buffer, (byte)'x', offset, count);
            return count;
        }

        public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
        {
            buffer.Span.Fill((byte)'x');
            return ValueTask.FromResult(buffer.Length);
        }

        public override void Flush() { }
        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
        public override void SetLength(long value) => throw new NotSupportedException();
        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
    }
}
