using System.Data.Common;
using FluentAssertions;
using InvoiSys.Application.Coletas.ExecutarColetaManual;
using InvoiSys.Application.Coletas.ExecutarColeta;
using InvoiSys.Application.Common.Coletas;
using InvoiSys.Application.Common.Abstractions;
using InvoiSys.Application.Common.Collectors;
using InvoiSys.Application.Common.Exceptions;
using InvoiSys.Domain.Entities;
using InvoiSys.Domain.Enums;
using InvoiSys.Domain.Repositories;
using Microsoft.Extensions.Logging.Abstractions;

namespace InvoiSys.UnitTests.Application.Coletas;

public sealed class ExecutarColetaManualUseCaseTests
{
    [Fact]
    public async Task ExecutarAsync_DeveConcluirComQuantidadeEfetivamenteInserida()
    {
        var fixture = new Fixture([DocumentoColetado("hash-1")]);
        fixture.Documentos.QuantidadeInserida = 1;

        var response = await fixture.CriarUseCase().ExecutarAsync(
            fixture.Fonte.Id,
            TestContext.Current.CancellationToken);

        response.Status.Should().Be(StatusExecucaoColeta.Concluida);
        response.QuantidadeDocumentos.Should().Be(1);
        fixture.UnitOfWork.Commits.Should().Be(2);
        fixture.UnitOfWork.TransacoesConfirmadas.Should().Be(1);
    }

    [Fact]
    public async Task ExecutarAsync_DeveConcluirComZeroQuandoRepositorioNaoInserir()
    {
        var fixture = new Fixture([DocumentoColetado("hash-1")]);
        fixture.Documentos.QuantidadeInserida = 0;

        var response = await fixture.CriarUseCase().ExecutarAsync(
            fixture.Fonte.Id,
            TestContext.Current.CancellationToken);

        response.Status.Should().Be(StatusExecucaoColeta.Concluida);
        response.QuantidadeDocumentos.Should().Be(0);
    }

    [Fact]
    public async Task ExecutarAsync_DeveRemoverDuplicatasDoMesmoLote()
    {
        var documento = DocumentoColetado("hash-1");
        var fixture = new Fixture([documento, documento]);

        await fixture.CriarUseCase().ExecutarAsync(
            fixture.Fonte.Id,
            TestContext.Current.CancellationToken);

        fixture.Documentos.Recebidos.Should().ContainSingle();
    }

    [Fact]
    public async Task ExecutarAsync_FalhaNoColetor_DevePreservarAuditoriaAtual()
    {
        var fixture = new Fixture([])
        {
            ErroColetor = new InvalidOperationException("Coletor indisponível.")
        };

        var act = () => fixture.CriarUseCase().ExecutarAsync(
            fixture.Fonte.Id,
            TestContext.Current.CancellationToken);

        await act.Should().ThrowAsync<BusinessException>();
        fixture.UnitOfWork.TransacoesIniciadas.Should().Be(1);
        fixture.UnitOfWork.Commits.Should().Be(1);
        fixture.Execucoes.Adicionadas.Should().ContainSingle();
        fixture.Execucoes.Adicionadas[0].Status.Should().Be(StatusExecucaoColeta.Falhou);
    }

    [Fact]
    public async Task ExecutarAsync_FalhaDePersistencia_DeveReverterEReinserirExecucaoFalha()
    {
        var fixture = new Fixture([DocumentoColetado("hash-1")]);
        fixture.Documentos.Erro = new FakeDbException("Falha no batch.");

        var act = () => fixture.CriarUseCase().ExecutarAsync(
            fixture.Fonte.Id,
            TestContext.Current.CancellationToken);

        await act.Should().ThrowAsync<FakeDbException>();
        fixture.UnitOfWork.Rollbacks.Should().Be(1);
        fixture.UnitOfWork.Commits.Should().Be(2);
        fixture.Execucoes.Adicionadas.Should().HaveCount(2);
        fixture.Execucoes.Adicionadas[^1].Status.Should().Be(StatusExecucaoColeta.Falhou);
        fixture.Execucoes.Adicionadas[^1].QuantidadeDocumentos.Should().Be(0);
    }

    private static CollectedDocument DocumentoColetado(string hash) =>
        new(
            "Documento",
            "https://exemplo.com/documento",
            "WEB",
            "https://exemplo.com",
            null,
            "Conteúdo",
            null,
            hash);

    private sealed class Fixture(IReadOnlyList<CollectedDocument> coletados)
    {
        public Fonte Fonte { get; } = new(
            "Portal",
            "https://exemplo.com",
            TipoFonte.WebHtml,
            30,
            Guid.NewGuid(),
            "Legislação");

        public DocumentosStub Documentos { get; } = new();
        public ExecucoesStub Execucoes { get; } = new();
        public UnitOfWorkStub UnitOfWork { get; } = new();
        public Exception? ErroColetor { get; init; }

        public ExecutarColetaManualUseCase CriarUseCase() =>
            new(new UsuarioStub(), new ColetaExecutor(
                new FonteStub(Fonte),
                Execucoes,
                Documentos,
                new ResolverStub(new CollectorStub(coletados, ErroColetor)),
                UnitOfWork,
                new LockStub(),
                TimeProvider.System,
                NullLogger<ColetaExecutor>.Instance));
    }

    private sealed class UsuarioStub : ICurrentUser
    {
        public Guid? UsuarioId { get; } = Guid.NewGuid();
        public string? Email => null;
        public bool EstaAutenticado => true;
    }

    private sealed class FonteStub(Fonte fonte) : IFonteRepository
    {
        public Task<IReadOnlyList<Guid>> ListarIdsAtivasVencidasAsync(DateTimeOffset agoraUtc, CancellationToken token) =>
            Task.FromResult<IReadOnlyList<Guid>>([]);
        public Task AdicionarAsync(Fonte item, CancellationToken token) => Task.CompletedTask;
        public Task<Fonte?> ObterPorIdAsync(Guid id, CancellationToken token) => Task.FromResult<Fonte?>(fonte);
        public Task<(IReadOnlyList<Fonte> Itens, int Total)> ListarAsync(
            int pagina, int tamanhoPagina, TipoFonte? tipo, StatusFonte? status,
            string? contexto, string? busca, CancellationToken token) =>
            Task.FromResult<(IReadOnlyList<Fonte>, int)>(([], 0));
    }

    private sealed class ExecucoesStub : IExecucaoColetaRepository
    {
        public Task<ExecucaoColeta?> ObterUltimaPorFonteAsync(Guid fonteId, CancellationToken token) =>
            Task.FromResult<ExecucaoColeta?>(null);
        public Task<IReadOnlyList<Guid>> ListarFontesEmAndamentoAsync(CancellationToken token) =>
            Task.FromResult<IReadOnlyList<Guid>>([]);
        public Task<IReadOnlyList<ExecucaoColeta>> ListarEmAndamentoPorFonteAsync(Guid fonteId, CancellationToken token) =>
            Task.FromResult<IReadOnlyList<ExecucaoColeta>>([]);
        public List<ExecucaoColeta> Adicionadas { get; } = [];

        public Task AdicionarAsync(ExecucaoColeta execucao, CancellationToken token)
        {
            Adicionadas.Add(execucao);
            return Task.CompletedTask;
        }

        public Task<ExecucaoColeta?> ObterPorIdAsync(Guid id, CancellationToken token) =>
            Task.FromResult<ExecucaoColeta?>(null);

        public Task<(IReadOnlyList<ExecucaoColeta> Itens, int Total)> ListarAsync(
            int pagina, int tamanhoPagina, Guid? fonteId, StatusExecucaoColeta? status,
            CancellationToken token) =>
            Task.FromResult<(IReadOnlyList<ExecucaoColeta>, int)>(([], 0));
    }

    private sealed class DocumentosStub : IDocumentoRepository
    {
        public int QuantidadeInserida { get; set; } = 1;
        public Exception? Erro { get; set; }
        public IReadOnlyCollection<Documento> Recebidos { get; private set; } = [];

        public Task<int> InserirNovosAsync(
            IReadOnlyCollection<Documento> documentos,
            CancellationToken token)
        {
            Recebidos = documentos;
            return Erro is null
                ? Task.FromResult(QuantidadeInserida)
                : Task.FromException<int>(Erro);
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

    private sealed class CollectorStub(
        IReadOnlyList<CollectedDocument> documentos,
        Exception? erro) : IContentCollector
    {
        public bool Suporta(TipoFonte tipo) => true;

        public Task<IReadOnlyList<CollectedDocument>> ColetarAsync(
            string url,
            string? seletorConteudo,
            CancellationToken token) =>
            erro is null
                ? Task.FromResult(documentos)
                : Task.FromException<IReadOnlyList<CollectedDocument>>(erro);
    }

    private sealed class UnitOfWorkStub : IUnitOfWork
    {
        public int Commits { get; private set; }
        public int TransacoesIniciadas { get; private set; }
        public int TransacoesConfirmadas { get; private set; }
        public int Rollbacks { get; private set; }

        public Task BeginTransactionAsync(CancellationToken cancellationToken)
        {
            TransacoesIniciadas++;
            return Task.CompletedTask;
        }

        public Task<int> CommitAsync(CancellationToken cancellationToken)
        {
            Commits++;
            return Task.FromResult(1);
        }

        public Task CommitTransactionAsync(CancellationToken cancellationToken)
        {
            TransacoesConfirmadas++;
            return Task.CompletedTask;
        }

        public Task RollbackTransactionAsync(CancellationToken cancellationToken)
        {
            Rollbacks++;
            return Task.CompletedTask;
        }
    }

    private sealed class FakeDbException(string message) : DbException(message);

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
}
