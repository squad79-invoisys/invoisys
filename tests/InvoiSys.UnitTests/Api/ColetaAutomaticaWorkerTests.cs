using FluentAssertions;
using InvoiSys.Api.Workers;
using InvoiSys.Application.Coletas;
using InvoiSys.Application.Coletas.ExecutarColeta;
using InvoiSys.Domain.Entities;
using InvoiSys.Domain.Enums;
using InvoiSys.Domain.Repositories;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace InvoiSys.UnitTests.Api;

public sealed class ColetaAutomaticaWorkerTests
{
    [Fact]
    public async Task HostStopAsync_DeveCancelarColetaEAguardarWorker()
    {
        var fonte = Guid.NewGuid();
        var executor = new BlockingExecutorStub();
        var builder = Host.CreateApplicationBuilder();
        builder.Services.AddSingleton(TimeProvider.System);
        builder.Services.AddSingleton<IFonteRepository>(new FonteStub([fonte]));
        builder.Services.AddSingleton<IExecucaoColetaRepository>(new ExecucaoStub());
        builder.Services.AddSingleton<IColetaExecutor>(executor);
        builder.Services.Configure<ColetaAutomaticaOptions>(options =>
        {
            options.IntervaloVerificacaoSegundos = 1;
            options.TimeoutFonteSegundos = 30;
            options.ParalelismoMaximo = 1;
        });
        builder.Services.AddHostedService<ColetaAutomaticaWorker>();

        using var host = builder.Build();
        await host.StartAsync(TestContext.Current.CancellationToken);
        await executor.Iniciou.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);
        await host.StopAsync(TestContext.Current.CancellationToken);
        await executor.Cancelou.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);
        var iniciadas = executor.Iniciadas;
        await Task.Delay(100, TestContext.Current.CancellationToken);
        executor.Iniciadas.Should().Be(iniciadas);
    }

    [Fact]
    public async Task ExecuteAsync_DeveContinuarComOutrasFontesAposFalha()
    {
        var falha = Guid.NewGuid();
        var sucesso = Guid.NewGuid();
        var executor = new ExecutorStub(falha, sucesso);
        var services = new ServiceCollection();
        services.AddSingleton<IFonteRepository>(new FonteStub([falha, sucesso]));
        services.AddSingleton<IExecucaoColetaRepository>(new ExecucaoStub());
        services.AddSingleton<IColetaExecutor>(executor);
        await using var provider = services.BuildServiceProvider();
        using var worker = new ColetaAutomaticaWorker(
            provider.GetRequiredService<IServiceScopeFactory>(),
            TimeProvider.System,
            Options.Create(new ColetaAutomaticaOptions
            {
                IntervaloVerificacaoSegundos = 60,
                ParalelismoMaximo = 2,
                TimeoutFonteSegundos = 10
            }),
            NullLogger<ColetaAutomaticaWorker>.Instance);

        await worker.StartAsync(TestContext.Current.CancellationToken);
        await executor.Concluiu.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);
        await worker.StopAsync(TestContext.Current.CancellationToken);

        executor.Executadas.Should().Contain([falha, sucesso]);
    }

    private sealed class ExecutorStub(Guid falha, Guid sucesso) : IColetaExecutor
    {
        private readonly TaskCompletionSource _concluiu =
            new(TaskCreationOptions.RunContinuationsAsynchronously);
        public Task Concluiu => _concluiu.Task;
        public List<Guid> Executadas { get; } = [];
        public Task<ExecucaoColetaResponse> ExecutarManualAsync(
            Guid fonteId, Guid usuarioId, CancellationToken token) =>
            throw new NotSupportedException();

        public Task RecuperarInterrompidaAsync(Guid fonteId, CancellationToken token) =>
            Task.CompletedTask;

        public Task<ExecucaoColetaResponse?> ExecutarAutomaticaSeVencidaAsync(
            Guid fonteId, CancellationToken token,
            CancellationToken encerramentoToken = default)
        {
            lock (Executadas)
            {
                Executadas.Add(fonteId);
                if (Executadas.Contains(falha) && Executadas.Contains(sucesso))
                    _concluiu.TrySetResult();
            }
            if (fonteId == falha)
                throw new InvalidOperationException("Fonte indisponível.");
            return Task.FromResult<ExecucaoColetaResponse?>(null);
        }
    }

    private sealed class BlockingExecutorStub : IColetaExecutor
    {
        private readonly TaskCompletionSource _iniciou = new(TaskCreationOptions.RunContinuationsAsynchronously);
        private readonly TaskCompletionSource _cancelou = new(TaskCreationOptions.RunContinuationsAsynchronously);
        private int _iniciadas;
        public Task Iniciou => _iniciou.Task;
        public Task Cancelou => _cancelou.Task;
        public int Iniciadas => Volatile.Read(ref _iniciadas);
        public Task<ExecucaoColetaResponse> ExecutarManualAsync(Guid fonteId, Guid usuarioId,
            CancellationToken token) => throw new NotSupportedException();
        public Task RecuperarInterrompidaAsync(Guid fonteId, CancellationToken token) => Task.CompletedTask;
        public async Task<ExecucaoColetaResponse?> ExecutarAutomaticaSeVencidaAsync(
            Guid fonteId, CancellationToken token, CancellationToken encerramentoToken = default)
        {
            Interlocked.Increment(ref _iniciadas);
            _iniciou.TrySetResult();
            try { await Task.Delay(Timeout.InfiniteTimeSpan, token); }
            catch (OperationCanceledException) when (token.IsCancellationRequested)
            { _cancelou.TrySetResult(); throw; }
            return null;
        }
    }

    private sealed class FonteStub(IReadOnlyList<Guid> ids) : IFonteRepository
    {
        public Task<IReadOnlyList<Guid>> ListarIdsAtivasVencidasAsync(
            DateTimeOffset agoraUtc, CancellationToken token) => Task.FromResult(ids);
        public Task AdicionarAsync(Fonte fonte, CancellationToken token) => throw new NotSupportedException();
        public Task<Fonte?> ObterPorIdAsync(Guid id, CancellationToken token) =>
            throw new NotSupportedException();
        public Task<(IReadOnlyList<Fonte> Itens, int Total)> ListarAsync(
            int pagina, int tamanhoPagina, TipoFonte? tipo, StatusFonte? status,
            string? contexto, string? busca, CancellationToken token) =>
            throw new NotSupportedException();
    }

    private sealed class ExecucaoStub : IExecucaoColetaRepository
    {
        public Task<IReadOnlyList<Guid>> ListarFontesEmAndamentoAsync(CancellationToken token) =>
            Task.FromResult<IReadOnlyList<Guid>>([]);
        public Task<ExecucaoColeta?> ObterUltimaPorFonteAsync(Guid id, CancellationToken token) =>
            throw new NotSupportedException();
        public Task<IReadOnlyList<ExecucaoColeta>> ListarEmAndamentoPorFonteAsync(
            Guid id, CancellationToken token) => throw new NotSupportedException();
        public Task AdicionarAsync(ExecucaoColeta execucao, CancellationToken token) =>
            throw new NotSupportedException();
        public Task<ExecucaoColeta?> ObterPorIdAsync(Guid id, CancellationToken token) =>
            throw new NotSupportedException();
        public Task<(IReadOnlyList<ExecucaoColeta> Itens, int Total)> ListarAsync(
            int pagina, int tamanhoPagina, Guid? fonteId, StatusExecucaoColeta? status,
            CancellationToken token) => throw new NotSupportedException();
    }
}
