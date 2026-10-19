using InvoiSys.Application.Coletas.ExecutarColeta;
using InvoiSys.Domain.Repositories;
using Microsoft.Extensions.Options;

namespace InvoiSys.Api.Workers;

public sealed class ColetaAutomaticaWorker(
    IServiceScopeFactory scopeFactory,
    TimeProvider timeProvider,
    IOptions<ColetaAutomaticaOptions> options,
    ILogger<ColetaAutomaticaWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var configuracao = options.Value;
        using var timer = new PeriodicTimer(
            TimeSpan.FromSeconds(configuracao.IntervaloVerificacaoSegundos), timeProvider);

        do
        {
            try
            {
                await ExecutarCicloAsync(configuracao, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception exception)
            {
                logger.LogError(exception, "Falha ao verificar as Fontes para coleta automática.");
            }
        }
        while (await timer.WaitForNextTickAsync(stoppingToken));
    }

    private async Task ExecutarCicloAsync(
        ColetaAutomaticaOptions configuracao,
        CancellationToken stoppingToken)
    {
        IReadOnlyList<Guid> interrompidas;
        await using (var scope = scopeFactory.CreateAsyncScope())
        {
            interrompidas = await scope.ServiceProvider
                .GetRequiredService<IExecucaoColetaRepository>()
                .ListarFontesEmAndamentoAsync(stoppingToken);
        }

        await Parallel.ForEachAsync(interrompidas,
            new ParallelOptions
            {
                MaxDegreeOfParallelism = configuracao.ParalelismoMaximo,
                CancellationToken = stoppingToken
            },
            async (fonteId, token) =>
            {
                await ExecutarFonteAsync(fonteId, configuracao, true, token);
            });

        IReadOnlyList<Guid> vencidas;
        await using (var scope = scopeFactory.CreateAsyncScope())
        {
            vencidas = await scope.ServiceProvider
                .GetRequiredService<IFonteRepository>()
                .ListarIdsAtivasVencidasAsync(timeProvider.GetUtcNow(), stoppingToken);
        }

        await Parallel.ForEachAsync(vencidas,
            new ParallelOptions
            {
                MaxDegreeOfParallelism = configuracao.ParalelismoMaximo,
                CancellationToken = stoppingToken
            },
            async (fonteId, token) =>
            {
                await ExecutarFonteAsync(fonteId, configuracao, false, token);
            });
    }

    private async Task ExecutarFonteAsync(
        Guid fonteId,
        ColetaAutomaticaOptions configuracao,
        bool recuperar,
        CancellationToken stoppingToken)
    {
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(stoppingToken);
        timeout.CancelAfter(TimeSpan.FromSeconds(configuracao.TimeoutFonteSegundos));

        try
        {
            await using var scope = scopeFactory.CreateAsyncScope();
            var executor = scope.ServiceProvider.GetRequiredService<IColetaExecutor>();
            if (recuperar)
                await executor.RecuperarInterrompidaAsync(fonteId, timeout.Token);
            else
                await executor.ExecutarAutomaticaSeVencidaAsync(
                    fonteId, timeout.Token, stoppingToken);
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            // Encerramento normal do host.
        }
        catch (OperationCanceledException) when (timeout.IsCancellationRequested)
        {
            logger.LogWarning("Tempo limite da coleta da Fonte {FonteId} excedido.", fonteId);
        }
        catch (Exception exception)
        {
            logger.LogError(exception,
                "Falha ao {Operacao} a Fonte {FonteId}.",
                recuperar ? "recuperar" : "coletar", fonteId);
        }
    }
}
