using FluentAssertions;
using InvoiSys.Application.Coletas.ExecutarColetaManual;
using InvoiSys.Application.Coletas.ExecutarColeta;
using InvoiSys.Application.Common.Coletas;
using InvoiSys.Application.Common.Abstractions;
using InvoiSys.Application.Common.Collectors;
using InvoiSys.Domain.Entities;
using InvoiSys.Domain.Enums;
using InvoiSys.Domain.Repositories;
using Microsoft.Extensions.Logging.Abstractions;

namespace InvoiSys.UnitTests.Application.Coletas;

public sealed class ExecutarColetaManualContextoTests
{
    [Fact]
    public async Task ExecutarAsync_DeveCopiarContextoDaFonteSemAlterarDocumentoAnterior()
    {
        var fonte = new Fonte("Portal", "https://exemplo.com/feed", TipoFonte.Rss, 30, Guid.NewGuid(), "Legislação");
        var documentos = new DocumentosStub();
        var useCase = new ExecutarColetaManualUseCase(new UsuarioStub(),
            new ColetaExecutor(new FonteStub(fonte), new ExecucoesStub(), documentos,
                new CollectorResolverStub(), new UnitOfWorkStub(), new LockStub(),
                TimeProvider.System, NullLogger<ColetaExecutor>.Instance));

        await useCase.ExecutarAsync(fonte.Id, TestContext.Current.CancellationToken);

        documentos.Itens.Should().ContainSingle();
        documentos.Itens[0].Contexto.Should().Be("Legislação");

        fonte.Atualizar(fonte.Nome, fonte.Url, fonte.Tipo, fonte.PeriodicidadeMinutos, Guid.NewGuid(), "MOC");
        documentos.Itens[0].Contexto.Should().Be("Legislação");

        await useCase.ExecutarAsync(fonte.Id, TestContext.Current.CancellationToken);
        documentos.Itens.Should().ContainSingle();
        documentos.Itens[0].Contexto.Should().Be("Legislação");
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
        public Task<(IReadOnlyList<Fonte> Itens, int Total)> ListarAsync(int pagina, int tamanhoPagina,
            TipoFonte? tipo, StatusFonte? status, string? contexto, string? busca, CancellationToken token) =>
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
        public Task AdicionarAsync(ExecucaoColeta execucao, CancellationToken token) => Task.CompletedTask;
        public Task<ExecucaoColeta?> ObterPorIdAsync(Guid id, CancellationToken token) => Task.FromResult<ExecucaoColeta?>(null);
        public Task<(IReadOnlyList<ExecucaoColeta> Itens, int Total)> ListarAsync(int pagina, int tamanhoPagina,
            Guid? fonteId, StatusExecucaoColeta? status, CancellationToken token) =>
            Task.FromResult<(IReadOnlyList<ExecucaoColeta>, int)>(([], 0));
    }

    private sealed class DocumentosStub : IDocumentoRepository
    {
        public List<Documento> Itens { get; } = [];
        public Task<int> InserirNovosAsync(IReadOnlyCollection<Documento> documentos, CancellationToken token)
        {
            var novos = documentos
                .Where(documento => !Itens.Any(existente =>
                    existente.UrlOriginal == documento.UrlOriginal &&
                    existente.Hash == documento.Hash))
                .ToList();
            Itens.AddRange(novos);
            return Task.FromResult(novos.Count);
        }
        public Task<Documento?> ObterPorIdAsync(Guid id, CancellationToken token) => Task.FromResult<Documento?>(null);
        public Task<(IReadOnlyList<Documento> Itens, int Total)> ListarAsync(int pagina, int tamanhoPagina,
            string? contexto, string? busca, CancellationToken token) =>
            Task.FromResult<(IReadOnlyList<Documento>, int)>(([], 0));
    }

    private sealed class CollectorResolverStub : ICollectorResolver
    {
        public IContentCollector Resolver(TipoFonte tipo) => new CollectorStub();
    }

    private sealed class CollectorStub : IContentCollector
    {
        public bool Suporta(TipoFonte tipo) => true;
        public Task<IReadOnlyList<CollectedDocument>> ColetarAsync(string url, string? seletorConteudo, CancellationToken token) =>
            Task.FromResult<IReadOnlyList<CollectedDocument>>([
                new("Documento", "https://exemplo.com/documento", "PDF", "Portal", null, null, null, "hash")
            ]);
    }

    private sealed class UnitOfWorkStub : IUnitOfWork
    {
        public Task BeginTransactionAsync(CancellationToken token) => Task.CompletedTask;
        public Task<int> CommitAsync(CancellationToken token) => Task.FromResult(1);
        public Task CommitTransactionAsync(CancellationToken token) => Task.CompletedTask;
        public Task RollbackTransactionAsync(CancellationToken token) => Task.CompletedTask;
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
}
