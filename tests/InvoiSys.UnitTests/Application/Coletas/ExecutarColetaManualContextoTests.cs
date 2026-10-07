using FluentAssertions;
using InvoiSys.Application.Coletas.ExecutarColetaManual;
using InvoiSys.Application.Common.Abstractions;
using InvoiSys.Application.Common.Auditoria;
using InvoiSys.Application.Common.Collectors;
using InvoiSys.Domain.Entities;
using InvoiSys.Domain.Enums;
using InvoiSys.Domain.Repositories;

namespace InvoiSys.UnitTests.Application.Coletas;

public sealed class ExecutarColetaManualContextoTests
{
    [Fact]
    public async Task ExecutarAsync_DeveCopiarContextoDaFonteSemAlterarDocumentoAnterior()
    {
        var fonte = new Fonte("Portal", "https://exemplo.com/feed", TipoFonte.Rss, 30, Guid.NewGuid(), "Legislação");
        var documentos = new DocumentosStub();
        var useCase = new ExecutarColetaManualUseCase(
            new UsuarioStub(), new FonteStub(fonte), new ExecucoesStub(), documentos,
            new CollectorResolverStub(), new AuditoriaServiceStub(), new UnitOfWorkStub());

        await useCase.ExecutarAsync(fonte.Id, TestContext.Current.CancellationToken);

        documentos.Itens.Should().ContainSingle();
        documentos.Itens[0].Contexto.Should().Be("Legislação");

        fonte.Atualizar(fonte.Nome, fonte.Url, fonte.Tipo, fonte.PeriodicidadeMinutos, Guid.NewGuid(), "MOC");
        documentos.Itens[0].Contexto.Should().Be("Legislação");

        await useCase.ExecutarAsync(fonte.Id, TestContext.Current.CancellationToken);
        documentos.Itens[1].Contexto.Should().Be("MOC");
    }

    private sealed class UsuarioStub : ICurrentUser
    {
        public Guid? UsuarioId { get; } = Guid.NewGuid();
        public string? Email => null;
        public bool EstaAutenticado => true;
    }

    private sealed class FonteStub(Fonte fonte) : IFonteRepository
    {
        public Task AdicionarAsync(Fonte item, CancellationToken token) => Task.CompletedTask;
        public Task<Fonte?> ObterPorIdAsync(Guid id, CancellationToken token) => Task.FromResult<Fonte?>(fonte);
        public Task<(IReadOnlyList<Fonte> Itens, int Total)> ListarAsync(int pagina, int tamanhoPagina,
            TipoFonte? tipo, StatusFonte? status, string? contexto, string? busca, CancellationToken token) =>
            Task.FromResult<(IReadOnlyList<Fonte>, int)>(([], 0));
    }

    private sealed class ExecucoesStub : IExecucaoColetaRepository
    {
        public Task AdicionarAsync(ExecucaoColeta execucao, CancellationToken token) => Task.CompletedTask;
        public Task<ExecucaoColeta?> ObterPorIdAsync(Guid id, CancellationToken token) => Task.FromResult<ExecucaoColeta?>(null);
        public Task<(IReadOnlyList<ExecucaoColeta> Itens, int Total)> ListarAsync(int pagina, int tamanhoPagina,
            Guid? fonteId, StatusExecucaoColeta? status, CancellationToken token) =>
            Task.FromResult<(IReadOnlyList<ExecucaoColeta>, int)>(([], 0));
    }

    private sealed class DocumentosStub : IDocumentoRepository
    {
        public List<Documento> Itens { get; } = [];
        public Task AdicionarVariosAsync(IEnumerable<Documento> documentos, CancellationToken token)
        {
            Itens.AddRange(documentos);
            return Task.CompletedTask;
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
        public Task<int> CommitAsync(CancellationToken token) => Task.FromResult(1);
    }

    private sealed class AuditoriaServiceStub : IAuditoriaService
    {
        public List<(TipoEventoAuditoria Tipo, string Atividade, string Objeto)> Registros { get; } = [];

        public Task RegistrarAsync(TipoEventoAuditoria tipo, string atividade, string objeto,
            Guid? objetoId, CancellationToken cancellationToken)
        {
            Registros.Add((tipo, atividade, objeto));
            return Task.CompletedTask;
        }

        public Task RegistrarAsync(TipoEventoAuditoria tipo, string atividade, string objeto,
            Guid? objetoId, Guid? usuarioId, string responsavel, CancellationToken cancellationToken)
        {
            Registros.Add((tipo, atividade, objeto));
            return Task.CompletedTask;
        }
    }
}
