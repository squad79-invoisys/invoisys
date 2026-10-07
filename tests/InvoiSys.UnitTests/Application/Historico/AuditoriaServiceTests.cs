using FluentAssertions;
using InvoiSys.Application.Common.Abstractions;
using InvoiSys.Application.Common.Auditoria;
using InvoiSys.Domain.Entities;
using InvoiSys.Domain.Enums;
using InvoiSys.Domain.Repositories;

namespace InvoiSys.UnitTests.Application.Historico;

public sealed class AuditoriaServiceTests
{
    [Fact]
    public async Task RegistrarAsync_DeveUsarUsuarioAtualEEnderecoIp()
    {
        var usuarioId = Guid.NewGuid();
        var repository = new RepositoryStub();
        var service = new AuditoriaService(
            new CurrentUserStub(usuarioId, "operador@invoisys.local"),
            new ContextoRequisicaoStub("10.0.0.7"),
            repository);
        var fonteId = Guid.NewGuid();

        await service.RegistrarAsync(
            TipoEventoAuditoria.Fonte, "Fonte cadastrada", "Portal", fonteId,
            TestContext.Current.CancellationToken);

        var registro = repository.Registros.Should().ContainSingle().Subject;
        registro.UsuarioId.Should().Be(usuarioId);
        registro.Responsavel.Should().Be("operador@invoisys.local");
        registro.EnderecoIp.Should().Be("10.0.0.7");
        registro.ObjetoId.Should().Be(fonteId);
    }

    [Fact]
    public async Task RegistrarAsync_DeveUsarSistemaComoResponsavel_QuandoNaoHouverUsuario()
    {
        var repository = new RepositoryStub();
        var service = new AuditoriaService(
            new CurrentUserStub(null, null),
            new ContextoRequisicaoStub(null),
            repository);

        await service.RegistrarAsync(
            TipoEventoAuditoria.Coleta, "Coleta automática concluída", "Portal", null,
            TestContext.Current.CancellationToken);

        var registro = repository.Registros.Should().ContainSingle().Subject;
        registro.UsuarioId.Should().BeNull();
        registro.Responsavel.Should().Be(AuditoriaService.ResponsavelSistema);
    }

    private sealed class CurrentUserStub(Guid? usuarioId, string? email) : ICurrentUser
    {
        public Guid? UsuarioId { get; } = usuarioId;
        public string? Email { get; } = email;
        public bool EstaAutenticado => UsuarioId is not null;
    }

    private sealed class ContextoRequisicaoStub(string? enderecoIp) : IContextoRequisicao
    {
        public string? EnderecoIp { get; } = enderecoIp;
    }

    private sealed class RepositoryStub : IRegistroAuditoriaRepository
    {
        public List<RegistroAuditoria> Registros { get; } = [];

        public Task AdicionarAsync(RegistroAuditoria registro, CancellationToken token)
        {
            Registros.Add(registro);
            return Task.CompletedTask;
        }

        public Task<(IReadOnlyList<RegistroAuditoria> Itens, int Total)> ListarAsync(
            FiltroAuditoria filtro, int pagina, int tamanhoPagina, CancellationToken token) =>
            Task.FromResult<(IReadOnlyList<RegistroAuditoria>, int)>(([], 0));

        public Task<IReadOnlyDictionary<TipoEventoAuditoria, int>> ContarPorTipoAsync(
            FiltroAuditoria filtro, CancellationToken token) =>
            Task.FromResult<IReadOnlyDictionary<TipoEventoAuditoria, int>>(
                new Dictionary<TipoEventoAuditoria, int>());

        public Task<IReadOnlyList<RegistroAuditoria>> ListarParaExportacaoAsync(
            FiltroAuditoria filtro, int limite, CancellationToken token) =>
            Task.FromResult<IReadOnlyList<RegistroAuditoria>>([]);
    }
}
