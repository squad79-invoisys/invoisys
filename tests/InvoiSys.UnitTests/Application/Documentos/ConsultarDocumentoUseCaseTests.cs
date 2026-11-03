using FluentAssertions;
using InvoiSys.Application.Common.Exceptions;
using InvoiSys.Application.Documentos.ConsultarDocumento;
using InvoiSys.Domain.Entities;
using InvoiSys.Domain.Repositories;

namespace InvoiSys.UnitTests.Application.Documentos;

public sealed class ConsultarDocumentoUseCaseTests
{
    [Fact]
    public async Task Execute_DeveRetornarDocumento_QuandoExiste()
    {
        var documento = new Documento(Guid.NewGuid(), "Nota técnica", "https://exemplo.com/nota", "RSS",
            "Portal Fiscal", "MOC", new DateTimeOffset(2026, 10, 1, 0, 0, 0, TimeSpan.Zero), "Conteúdo", null, "hash");
        var useCase = new ConsultarDocumentoUseCase(new DocumentosStub(documento));

        var response = await useCase.Execute(documento.Id, TestContext.Current.CancellationToken);

        response.Id.Should().Be(documento.Id);
        response.Titulo.Should().Be("Nota técnica");
        response.UrlOriginal.Should().Be("https://exemplo.com/nota");
        response.Origem.Should().Be("Portal Fiscal");
        response.Tipo.Should().Be("RSS");
        response.DataPublicacao.Should().Be(documento.DataPublicacao);
        response.DataColeta.Should().Be(documento.DataColeta);
        response.ConteudoTextual.Should().Be("Conteúdo");
    }

    [Fact]
    public async Task Execute_DeveLancarNotFoundException_QuandoNaoExiste()
    {
        var useCase = new ConsultarDocumentoUseCase(new DocumentosStub(null));

        var act = () => useCase.Execute(Guid.NewGuid(), TestContext.Current.CancellationToken);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    private sealed class DocumentosStub(Documento? documento) : IDocumentoRepository
    {
        public Task AdicionarVariosAsync(IEnumerable<Documento> documentos, CancellationToken token) => Task.CompletedTask;

        public Task<Documento?> ObterPorIdAsync(Guid id, CancellationToken token) =>
            Task.FromResult(documento?.Id == id ? documento : null);

        public Task<(IReadOnlyList<Documento> Itens, int Total)> ListarAsync(int pagina, int tamanhoPagina,
            string? contexto, string? busca, string? tipo, DateTimeOffset? dataInicio, DateTimeOffset? dataFim,
            CancellationToken token) =>
            Task.FromResult<(IReadOnlyList<Documento>, int)>(([], 0));
    }
}
