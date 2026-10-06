using FluentAssertions;
using FluentValidation;
using InvoiSys.Application.Historico.ListarHistorico;
using InvoiSys.Domain.Entities;
using InvoiSys.Domain.Enums;
using InvoiSys.Domain.Repositories;

namespace InvoiSys.UnitTests.Application.Historico;

public sealed class ListarHistoricoUseCaseTests
{
    [Fact]
    public async Task ExecutarAsync_DeveRetornarEventosETotaisPorCategoria()
    {
        var registro = new RegistroAuditoria(
            TipoEventoAuditoria.Coleta, "Coleta manual concluída (3 documentos)", "Portal",
            Guid.NewGuid(), Guid.NewGuid(), "operador@invoisys.local", "127.0.0.1");
        var repository = new RepositoryStub([registro], 42, new Dictionary<TipoEventoAuditoria, int>
        {
            [TipoEventoAuditoria.Coleta] = 20,
            [TipoEventoAuditoria.Fonte] = 10,
            [TipoEventoAuditoria.Usuario] = 2,
            [TipoEventoAuditoria.Autenticacao] = 10
        });
        var useCase = new ListarHistoricoUseCase(new ListarHistoricoValidator(), repository);

        var response = await useCase.ExecutarAsync(
            new ListarHistoricoRequest(Pagina: 2, TamanhoPagina: 1),
            TestContext.Current.CancellationToken);

        response.Eventos.Itens.Should().ContainSingle()
            .Which.Atividade.Should().Be("Coleta manual concluída (3 documentos)");
        response.Eventos.Total.Should().Be(42);
        response.Eventos.Pagina.Should().Be(2);
        response.Totais.Should().Be(new TotaisHistoricoResponse(42, 20, 12, 10));
    }

    [Fact]
    public async Task ExecutarAsync_DeveRepassarFiltrosComDatasEmUtc()
    {
        var repository = new RepositoryStub([], 0, new Dictionary<TipoEventoAuditoria, int>());
        var useCase = new ListarHistoricoUseCase(new ListarHistoricoValidator(), repository);
        var usuarioId = Guid.NewGuid();
        var de = new DateTimeOffset(2026, 10, 1, 0, 0, 0, TimeSpan.FromHours(-3));
        var ate = new DateTimeOffset(2026, 10, 6, 23, 59, 59, TimeSpan.FromHours(-3));

        await useCase.ExecutarAsync(
            new ListarHistoricoRequest(
                Tipo: TipoEventoAuditoria.Fonte, UsuarioId: usuarioId, De: de, Ate: ate, Busca: "  portal "),
            TestContext.Current.CancellationToken);

        var filtro = repository.UltimoFiltro!;
        filtro.Tipo.Should().Be(TipoEventoAuditoria.Fonte);
        filtro.UsuarioId.Should().Be(usuarioId);
        filtro.De!.Value.Offset.Should().Be(TimeSpan.Zero);
        filtro.De.Should().Be(de);
        filtro.Ate!.Value.Offset.Should().Be(TimeSpan.Zero);
        filtro.Busca.Should().Be("portal");
    }

    [Fact]
    public async Task ExecutarAsync_DeveLancarValidationException_QuandoPeriodoInvertido()
    {
        var useCase = new ListarHistoricoUseCase(
            new ListarHistoricoValidator(),
            new RepositoryStub([], 0, new Dictionary<TipoEventoAuditoria, int>()));

        var act = () => useCase.ExecutarAsync(
            new ListarHistoricoRequest(De: DateTimeOffset.UtcNow, Ate: DateTimeOffset.UtcNow.AddDays(-1)),
            TestContext.Current.CancellationToken);

        await act.Should().ThrowAsync<ValidationException>();
    }

    private sealed class RepositoryStub(
        IReadOnlyList<RegistroAuditoria> itens,
        int total,
        IReadOnlyDictionary<TipoEventoAuditoria, int> porTipo) : IRegistroAuditoriaRepository
    {
        public FiltroAuditoria? UltimoFiltro { get; private set; }

        public Task AdicionarAsync(RegistroAuditoria registro, CancellationToken token) => Task.CompletedTask;

        public Task<(IReadOnlyList<RegistroAuditoria> Itens, int Total)> ListarAsync(
            FiltroAuditoria filtro, int pagina, int tamanhoPagina, CancellationToken token)
        {
            UltimoFiltro = filtro;
            return Task.FromResult((itens, total));
        }

        public Task<IReadOnlyDictionary<TipoEventoAuditoria, int>> ContarPorTipoAsync(
            FiltroAuditoria filtro, CancellationToken token) =>
            Task.FromResult(porTipo);

        public Task<IReadOnlyList<RegistroAuditoria>> ListarParaExportacaoAsync(
            FiltroAuditoria filtro, int limite, CancellationToken token) =>
            Task.FromResult(itens);
    }
}
