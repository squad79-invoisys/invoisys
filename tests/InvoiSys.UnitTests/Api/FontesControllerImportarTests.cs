using System.Text;
using FluentAssertions;
using InvoiSys.Api.Controllers;
using InvoiSys.Application.Common.Exceptions;
using InvoiSys.Application.Common.Responses;
using InvoiSys.Application.Fontes;
using InvoiSys.Application.Fontes.AlterarStatusFonte;
using InvoiSys.Application.Fontes.AtualizarFonte;
using InvoiSys.Application.Fontes.ConsultarFonte;
using InvoiSys.Application.Fontes.CriarFonte;
using InvoiSys.Application.Fontes.ImportarFontes;
using InvoiSys.Application.Fontes.ListarFontes;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace InvoiSys.UnitTests.Api;

public sealed class FontesControllerImportarTests
{
    [Fact]
    public async Task ImportarAsync_DeveLancarBusinessException_QuandoArquivoNaoEnviado()
    {
        var useCase = new ImportarFontesUseCaseStub(new ImportarFontesResponse(0, 0, []));
        var controller = CriarController(useCase);

        var act = () => controller.ImportarAsync(
            null,
            TestContext.Current.CancellationToken);

        await act.Should().ThrowAsync<BusinessException>();
        useCase.Chamadas.Should().Be(0);
    }

    [Fact]
    public async Task ImportarAsync_DeveRetornarOkComResumo_QuandoArquivoEnviado()
    {
        var esperado = new ImportarFontesResponse(
            3,
            2,
            [new LinhaRejeitada(3, "Tipo inválido: 'Xml'. Use Rss, Atom ou WebHtml.")]);
        var useCase = new ImportarFontesUseCaseStub(esperado);
        var controller = CriarController(useCase);
        var bytes = Encoding.UTF8.GetBytes("nome,url,tipo,periodicidade\n");
        var arquivo = new FormFile(new MemoryStream(bytes), 0, bytes.Length, "arquivo", "fontes.csv");

        var resultado = await controller.ImportarAsync(
            arquivo,
            TestContext.Current.CancellationToken);

        var ok = resultado.Should().BeOfType<OkObjectResult>().Subject;
        var envelope = ok.Value.Should()
            .BeOfType<ApiResponse<ImportarFontesResponse>>().Subject;
        envelope.Sucesso.Should().BeTrue();
        envelope.Mensagem.Should().Be("2 fonte(s) cadastrada(s) e 1 linha(s) rejeitada(s).");
        envelope.Dados.Should().BeSameAs(esperado);
        useCase.Chamadas.Should().Be(1);
        useCase.TextoRecebido.Should().Be("nome,url,tipo,periodicidade\n");
    }

    private static FontesController CriarController(IImportarFontesUseCase importar) =>
        new(
            new NaoUsado(),
            new NaoUsado(),
            new NaoUsado(),
            new NaoUsado(),
            new NaoUsado(),
            importar);

    private sealed class ImportarFontesUseCaseStub(
        ImportarFontesResponse resposta) : IImportarFontesUseCase
    {
        public int Chamadas { get; private set; }
        public string? TextoRecebido { get; private set; }

        public async Task<ImportarFontesResponse> ExecutarAsync(
            Stream arquivo,
            CancellationToken cancellationToken)
        {
            Chamadas++;
            using var reader = new StreamReader(arquivo);
            TextoRecebido = await reader.ReadToEndAsync(cancellationToken);
            return resposta;
        }
    }

    private sealed class NaoUsado :
        ICriarFonteUseCase,
        IConsultarFonteUseCase,
        IListarFontesUseCase,
        IAtualizarFonteUseCase,
        IAlterarStatusFonteUseCase
    {
        Task<FonteResponse> ICriarFonteUseCase.ExecutarAsync(
            CriarFonteRequest request, CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<FonteResponse> Execute(Guid id, CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<PagedResponse<FonteResponse>> Execute(
            ListarFontesRequest request, CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<FonteResponse> Execute(
            Guid id, AtualizarFonteRequest request, CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task Execute(Guid id, bool ativo, CancellationToken cancellationToken) =>
            throw new NotSupportedException();
    }
}
