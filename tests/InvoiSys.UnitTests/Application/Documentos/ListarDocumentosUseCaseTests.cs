using FluentAssertions;
using FluentValidation;
using InvoiSys.Application.Documentos.ListarDocumentos;
using InvoiSys.Domain.Entities;
using InvoiSys.Domain.Repositories;

namespace InvoiSys.UnitTests.Application.Documentos;

public sealed class ListarDocumentosUseCaseTests
{
    [Fact]
    public async Task Execute_DevePropagarBuscaTipoEPeriodo_QuandoFiltrosInformados()
    {
        var repository = new DocumentosStub();
        var useCase = new ListarDocumentosUseCase(new ListarDocumentosValidator(), repository);
        var dataInicio = new DateTimeOffset(2026, 10, 1, 0, 0, 0, TimeSpan.Zero);
        var dataFim = new DateTimeOffset(2026, 10, 15, 23, 59, 59, TimeSpan.Zero);

        await useCase.Execute(
            new ListarDocumentosRequest(1, 10, "MOC", "nota", "RSS", dataInicio, dataFim),
            TestContext.Current.CancellationToken);

        repository.Contexto.Should().Be("MOC");
        repository.Busca.Should().Be("nota");
        repository.Tipo.Should().Be("RSS");
        repository.DataInicio.Should().Be(dataInicio);
        repository.DataFim.Should().Be(dataFim);
    }

    [Fact]
    public async Task Execute_DeveConverterPeriodoParaUtc_QuandoDatasComOffset()
    {
        var repository = new DocumentosStub();
        var useCase = new ListarDocumentosUseCase(new ListarDocumentosValidator(), repository);
        var offset = TimeSpan.FromHours(-3);

        await useCase.Execute(
            new ListarDocumentosRequest(
                DataInicio: new DateTimeOffset(2026, 10, 1, 0, 0, 0, offset),
                DataFim: new DateTimeOffset(2026, 10, 15, 23, 59, 59, offset)),
            TestContext.Current.CancellationToken);

        repository.DataInicio!.Value.Offset.Should().Be(TimeSpan.Zero);
        repository.DataInicio.Value.UtcDateTime.Should().Be(new DateTime(2026, 10, 1, 3, 0, 0, DateTimeKind.Utc));
        repository.DataFim!.Value.Offset.Should().Be(TimeSpan.Zero);
        repository.DataFim.Value.UtcDateTime.Should().Be(new DateTime(2026, 10, 16, 2, 59, 59, DateTimeKind.Utc));
    }

    [Fact]
    public async Task Execute_DeveRetornarPaginaETotal_QuandoRepositorioRetornaItens()
    {
        var repository = new DocumentosStub();
        var useCase = new ListarDocumentosUseCase(new ListarDocumentosValidator(), repository);

        var response = await useCase.Execute(
            new ListarDocumentosRequest(3, 5),
            TestContext.Current.CancellationToken);

        repository.Pagina.Should().Be(3);
        repository.TamanhoPagina.Should().Be(5);
        response.Pagina.Should().Be(3);
        response.TamanhoPagina.Should().Be(5);
        response.Total.Should().Be(12);
        response.Itens.Should().ContainSingle()
            .Which.Titulo.Should().Be("Nota técnica");
    }

    [Fact]
    public async Task Execute_DeveLancarValidationException_QuandoDataFimAnteriorADataInicio()
    {
        var repository = new DocumentosStub();
        var useCase = new ListarDocumentosUseCase(new ListarDocumentosValidator(), repository);
        var request = new ListarDocumentosRequest(
            DataInicio: new DateTimeOffset(2026, 10, 15, 0, 0, 0, TimeSpan.Zero),
            DataFim: new DateTimeOffset(2026, 10, 1, 0, 0, 0, TimeSpan.Zero));

        var act = () => useCase.Execute(request, TestContext.Current.CancellationToken);

        await act.Should().ThrowAsync<ValidationException>();
        repository.Chamado.Should().BeFalse();
    }

    private sealed class DocumentosStub : IDocumentoRepository
    {
        public bool Chamado { get; private set; }
        public int Pagina { get; private set; }
        public int TamanhoPagina { get; private set; }
        public string? Contexto { get; private set; }
        public string? Busca { get; private set; }
        public string? Tipo { get; private set; }
        public DateTimeOffset? DataInicio { get; private set; }
        public DateTimeOffset? DataFim { get; private set; }

        public Task AdicionarVariosAsync(IEnumerable<Documento> documentos, CancellationToken token) => Task.CompletedTask;
        public Task<Documento?> ObterPorIdAsync(Guid id, CancellationToken token) => Task.FromResult<Documento?>(null);

        public Task<(IReadOnlyList<Documento> Itens, int Total)> ListarAsync(int pagina, int tamanhoPagina,
            string? contexto, string? busca, string? tipo, DateTimeOffset? dataInicio, DateTimeOffset? dataFim,
            CancellationToken token)
        {
            Chamado = true;
            Pagina = pagina;
            TamanhoPagina = tamanhoPagina;
            Contexto = contexto;
            Busca = busca;
            Tipo = tipo;
            DataInicio = dataInicio;
            DataFim = dataFim;
            return Task.FromResult<(IReadOnlyList<Documento>, int)>((
                [new Documento(Guid.NewGuid(), "Nota técnica", "https://exemplo.com/nota", "RSS", "Portal", "MOC", null, null, null, "hash")], 12));
        }
    }
}
