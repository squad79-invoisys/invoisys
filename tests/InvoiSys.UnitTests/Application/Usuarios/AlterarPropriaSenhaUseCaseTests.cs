using FluentAssertions;
using FluentValidation;
using InvoiSys.Application.Common.Abstractions;
using InvoiSys.Application.Common.Auditoria;
using InvoiSys.Application.Common.Authentication;
using InvoiSys.Application.Common.Exceptions;
using InvoiSys.Application.Usuarios.AlterarPropriaSenha;
using InvoiSys.Domain.Enums;
using InvoiSys.Domain.Repositories;

namespace InvoiSys.UnitTests.Application.Usuarios;

public sealed class AlterarPropriaSenhaUseCaseTests
{
    private static readonly AlterarPropriaSenhaRequest RequestValido = new("Senha@Atual1", "Senha@Nova22");

    [Fact]
    public async Task Execute_DeveAlterarSenhaDoUsuarioLogadoEManterSessaoAtual()
    {
        var usuarioId = Guid.NewGuid();
        var sessaoId = Guid.NewGuid();
        var fixture = new Fixture(usuarioId);

        await fixture.CriarUseCase().Execute(
            RequestValido,
            sessaoId,
            TestContext.Current.CancellationToken);

        fixture.Usuarios.Chamada.Should().Be((usuarioId, "Senha@Atual1", "Senha@Nova22", (Guid?)sessaoId));
        fixture.Auditoria.Registros.Should().ContainSingle()
            .Which.Should().Be((TipoEventoAuditoria.Usuario, "Senha alterada pelo próprio usuário", "ana@invoisys.test"));
        fixture.UnitOfWork.Commits.Should().Be(1);
    }

    [Fact]
    public async Task Execute_DeveRecusar_QuandoNaoHouverUsuarioLogado()
    {
        var fixture = new Fixture(null);

        var act = () => fixture.CriarUseCase().Execute(
            RequestValido,
            null,
            TestContext.Current.CancellationToken);

        await act.Should().ThrowAsync<UnauthorizedException>();
        fixture.Usuarios.Chamada.Should().BeNull();
    }

    [Fact]
    public async Task Execute_NaoDeveAlterarSenha_QuandoRequestForInvalido()
    {
        var fixture = new Fixture(Guid.NewGuid());

        var act = () => fixture.CriarUseCase().Execute(
            new AlterarPropriaSenhaRequest("Senha@Atual1", "fraca"),
            null,
            TestContext.Current.CancellationToken);

        await act.Should().ThrowAsync<ValidationException>();
        fixture.Usuarios.Chamada.Should().BeNull();
        fixture.UnitOfWork.Commits.Should().Be(0);
    }

    [Fact]
    public async Task Execute_NaoDeveRegistrarAuditoria_QuandoSenhaAtualEstiverIncorreta()
    {
        var fixture = new Fixture(Guid.NewGuid());
        fixture.Usuarios.Erro = new BusinessException("A senha atual está incorreta.");

        var act = () => fixture.CriarUseCase().Execute(
            RequestValido,
            null,
            TestContext.Current.CancellationToken);

        await act.Should().ThrowAsync<BusinessException>()
            .WithMessage("A senha atual está incorreta.");
        fixture.Auditoria.Registros.Should().BeEmpty();
        fixture.UnitOfWork.Commits.Should().Be(0);
    }

    private sealed class Fixture(Guid? usuarioId)
    {
        public UsuariosStub Usuarios { get; } = new();
        public AuditoriaStub Auditoria { get; } = new();
        public UnitOfWorkStub UnitOfWork { get; } = new();

        public AlterarPropriaSenhaUseCase CriarUseCase() =>
            new(new AlterarPropriaSenhaValidator(), new UsuarioStub(usuarioId), Usuarios, Auditoria, UnitOfWork);
    }

    private sealed class UsuarioStub(Guid? usuarioId) : ICurrentUser
    {
        public Guid? UsuarioId => usuarioId;
        public string? Email => "ana@invoisys.test";
        public bool EstaAutenticado => usuarioId is not null;
    }

    private sealed class UsuariosStub : IUserManagementService
    {
        public (Guid UsuarioId, string SenhaAtual, string NovaSenha, Guid? SessaoAtualId)? Chamada { get; private set; }
        public Exception? Erro { get; set; }

        public Task<UserSummary> AlterarPropriaSenhaAsync(Guid usuarioId, string senhaAtual, string novaSenha,
            Guid? sessaoAtualId, CancellationToken cancellationToken)
        {
            if (Erro is not null)
                return Task.FromException<UserSummary>(Erro);

            Chamada = (usuarioId, senhaAtual, novaSenha, sessaoAtualId);
            return Task.FromResult(new UserSummary(usuarioId, "Ana", "ana@invoisys.test", true, ["Operador"]));
        }

        public Task<UserSummary> CriarAsync(string nome, string email, string senha, string perfil,
            CancellationToken cancellationToken) => throw new NotSupportedException();

        public Task<IReadOnlyList<UserSummary>> ListarAsync(CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<UserSummary> DefinirStatusAsync(Guid usuarioId, bool ativo, CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<UserSummary> RedefinirSenhaAsync(Guid usuarioId, string novaSenha,
            CancellationToken cancellationToken) => throw new NotSupportedException();
    }

    private sealed class AuditoriaStub : IAuditoriaService
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

    private sealed class UnitOfWorkStub : IUnitOfWork
    {
        public int Commits { get; private set; }

        public Task BeginTransactionAsync(CancellationToken cancellationToken) => Task.CompletedTask;
        public Task CommitTransactionAsync(CancellationToken cancellationToken) => Task.CompletedTask;
        public Task RollbackTransactionAsync(CancellationToken cancellationToken) => Task.CompletedTask;

        public Task<int> CommitAsync(CancellationToken cancellationToken)
        {
            Commits++;
            return Task.FromResult(1);
        }
    }
}
