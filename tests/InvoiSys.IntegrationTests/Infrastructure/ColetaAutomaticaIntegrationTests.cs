using FluentAssertions;
using InvoiSys.Application.Coletas.ExecutarColeta;
using InvoiSys.Application.Common.Coletas;
using InvoiSys.Application.Common.Collectors;
using InvoiSys.Application.Common.Exceptions;
using InvoiSys.Domain.Entities;
using InvoiSys.Domain.Enums;
using InvoiSys.Domain.Repositories;
using InvoiSys.Infrastructure.Data;
using InvoiSys.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Npgsql;

namespace InvoiSys.IntegrationTests.Infrastructure;

[Collection("Aspire")]
public sealed class ColetaAutomaticaIntegrationTests(PostgresFixture fixture)
    : IClassFixture<PostgresFixture>
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    [Fact]
    public async Task Automatica_DeveRespeitarVencimentoStatusEDeduplicacao()
    {
        var collector = new CollectorStub();
        await using var services = CriarServices(collector);
        var vencida = await CriarFonteAsync(1, true);
        var recente = await CriarFonteAsync(60, false);
        var inativa = await CriarFonteAsync(1, true, false);

        await using var scope = services.CreateAsyncScope();
        var fontes = scope.ServiceProvider.GetRequiredService<IFonteRepository>();
        var ids = await fontes.ListarIdsAtivasVencidasAsync(DateTimeOffset.UtcNow, Ct);
        ids.Should().Contain(vencida.Id).And.NotContain(recente.Id).And.NotContain(inativa.Id);

        var executor = scope.ServiceProvider.GetRequiredService<IColetaExecutor>();
        var executada = await executor.ExecutarAutomaticaSeVencidaAsync(vencida.Id, Ct);
        executada.Should().NotBeNull();
        executada!.TipoExecucao.Should().Be(TipoExecucao.Automatica);
        executada.SolicitadaPorUsuarioId.Should().BeNull();
        executada.Status.Should().Be(StatusExecucaoColeta.Concluida);
        executada.QuantidadeDocumentos.Should().Be(1);

        (await executor.ExecutarAutomaticaSeVencidaAsync(vencida.Id, Ct)).Should().BeNull();
        (await executor.ExecutarAutomaticaSeVencidaAsync(recente.Id, Ct)).Should().BeNull();
        (await executor.ExecutarAutomaticaSeVencidaAsync(inativa.Id, Ct)).Should().BeNull();

        await using var db = fixture.CreateDbContext();
        (await db.Documentos.CountAsync(x => x.ExecucaoColetaId == executada.Id, Ct))
            .Should().Be(1);
    }

    [Fact]
    public async Task Lock_DeveImpedirManualEAutomaticaEmOutraInstanciaESerLiberadoAoPool()
    {
        var collector = new CollectorStub(bloquear: true);
        await using var instancia1 = CriarServices(collector);
        await using var instancia2 = CriarServices(collector);
        var fonte = await CriarFonteAsync(1, true);

        await using var scope1 = instancia1.CreateAsyncScope();
        var primeiro = scope1.ServiceProvider.GetRequiredService<IColetaExecutor>()
            .ExecutarAutomaticaSeVencidaAsync(fonte.Id, Ct);
        await collector.Entrou.WaitAsync(TimeSpan.FromSeconds(15), Ct);

        await using (var db = fixture.CreateDbContext())
        {
            (await db.ExecucoesColeta.SingleAsync(x => x.FonteId == fonte.Id, Ct))
                .Status.Should().Be(StatusExecucaoColeta.EmAndamento);
        }

        await using var scope2 = instancia2.CreateAsyncScope();
        var executor2 = scope2.ServiceProvider.GetRequiredService<IColetaExecutor>();
        (await executor2.ExecutarAutomaticaSeVencidaAsync(fonte.Id, Ct)).Should().BeNull();
        var manual = () => executor2.ExecutarManualAsync(fonte.Id, Guid.NewGuid(), Ct);
        await manual.Should().ThrowAsync<BusinessException>();

        collector.Liberar();
        (await primeiro).Should().NotBeNull();

        await using var scope3 = instancia1.CreateAsyncScope();
        var lock1 = scope3.ServiceProvider.GetRequiredService<IFonteColetaLock>();
        await using (var lease = await lock1.TentarAdquirirAsync(fonte.Id, Ct))
        {
            lease.Should().NotBeNull();
            (await scope2.ServiceProvider.GetRequiredService<IFonteColetaLock>()
                .TentarAdquirirAsync(fonte.Id, Ct)).Should().BeNull();
        }
        await using var lease2 = await scope2.ServiceProvider
            .GetRequiredService<IFonteColetaLock>().TentarAdquirirAsync(fonte.Id, Ct);
        lease2.Should().NotBeNull();
    }

    [Fact]
    public async Task Automatica_DeveAguardarColetaManualDaMesmaFonte()
    {
        var collector = new CollectorStub(bloquear: true);
        await using var services = CriarServices(collector);
        var fonte = await CriarFonteAsync(1, true);
        await using var scope1 = services.CreateAsyncScope();
        await using var scope2 = services.CreateAsyncScope();
        var manual = scope1.ServiceProvider.GetRequiredService<IColetaExecutor>()
            .ExecutarManualAsync(fonte.Id, Guid.NewGuid(), Ct);
        await collector.Entrou.WaitAsync(TimeSpan.FromSeconds(15), Ct);

        var automatica = await scope2.ServiceProvider.GetRequiredService<IColetaExecutor>()
            .ExecutarAutomaticaSeVencidaAsync(fonte.Id, Ct);
        automatica.Should().BeNull();

        collector.Liberar();
        (await manual).TipoExecucao.Should().Be(TipoExecucao.Manual);
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, true)]
    public async Task DuasEntradasDaMesmaFonte_NaoDevemIniciarSegundoColetor(
        bool primeiraManual, bool segundaManual)
    {
        var collector = new CollectorStub(bloquear: true);
        await using var instancia1 = CriarServices(collector);
        await using var instancia2 = CriarServices(collector);
        var fonte = await CriarFonteAsync(1, true);
        await using var scope1 = instancia1.CreateAsyncScope();
        await using var scope2 = instancia2.CreateAsyncScope();
        var executor1 = scope1.ServiceProvider.GetRequiredService<IColetaExecutor>();
        var executor2 = scope2.ServiceProvider.GetRequiredService<IColetaExecutor>();
        Task primeira = primeiraManual
            ? (Task)executor1.ExecutarManualAsync(fonte.Id, Guid.NewGuid(), Ct)
            : executor1.ExecutarAutomaticaSeVencidaAsync(fonte.Id, Ct);
        await collector.Entrou.WaitAsync(TimeSpan.FromSeconds(15), Ct);

        if (segundaManual)
        {
            var segunda = () => executor2.ExecutarManualAsync(fonte.Id, Guid.NewGuid(), Ct);
            await segunda.Should().ThrowAsync<BusinessException>();
        }
        else
        {
            (await executor2.ExecutarAutomaticaSeVencidaAsync(fonte.Id, Ct)).Should().BeNull();
        }

        collector.EntraramDuas.IsCompleted.Should().BeFalse();
        collector.Liberar();
        await primeira;
    }

    [Fact]
    public async Task FontesDiferentes_DevemExecutarEmParalelo()
    {
        var collector = new CollectorStub(bloquear: true);
        await using var services = CriarServices(collector);
        var fonte1 = await CriarFonteAsync(1, true);
        var fonte2 = await CriarFonteAsync(1, true);
        await using var scope1 = services.CreateAsyncScope();
        await using var scope2 = services.CreateAsyncScope();
        var primeira = scope1.ServiceProvider.GetRequiredService<IColetaExecutor>()
            .ExecutarAutomaticaSeVencidaAsync(fonte1.Id, Ct);
        var segunda = scope2.ServiceProvider.GetRequiredService<IColetaExecutor>()
            .ExecutarAutomaticaSeVencidaAsync(fonte2.Id, Ct);

        await collector.EntraramDuas.WaitAsync(TimeSpan.FromSeconds(15), Ct);
        collector.Liberar();
        (await primeira).Should().NotBeNull();
        (await segunda).Should().NotBeNull();
    }

    [Fact]
    public async Task Cancelamento_DeveEncerrarHistoricoELiberarLock()
    {
        var collector = new CollectorStub(bloquear: true);
        await using var services = CriarServices(collector);
        var fonte = await CriarFonteAsync(1, true);
        await using var scope = services.CreateAsyncScope();
        using var cancellation = new CancellationTokenSource();
        var tarefa = scope.ServiceProvider.GetRequiredService<IColetaExecutor>()
            .ExecutarAutomaticaSeVencidaAsync(fonte.Id, cancellation.Token);
        await collector.Entrou.WaitAsync(TimeSpan.FromSeconds(15), Ct);
        cancellation.Cancel();
        var act = () => tarefa;
        await act.Should().ThrowAsync<OperationCanceledException>();

        await using (var db = fixture.CreateDbContext())
        {
            var execucao = await db.ExecucoesColeta.SingleAsync(x => x.FonteId == fonte.Id, Ct);
            execucao.Status.Should().Be(StatusExecucaoColeta.Falhou);
            execucao.MensagemErro.Should().Contain("cancelada");
            (await db.Documentos.CountAsync(x => x.ExecucaoColetaId == execucao.Id, Ct))
                .Should().Be(0);
        }

        await using var outroScope = services.CreateAsyncScope();
        await using var lease = await outroScope.ServiceProvider
            .GetRequiredService<IFonteColetaLock>().TentarAdquirirAsync(fonte.Id, Ct);
        lease.Should().NotBeNull();
    }

    [Fact]
    public async Task CancelamentoDurantePersistencia_DeveReverterDocumentos()
    {
        var fonte = await CriarFonteAsync(1, true);
        await using var services = CriarServices(new CollectorStub());
        await using var bloqueador = new NpgsqlConnection(fixture.ConnectionString);
        await bloqueador.OpenAsync(Ct);
        await using var transacaoBloqueadora = await bloqueador.BeginTransactionAsync(Ct);
        await using (var command = new NpgsqlCommand(
            "LOCK TABLE documentos IN ACCESS EXCLUSIVE MODE", bloqueador, transacaoBloqueadora))
            await command.ExecuteNonQueryAsync(Ct);
        await using var observador = new NpgsqlConnection(fixture.ConnectionString);
        await observador.OpenAsync(Ct);

        await using var scope = services.CreateAsyncScope();
        using var cancellation = new CancellationTokenSource();
        var tarefa = scope.ServiceProvider.GetRequiredService<IColetaExecutor>()
            .ExecutarAutomaticaSeVencidaAsync(fonte.Id, cancellation.Token);

        var aguardandoBanco = false;
        var limite = DateTimeOffset.UtcNow.AddSeconds(15);
        while (DateTimeOffset.UtcNow < limite && !aguardandoBanco)
        {
            await using var command = new NpgsqlCommand("""
                SELECT EXISTS(SELECT 1 FROM pg_stat_activity
                    WHERE pid <> pg_backend_pid() AND wait_event_type = 'Lock'
                      AND query ILIKE '%documentos%')
                """, observador);
            aguardandoBanco = (bool)(await command.ExecuteScalarAsync(Ct))!;
            if (!aguardandoBanco)
                await Task.Delay(50, Ct);
        }
        aguardandoBanco.Should().BeTrue();

        cancellation.Cancel();
        var cancelada = () => tarefa;
        await cancelada.Should().ThrowAsync<OperationCanceledException>();
        await transacaoBloqueadora.RollbackAsync(Ct);

        await using var db = fixture.CreateDbContext();
        var execucao = await db.ExecucoesColeta.SingleAsync(x => x.FonteId == fonte.Id, Ct);
        execucao.Status.Should().Be(StatusExecucaoColeta.Falhou);
        (await db.Documentos.CountAsync(x => x.ExecucaoColetaId == execucao.Id, Ct)).Should().Be(0);
    }

    [Fact]
    public async Task Recuperacao_DeveEsperarLockDeOutraInstancia()
    {
        var fonte = await CriarFonteAsync(1, true);
        var aberta = ExecucaoColeta.IniciarAutomatica(fonte.Id);
        await using (var db = fixture.CreateDbContext())
        {
            db.ExecucoesColeta.Add(aberta);
            await db.SaveChangesAsync(Ct);
        }

        await using var instancia1 = CriarServices(new CollectorStub());
        await using var instancia2 = CriarServices(new CollectorStub());
        await using var scope1 = instancia1.CreateAsyncScope();
        await using var scope2 = instancia2.CreateAsyncScope();
        var lock1 = scope1.ServiceProvider.GetRequiredService<IFonteColetaLock>();
        var executor2 = scope2.ServiceProvider.GetRequiredService<IColetaExecutor>();

        await using (var lease = await lock1.TentarAdquirirAsync(fonte.Id, Ct))
        {
            lease.Should().NotBeNull();
            await executor2.RecuperarInterrompidaAsync(fonte.Id, Ct);
            await using var db = fixture.CreateDbContext();
            (await db.ExecucoesColeta.SingleAsync(x => x.Id == aberta.Id, Ct))
                .Status.Should().Be(StatusExecucaoColeta.EmAndamento);
        }

        await executor2.RecuperarInterrompidaAsync(fonte.Id, Ct);
        await using var verificador = fixture.CreateDbContext();
        var recuperada = await verificador.ExecucoesColeta.SingleAsync(x => x.Id == aberta.Id, Ct);
        recuperada.Status.Should().Be(StatusExecucaoColeta.Falhou);
        recuperada.Fim.Should().NotBeNull();
    }

    [Fact]
    public async Task PerdaDaSessao_DeveImpedirCommitEPermitirRecuperacao()
    {
        var collector = new CollectorStub(bloquear: true);
        var logs = new CapturingLogger();
        await using var services = CriarServices(collector, logs);
        var fonte = await CriarFonteAsync(1, true);
        await using var scope = services.CreateAsyncScope();
        var executor = scope.ServiceProvider.GetRequiredService<IColetaExecutor>();
        var tarefa = executor.ExecutarAutomaticaSeVencidaAsync(fonte.Id, Ct);
        await collector.Entrou.WaitAsync(TimeSpan.FromSeconds(15), Ct);

        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var lockConnection = (NpgsqlConnection)dbContext.Database.GetDbConnection();
        await using (var pidCommand = new NpgsqlCommand("SELECT pg_backend_pid()", lockConnection))
        {
            var pid = (int)(await pidCommand.ExecuteScalarAsync(Ct))!;
            await using var terminator = new NpgsqlConnection(fixture.ConnectionString);
            await terminator.OpenAsync(Ct);
            await using var terminateCommand = new NpgsqlCommand(
                "SELECT pg_terminate_backend(@pid)", terminator);
            terminateCommand.Parameters.AddWithValue("pid", pid);
            ((bool)(await terminateCommand.ExecuteScalarAsync(Ct))!).Should().BeTrue();
        }

        await using (var segundoScope = services.CreateAsyncScope())
        {
            var segundo = segundoScope.ServiceProvider.GetRequiredService<IColetaExecutor>();
            (await segundo.ExecutarAutomaticaSeVencidaAsync(fonte.Id, Ct)).Should().BeNull();
            var manual = () => segundo.ExecutarManualAsync(fonte.Id, Guid.NewGuid(), Ct);
            await manual.Should().ThrowAsync<BusinessException>();
        }

        var falha = () => tarefa;
        await falha.Should().ThrowAsync<ColetaPossePerdidaException>();
        logs.Messages.Should().Contain(message => message.Contains("não confirmada"));

        await using var verificador = fixture.CreateDbContext();
        (await verificador.ExecucoesColeta.SingleAsync(x => x.FonteId == fonte.Id, Ct))
            .Status.Should().Be(StatusExecucaoColeta.EmAndamento);

        await using var outroScope = services.CreateAsyncScope();
        await outroScope.ServiceProvider.GetRequiredService<IColetaExecutor>()
            .RecuperarInterrompidaAsync(fonte.Id, Ct);
        await verificador.Entry(await verificador.ExecucoesColeta.SingleAsync(
            x => x.FonteId == fonte.Id, Ct)).ReloadAsync(Ct);
        (await verificador.ExecucoesColeta.SingleAsync(x => x.FonteId == fonte.Id, Ct))
            .Status.Should().Be(StatusExecucaoColeta.EmAndamento);

        await ExpirarLeaseAsync(fonte.Id);
        await outroScope.ServiceProvider.GetRequiredService<IColetaExecutor>()
            .RecuperarInterrompidaAsync(fonte.Id, Ct);
        await verificador.Entry(await verificador.ExecucoesColeta.SingleAsync(
            x => x.FonteId == fonte.Id, Ct)).ReloadAsync(Ct);
        (await verificador.ExecucoesColeta.SingleAsync(x => x.FonteId == fonte.Id, Ct))
            .Status.Should().Be(StatusExecucaoColeta.Falhou);
    }

    [Fact]
    public async Task PerdaDaSessaoManual_DeveImpedirSegundaColetaSemHistoricoVisivel()
    {
        var collector = new CollectorStub(bloquear: true);
        await using var services = CriarServices(collector);
        var fonte = await CriarFonteAsync(1, true);
        await using var scope1 = services.CreateAsyncScope();
        var primeira = scope1.ServiceProvider.GetRequiredService<IColetaExecutor>()
            .ExecutarManualAsync(fonte.Id, Guid.NewGuid(), Ct);
        await collector.Entrou.WaitAsync(TimeSpan.FromSeconds(15), Ct);
        await using (var db = fixture.CreateDbContext())
            (await db.ExecucoesColeta.CountAsync(x => x.FonteId == fonte.Id, Ct)).Should().Be(0);

        await EncerrarSessaoAsync(scope1);
        await using var scope2 = services.CreateAsyncScope();
        (await scope2.ServiceProvider.GetRequiredService<IColetaExecutor>()
            .ExecutarAutomaticaSeVencidaAsync(fonte.Id, Ct)).Should().BeNull();
        var falha = () => primeira;
        await falha.Should().ThrowAsync<ColetaPossePerdidaException>();
        await using var verificador = fixture.CreateDbContext();
        (await verificador.ExecucoesColeta.CountAsync(x => x.FonteId == fonte.Id, Ct)).Should().Be(0);
        (await verificador.Documentos.CountAsync(x => x.UrlOriginal == fonte.Url, Ct)).Should().Be(0);
    }

    [Fact]
    public async Task TransacaoQueUltrapassaValidade_DeveReverterEscritas()
    {
        var fonte = await CriarFonteAsync(1, true);
        await using var services = CriarServices(new CollectorStub());
        await using var scope = services.CreateAsyncScope();
        var lease = await scope.ServiceProvider.GetRequiredService<IFonteColetaLock>()
            .TentarAdquirirAsync(fonte.Id, Ct);
        lease.Should().NotBeNull();
        await using (lease!)
        {
            (await lease.TentarReivindicarAsync(Ct)).Should().BeTrue();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            await using (var command = new NpgsqlCommand(
                "UPDATE fontes_coleta_lease SET valido_ate = clock_timestamp() + interval '2 seconds' WHERE fonte_id = @fonte",
                (NpgsqlConnection)db.Database.GetDbConnection()))
            {
                command.Parameters.AddWithValue("fonte", fonte.Id);
                (await command.ExecuteNonQueryAsync(Ct)).Should().Be(1);
            }

            var unidade = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
            await unidade.BeginTransactionAsync(Ct);
            db.ExecucoesColeta.Add(ExecucaoColeta.IniciarAutomatica(fonte.Id));
            await unidade.CommitAsync(Ct);
            await Task.Delay(TimeSpan.FromSeconds(3), Ct);
            var commit = () => unidade.CommitTransactionAsync(Ct);
            await commit.Should().ThrowAsync<ColetaPossePerdidaException>();
            await unidade.RollbackTransactionAsync(Ct);
        }

        await using var verificador = fixture.CreateDbContext();
        (await verificador.ExecucoesColeta.CountAsync(x => x.FonteId == fonte.Id, Ct)).Should().Be(0);
    }

    [Fact]
    public async Task ValidacaoInicialComPosseExpirada_DeveLimparTransacaoEManterErroOriginal()
    {
        var fonte = await CriarFonteAsync(1, true);
        await using var services = CriarServices(new CollectorStub());
        await using var scope = services.CreateAsyncScope();
        var lease = await scope.ServiceProvider.GetRequiredService<IFonteColetaLock>()
            .TentarAdquirirAsync(fonte.Id, Ct);
        lease.Should().NotBeNull();
        await using (lease!)
        {
            (await lease.TentarReivindicarAsync(Ct)).Should().BeTrue();
            await ExpirarLeaseAsync(fonte.Id);

            var unidade = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var primeira = () => unidade.BeginTransactionAsync(Ct);
            await primeira.Should().ThrowAsync<ColetaPossePerdidaException>();
            db.Database.CurrentTransaction.Should().BeNull();

            var segunda = () => unidade.BeginTransactionAsync(Ct);
            await segunda.Should().ThrowAsync<ColetaPossePerdidaException>();
            db.Database.CurrentTransaction.Should().BeNull();
        }

        await using var outroScope = services.CreateAsyncScope();
        await using var outroLock = await outroScope.ServiceProvider
            .GetRequiredService<IFonteColetaLock>().TentarAdquirirAsync(fonte.Id, Ct);
        outroLock.Should().NotBeNull();
    }

    [Fact]
    public async Task CancelamentoNaValidacaoInicial_DeveLimparTransacaoELiberarPosseAntesDoEscopo()
    {
        var fonte = await CriarFonteAsync(1, true);
        await using var services = CriarServices(new CollectorStub());
        await using var scope = services.CreateAsyncScope();
        var lease = await scope.ServiceProvider.GetRequiredService<IFonteColetaLock>()
            .TentarAdquirirAsync(fonte.Id, Ct);
        lease.Should().NotBeNull();
        await using (lease!)
        {
            (await lease.TentarReivindicarAsync(Ct)).Should().BeTrue();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var connection = (NpgsqlConnection)db.Database.GetDbConnection();
            await using var pidCommand = new NpgsqlCommand("SELECT pg_backend_pid()", connection);
            var pid = (int)(await pidCommand.ExecuteScalarAsync(Ct))!;

            await using var bloqueador = new NpgsqlConnection(fixture.ConnectionString);
            await bloqueador.OpenAsync(Ct);
            await using var transacaoBloqueadora = await bloqueador.BeginTransactionAsync(Ct);
            await using (var command = new NpgsqlCommand(
                "UPDATE fontes_coleta_lease SET valido_ate = valido_ate WHERE fonte_id = @fonte",
                bloqueador, transacaoBloqueadora))
            {
                command.Parameters.AddWithValue("fonte", fonte.Id);
                (await command.ExecuteNonQueryAsync(Ct)).Should().Be(1);
            }

            using var cancelamento = new CancellationTokenSource();
            var unidade = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
            var inicio = unidade.BeginTransactionAsync(cancelamento.Token);
            await using var observador = new NpgsqlConnection(fixture.ConnectionString);
            await observador.OpenAsync(Ct);
            var aguardando = false;
            var limite = DateTimeOffset.UtcNow.AddSeconds(15);
            while (!aguardando && DateTimeOffset.UtcNow < limite)
            {
                await using var command = new NpgsqlCommand("""
                    SELECT EXISTS(SELECT 1 FROM pg_stat_activity
                        WHERE pid = @pid AND wait_event_type = 'Lock'
                          AND query ILIKE '%fontes_coleta_lease%')
                    """, observador);
                command.Parameters.AddWithValue("pid", pid);
                aguardando = (bool)(await command.ExecuteScalarAsync(Ct))!;
                if (!aguardando)
                    await Task.Delay(50, Ct);
            }
            aguardando.Should().BeTrue();

            cancelamento.Cancel();
            var falha = () => inicio;
            await falha.Should().ThrowAsync<OperationCanceledException>();
            db.Database.CurrentTransaction.Should().BeNull();
            await transacaoBloqueadora.RollbackAsync(Ct);

            await lease.DisposeAsync();
            await using (var verificador = new NpgsqlConnection(fixture.ConnectionString))
            {
                await verificador.OpenAsync(Ct);
                await using var command = new NpgsqlCommand("""
                    SELECT valido_ate <= clock_timestamp()
                    FROM fontes_coleta_lease WHERE fonte_id = @fonte
                    """, verificador);
                command.Parameters.AddWithValue("fonte", fonte.Id);
                (await command.ExecuteScalarAsync(Ct)).Should().Be(true);
            }
        }

        await using var outroScope = services.CreateAsyncScope();
        await using var outroLock = await outroScope.ServiceProvider
            .GetRequiredService<IFonteColetaLock>().TentarAdquirirAsync(fonte.Id, Ct);
        outroLock.Should().NotBeNull();
    }

    [Fact]
    public async Task LiberacaoNaoConfirmada_NaoDeveAlterarProprietarioNovoNemDevolverLockAoPool()
    {
        var fonte = await CriarFonteAsync(1, true);
        var outroProprietario = Guid.NewGuid();
        var logs = new CapturingLogger();
        await using var services = CriarServices(new CollectorStub(), logs);
        await using var scope = services.CreateAsyncScope();
        var lease = await scope.ServiceProvider.GetRequiredService<IFonteColetaLock>()
            .TentarAdquirirAsync(fonte.Id, Ct);
        lease.Should().NotBeNull();
        (await lease!.TentarReivindicarAsync(Ct)).Should().BeTrue();
        await using (var db = fixture.CreateDbContext())
            await db.Database.ExecuteSqlInterpolatedAsync(
                $"UPDATE fontes_coleta_lease SET proprietario_id = {outroProprietario} WHERE fonte_id = {fonte.Id}", Ct);

        await lease.DisposeAsync();
        logs.Messages.Should().Contain(message => message.Contains("não confirmada"));
        await using (var db = new NpgsqlConnection(fixture.ConnectionString))
        {
            await db.OpenAsync(Ct);
            await using var command = new NpgsqlCommand(
                "SELECT proprietario_id FROM fontes_coleta_lease WHERE fonte_id = @fonte", db);
            command.Parameters.AddWithValue("fonte", fonte.Id);
            (await command.ExecuteScalarAsync(Ct)).Should().Be(outroProprietario);
        }
        await using var novoScope = services.CreateAsyncScope();
        await using var novoLease = await novoScope.ServiceProvider
            .GetRequiredService<IFonteColetaLock>().TentarAdquirirAsync(fonte.Id, Ct);
        novoLease.Should().NotBeNull();
        (await novoLease!.TentarReivindicarAsync(Ct)).Should().BeFalse();
    }

    private async Task ExpirarLeaseAsync(Guid fonteId)
    {
        await using var db = fixture.CreateDbContext();
        await db.Database.ExecuteSqlInterpolatedAsync(
            $"UPDATE fontes_coleta_lease SET valido_ate = clock_timestamp() - interval '1 second' WHERE fonte_id = {fonteId}", Ct);
    }

    private async Task EncerrarSessaoAsync(AsyncServiceScope scope)
    {
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var connection = (NpgsqlConnection)db.Database.GetDbConnection();
        await using var pidCommand = new NpgsqlCommand("SELECT pg_backend_pid()", connection);
        var pid = (int)(await pidCommand.ExecuteScalarAsync(Ct))!;
        await using var terminator = new NpgsqlConnection(fixture.ConnectionString);
        await terminator.OpenAsync(Ct);
        await using var command = new NpgsqlCommand("SELECT pg_terminate_backend(@pid)", terminator);
        command.Parameters.AddWithValue("pid", pid);
        (await command.ExecuteScalarAsync(Ct)).Should().Be(true);
    }

    private ServiceProvider CriarServices(CollectorStub collector, CapturingLogger? logs = null)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        if (logs is not null)
            services.AddSingleton<ILogger<PostgresFonteColetaLock>>(logs);
        services.AddSingleton(TimeProvider.System);
        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseNpgsql(fixture.ConnectionString));
        services.AddScoped<IFonteRepository, FonteRepository>();
        services.AddScoped<IExecucaoColetaRepository, ExecucaoColetaRepository>();
        services.AddScoped<IDocumentoRepository, DocumentoRepository>();
        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddScoped<IFonteColetaLock, PostgresFonteColetaLock>();
        services.AddScoped<IColetaExecutor, ColetaExecutor>();
        services.AddSingleton<ICollectorResolver>(new ResolverStub(collector));
        return services.BuildServiceProvider();
    }

    private async Task<Fonte> CriarFonteAsync(int periodicidade, bool vencida, bool ativa = true)
    {
        var fonte = new Fonte($"Fonte {Guid.NewGuid():N}",
            $"https://exemplo.com/{Guid.NewGuid():N}", TipoFonte.WebHtml,
            periodicidade, Guid.NewGuid(), "Outro");
        if (!ativa)
            fonte.Desativar(Guid.NewGuid());

        await using var db = fixture.CreateDbContext();
        db.Fontes.Add(fonte);
        await db.SaveChangesAsync(Ct);
        if (vencida)
        {
            await db.Fontes.Where(x => x.Id == fonte.Id)
                .ExecuteUpdateAsync(setters => setters.SetProperty(
                    x => x.CriadoEm, DateTimeOffset.UtcNow.AddMinutes(-5)), Ct);
        }
        return fonte;
    }

    private sealed class ResolverStub(CollectorStub collector) : ICollectorResolver
    {
        public IContentCollector Resolver(TipoFonte tipo) => collector;
    }

    private sealed class CapturingLogger : ILogger<PostgresFonteColetaLock>
    {
        public List<string> Messages { get; } = [];
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
        public bool IsEnabled(LogLevel logLevel) => true;
        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state,
            Exception? exception, Func<TState, Exception?, string> formatter) =>
            Messages.Add(formatter(state, exception));
    }

    private sealed class CollectorStub(bool bloquear = false) : IContentCollector
    {
        private readonly TaskCompletionSource _entrou = new(TaskCreationOptions.RunContinuationsAsynchronously);
        private readonly TaskCompletionSource _entraramDuas = new(TaskCreationOptions.RunContinuationsAsynchronously);
        private readonly TaskCompletionSource _liberado = new(TaskCreationOptions.RunContinuationsAsynchronously);
        private int _acessos;
        public Task Entrou => _entrou.Task;
        public Task EntraramDuas => _entraramDuas.Task;
        public void Liberar() => _liberado.TrySetResult();
        public bool Suporta(TipoFonte tipo) => true;

        public async Task<IReadOnlyList<CollectedDocument>> ColetarAsync(
            string url, string? seletorConteudo, CancellationToken cancellationToken)
        {
            _entrou.TrySetResult();
            if (Interlocked.Increment(ref _acessos) == 2)
                _entraramDuas.TrySetResult();
            if (bloquear)
                await _liberado.Task.WaitAsync(cancellationToken);
            var item = new CollectedDocument("Documento", url, "WEB", url,
                null, "Conteúdo", null, new string('a', 64));
            return [item, item];
        }
    }
}
