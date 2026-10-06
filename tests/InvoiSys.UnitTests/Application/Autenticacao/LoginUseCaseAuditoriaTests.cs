using FluentAssertions;
using InvoiSys.Application.Autenticacao.Login;
using InvoiSys.Application.Common.Auditoria;
using InvoiSys.Application.Common.Authentication;
using InvoiSys.Application.Common.Exceptions;
using InvoiSys.Domain.Enums;
using InvoiSys.Domain.Repositories;

namespace InvoiSys.UnitTests.Application.Autenticacao;

public sealed class LoginUseCaseAuditoriaTests
{
    [Fact]
    public async Task ExecutarAsync_DeveRegistrarLogin_QuandoCredenciaisValidas()
    {
        var usuario = new AuthenticatedUser(Guid.NewGuid(), "Admin", "admin@invoisys.local", ["Administrador"]);
        var auditoria = new AuditoriaServiceStub();
        var unitOfWork = new UnitOfWorkStub();
        var useCase = new LoginUseCase(
            new LoginValidator(), new AuthenticationServiceStub(usuario), auditoria, unitOfWork);

        await useCase.ExecutarAsync(
            new LoginRequest("admin@invoisys.local", "Senha@12345"),
            TestContext.Current.CancellationToken);

        var registro = auditoria.Registros.Should().ContainSingle().Subject;
        registro.Should().Be((TipoEventoAuditoria.Autenticacao, "Login realizado", (Guid?)usuario.Id, "admin@invoisys.local"));
        unitOfWork.Commits.Should().Be(1);
    }

    [Fact]
    public async Task ExecutarAsync_DeveRegistrarTentativaRecusada_QuandoCredenciaisInvalidas()
    {
        var auditoria = new AuditoriaServiceStub();
        var unitOfWork = new UnitOfWorkStub();
        var useCase = new LoginUseCase(
            new LoginValidator(), new AuthenticationServiceStub(null), auditoria, unitOfWork);

        var act = () => useCase.ExecutarAsync(
            new LoginRequest(" intruso@invoisys.local ", "Senha@12345"),
            TestContext.Current.CancellationToken);

        await act.Should().ThrowAsync<UnauthorizedException>();
        var registro = auditoria.Registros.Should().ContainSingle().Subject;
        registro.Should().Be((TipoEventoAuditoria.Autenticacao, "Tentativa de login recusada", (Guid?)null, "intruso@invoisys.local"));
        unitOfWork.Commits.Should().Be(1);
    }

    private sealed class AuthenticationServiceStub(AuthenticatedUser? usuario) : IAuthenticationService
    {
        public Task<AuthenticationResult> LoginAsync(string email, string senha, CancellationToken token) =>
            usuario is null
                ? throw new UnauthorizedException("E-mail ou senha inválidos.")
                : Task.FromResult(new AuthenticationResult(
                    "access", DateTimeOffset.UtcNow.AddMinutes(30), "refresh", DateTimeOffset.UtcNow.AddDays(7), usuario));

        public Task<AuthenticationResult> RefreshAsync(string refreshToken, CancellationToken token) =>
            throw new NotSupportedException();

        public Task<SessaoEncerrada?> LogoutAsync(string refreshToken, CancellationToken token) =>
            throw new NotSupportedException();
    }

    private sealed class AuditoriaServiceStub : IAuditoriaService
    {
        public List<(TipoEventoAuditoria Tipo, string Atividade, Guid? UsuarioId, string Responsavel)> Registros { get; } = [];

        public Task RegistrarAsync(TipoEventoAuditoria tipo, string atividade, string objeto,
            Guid? objetoId, CancellationToken cancellationToken) =>
            throw new NotSupportedException("O login deve informar o responsável explicitamente.");

        public Task RegistrarAsync(TipoEventoAuditoria tipo, string atividade, string objeto,
            Guid? objetoId, Guid? usuarioId, string responsavel, CancellationToken cancellationToken)
        {
            Registros.Add((tipo, atividade, usuarioId, responsavel));
            return Task.CompletedTask;
        }
    }

    private sealed class UnitOfWorkStub : IUnitOfWork
    {
        public int Commits { get; private set; }

        public Task<int> CommitAsync(CancellationToken token)
        {
            Commits++;
            return Task.FromResult(1);
        }
    }
}
