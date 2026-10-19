using FluentAssertions;
using InvoiSys.Application.Documentos.ListarDocumentos;
using InvoiSys.Application.Fontes.ListarFontes;
using InvoiSys.Domain.Entities;
using InvoiSys.Domain.Enums;
using InvoiSys.Domain.Repositories;

namespace InvoiSys.UnitTests.Application;

public sealed class ListarContextoTests
{
    [Fact]
    public async Task ListarFontes_DevePropagarFiltroPaginacaoETotal()
    {
        var repository = new FontesStub();
        var useCase = new ListarFontesUseCase(new ListarFontesValidator(), repository);

        var response = await useCase.Execute(new ListarFontesRequest(2, 1, Contexto: " Legislação "), TestContext.Current.CancellationToken);

        repository.Contexto.Should().Be(" Legislação ");
        repository.Pagina.Should().Be(2);
        response.Total.Should().Be(3);
        response.Itens.Should().ContainSingle();
        response.Itens[0].Contexto.Should().Be("Legislação");
    }

    [Fact]
    public async Task ListarDocumentos_DevePropagarFiltroPaginacaoETotal()
    {
        var repository = new DocumentosStub();
        var useCase = new ListarDocumentosUseCase(new ListarDocumentosValidator(), repository);

        var response = await useCase.Execute(new ListarDocumentosRequest(2, 1, " MOC "), TestContext.Current.CancellationToken);

        repository.Contexto.Should().Be(" MOC ");
        repository.Pagina.Should().Be(2);
        response.Total.Should().Be(4);
        response.Itens.Should().ContainSingle();
        response.Itens[0].Contexto.Should().Be("MOC");
    }

    private sealed class FontesStub : IFonteRepository
    {
        public string? Contexto { get; private set; }
        public int Pagina { get; private set; }
        public Task AdicionarAsync(Fonte fonte, CancellationToken token) => Task.CompletedTask;
        public Task<Fonte?> ObterPorIdAsync(Guid id, CancellationToken token) => Task.FromResult<Fonte?>(null);
        public Task<(IReadOnlyList<Fonte> Itens, int Total)> ListarAsync(int pagina, int tamanhoPagina,
            TipoFonte? tipo, StatusFonte? status, string? contexto, string? busca, CancellationToken token)
        {
            Contexto = contexto;
            Pagina = pagina;
            return Task.FromResult<(IReadOnlyList<Fonte>, int)>((
                [new Fonte("Portal", "https://exemplo.com", TipoFonte.Rss, 30, Guid.NewGuid(), "Legislação")], 3));
        }
    }

    private sealed class DocumentosStub : IDocumentoRepository
    {
        public string? Contexto { get; private set; }
        public int Pagina { get; private set; }
        public Task AdicionarVariosAsync(IEnumerable<Documento> documentos, CancellationToken token) => Task.CompletedTask;
        public Task<Documento?> ObterPorIdAsync(Guid id, CancellationToken token) => Task.FromResult<Documento?>(null);
        public Task<(IReadOnlyList<Documento> Itens, int Total)> ListarAsync(int pagina, int tamanhoPagina,
            string? contexto, string? busca, CancellationToken token)
        {
            Contexto = contexto;
            Pagina = pagina;
            return Task.FromResult<(IReadOnlyList<Documento>, int)>((
                [new Documento(Guid.NewGuid(), "Manual", "https://exemplo.com/manual", "PDF", "Portal", "MOC", null, null, null, "hash")], 4));
        }
    }
}
