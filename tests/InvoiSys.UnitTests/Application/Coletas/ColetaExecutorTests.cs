using FluentAssertions;
using InvoiSys.Application.Coletas.ExecutarColeta;
using InvoiSys.Application.Common.Coletas;
using InvoiSys.Application.Common.Collectors;
using InvoiSys.Domain.Entities;
using InvoiSys.Domain.Enums;
using InvoiSys.Domain.Repositories;
using Microsoft.Extensions.Logging.Abstractions;

namespace InvoiSys.UnitTests.Application.Coletas;

public sealed class ColetaExecutorTests
{
    [Fact]
    public async Task Automatica_DeveEsperarPeriodicidadeERegistrarDocumentosUnicos()
    {
        var fixture = new Fixture();
        var executor = fixture.CriarExecutor();
        (await executor.ExecutarAutomaticaSeVencidaAsync(fixture.Fonte.Id,
            TestContext.Current.CancellationToken)).Should().BeNull();

        fixture.Relogio.Agora = DateTimeOffset.UtcNow.AddHours(2);
        var response = await executor.ExecutarAutomaticaSeVencidaAsync(
            fixture.Fonte.Id, TestContext.Current.CancellationToken);

        response.Should().NotBeNull();
        response!.TipoExecucao.Should().Be(TipoExecucao.Automatica);
        response.SolicitadaPorUsuarioId.Should().BeNull();
        response.QuantidadeDocumentos.Should().Be(1);
        fixture.Documentos.Recebidos.Should().ContainSingle();
        fixture.UnitOfWork.Commits.Should().Be(2);
    }

    [Fact]
    public async Task Automatica_CanceladaDuranteColetor_DeveEncerrarHistorico()
    {
        var fixture = new Fixture { ErroColetor = new OperationCanceledException() };
        fixture.Relogio.Agora = DateTimeOffset.UtcNow.AddHours(2);
        var act = () => fixture.CriarExecutor().ExecutarAutomaticaSeVencidaAsync(
            fixture.Fonte.Id, TestContext.Current.CancellationToken);

        await act.Should().ThrowAsync<OperationCanceledException>();
        fixture.Execucoes.Itens.Should().ContainSingle()
            .Which.Status.Should().Be(StatusExecucaoColeta.Falhou);
        fixture.Execucoes.Itens[0].MensagemErro.Should().Contain("cancelada");
    }

    [Fact]
    public async Task Automatica_ComTimeout_DeveRegistrarMotivoDistinto()
    {
        var fixture = new Fixture { ErroColetor = new OperationCanceledException() };
        fixture.Relogio.Agora = DateTimeOffset.UtcNow.AddHours(2);
        using var timeout = new CancellationTokenSource();
        using var encerramento = new CancellationTokenSource();
        timeout.Cancel();

        var act = () => fixture.CriarExecutor().ExecutarAutomaticaSeVencidaAsync(
            fixture.Fonte.Id, timeout.Token, encerramento.Token);
        await act.Should().ThrowAsync<OperationCanceledException>();
        fixture.Execucoes.Itens[0].MensagemErro.Should().Contain("Tempo limite");
    }

    [Fact]
    public async Task Automatica_CanceladaDurantePersistencia_DeveReverterEEncerrarHistorico()
    {
        var fixture = new Fixture();
        fixture.Relogio.Agora = DateTimeOffset.UtcNow.AddHours(2);
        fixture.Documentos.Erro = new OperationCanceledException();
        var act = () => fixture.CriarExecutor().ExecutarAutomaticaSeVencidaAsync(
            fixture.Fonte.Id, TestContext.Current.CancellationToken);

        await act.Should().ThrowAsync<OperationCanceledException>();
        fixture.UnitOfWork.Rollbacks.Should().Be(1);
        fixture.Execucoes.Itens.Should().ContainSingle()
            .Which.Status.Should().Be(StatusExecucaoColeta.Falhou);
    }

    [Fact]
    public async Task Manual_CanceladaDuranteColetor_DevePreservarAuditoriaAnterior()
    {
        var fixture = new Fixture { ErroColetor = new OperationCanceledException() };
        var act = () => fixture.CriarExecutor().ExecutarManualAsync(
            fixture.Fonte.Id, Guid.NewGuid(), TestContext.Current.CancellationToken);

        await act.Should().ThrowAsync<OperationCanceledException>();
        fixture.UnitOfWork.Commits.Should().Be(0);
        fixture.UnitOfWork.Rollbacks.Should().Be(0);
    }

    private sealed class Fixture
    {
        public Fonte Fonte { get; } = new("Portal", "https://exemplo.com",
            TipoFonte.WebHtml, 60, Guid.NewGuid(), "Outro");
        public RelogioStub Relogio { get; } = new();
        public ExecucoesStub Execucoes { get; } = new();
        public DocumentosStub Documentos { get; } = new();
        public UnitOfWorkStub UnitOfWork { get; } = new();
        public Exception? ErroColetor { get; init; }

        public ColetaExecutor CriarExecutor() => new(
            new FonteStub(Fonte), Execucoes, Documentos,
            new ResolverStub(new CollectorStub(ErroColetor)), UnitOfWork,
            new LockStub(), Relogio, NullLogger<ColetaExecutor>.Instance);
    }

    private sealed class RelogioStub : TimeProvider
    {
        public DateTimeOffset Agora { get; set; } = DateTimeOffset.UtcNow;
        public override DateTimeOffset GetUtcNow() => Agora;
    }

    private sealed class LockStub : IFonteColetaLock, IFonteColetaLease
    {
        public IFonteColetaLease? PosseAtual => this;
        public bool EstaReivindicada => true;
        public Task<bool> TentarReivindicarAsync(CancellationToken token) => Task.FromResult(true);
        public Task RenovarAsync(CancellationToken token) => Task.CompletedTask;
        public Task ValidarTransacaoAsync(System.Data.Common.DbTransaction transaction, CancellationToken token) => Task.CompletedTask;
        public Task RenovarTransacaoAsync(System.Data.Common.DbTransaction transaction, CancellationToken token) => Task.CompletedTask;
        public Task<IFonteColetaLease?> TentarAdquirirAsync(Guid fonteId, CancellationToken token) =>
            Task.FromResult<IFonteColetaLease?>(this);
        public Task VerificarSessaoAsync(CancellationToken token) => Task.CompletedTask;
        public ValueTask DisposeAsync() => ValueTask.CompletedTask;
    }

    private sealed class FonteStub(Fonte fonte) : IFonteRepository
    {
        public Task<IReadOnlyList<Guid>> ListarIdsAtivasVencidasAsync(
            DateTimeOffset agoraUtc, CancellationToken token) =>
            Task.FromResult<IReadOnlyList<Guid>>([]);
        public Task AdicionarAsync(Fonte item, CancellationToken token) => Task.CompletedTask;
        public Task<Fonte?> ObterPorIdAsync(Guid id, CancellationToken token) =>
            Task.FromResult<Fonte?>(id == fonte.Id ? fonte : null);
        public Task<(IReadOnlyList<Fonte> Itens, int Total)> ListarAsync(
            int pagina, int tamanhoPagina, TipoFonte? tipo, StatusFonte? status,
            string? contexto, string? busca, CancellationToken token) =>
            Task.FromResult<(IReadOnlyList<Fonte>, int)>(([], 0));
    }

    private sealed class ExecucoesStub : IExecucaoColetaRepository
    {
        public List<ExecucaoColeta> Itens { get; } = [];
        public Task AdicionarAsync(ExecucaoColeta execucao, CancellationToken token)
        {
            Itens.Add(execucao);
            return Task.CompletedTask;
        }
        public Task<ExecucaoColeta?> ObterPorIdAsync(Guid id, CancellationToken token) =>
            Task.FromResult(Itens.FirstOrDefault(x => x.Id == id));
        public Task<ExecucaoColeta?> ObterUltimaPorFonteAsync(Guid fonteId, CancellationToken token) =>
            Task.FromResult(Itens.Where(x => x.FonteId == fonteId)
                .OrderByDescending(x => x.Inicio).FirstOrDefault());
        public Task<IReadOnlyList<Guid>> ListarFontesEmAndamentoAsync(CancellationToken token) =>
            Task.FromResult<IReadOnlyList<Guid>>([]);
        public Task<IReadOnlyList<ExecucaoColeta>> ListarEmAndamentoPorFonteAsync(
            Guid fonteId, CancellationToken token) =>
            Task.FromResult<IReadOnlyList<ExecucaoColeta>>(Itens.Where(x =>
                x.FonteId == fonteId && x.Status == StatusExecucaoColeta.EmAndamento).ToList());
        public Task<(IReadOnlyList<ExecucaoColeta> Itens, int Total)> ListarAsync(
            int pagina, int tamanhoPagina, Guid? fonteId, StatusExecucaoColeta? status,
            CancellationToken token) =>
            Task.FromResult<(IReadOnlyList<ExecucaoColeta>, int)>(([], 0));
    }

    private sealed class DocumentosStub : IDocumentoRepository
    {
        public IReadOnlyCollection<Documento> Recebidos { get; private set; } = [];
        public Exception? Erro { get; set; }
        public Task<int> InserirNovosAsync(
            IReadOnlyCollection<Documento> documentos, CancellationToken token)
        {
            Recebidos = documentos;
            return Erro is null ? Task.FromResult(documentos.Count) : Task.FromException<int>(Erro);
        }
        public Task<Documento?> ObterPorIdAsync(Guid id, CancellationToken token) =>
            Task.FromResult<Documento?>(null);
        public Task<(IReadOnlyList<Documento> Itens, int Total)> ListarAsync(
            int pagina, int tamanhoPagina, string? contexto, string? busca,
            CancellationToken token) =>
            Task.FromResult<(IReadOnlyList<Documento>, int)>(([], 0));
    }

    private sealed class ResolverStub(IContentCollector collector) : ICollectorResolver
    {
        public IContentCollector Resolver(TipoFonte tipo) => collector;
    }

    private sealed class CollectorStub(Exception? erro) : IContentCollector
    {
        public bool Suporta(TipoFonte tipo) => true;
        public Task<IReadOnlyList<CollectedDocument>> ColetarAsync(
            string url, string? seletorConteudo, CancellationToken token)
        {
            if (erro is not null)
                return Task.FromException<IReadOnlyList<CollectedDocument>>(erro);
            var item = new CollectedDocument("Documento", url, "WEB", url,
                null, "Conteúdo", null, new string('a', 64));
            return Task.FromResult<IReadOnlyList<CollectedDocument>>([item, item]);
        }
    }

    private sealed class UnitOfWorkStub : IUnitOfWork
    {
        public int Commits { get; private set; }
        public int Rollbacks { get; private set; }
        public Task BeginTransactionAsync(CancellationToken token) => Task.CompletedTask;
        public Task<int> CommitAsync(CancellationToken token)
        {
            Commits++;
            return Task.FromResult(1);
        }
        public Task CommitTransactionAsync(CancellationToken token) => Task.CompletedTask;
        public Task RollbackTransactionAsync(CancellationToken token)
        {
            Rollbacks++;
            return Task.CompletedTask;
        }
    }
}
