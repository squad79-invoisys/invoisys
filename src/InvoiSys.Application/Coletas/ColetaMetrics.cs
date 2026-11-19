using System.Diagnostics.Metrics;
using InvoiSys.Domain.Enums;
using Microsoft.Extensions.Logging;

namespace InvoiSys.Application.Coletas;

/// <summary>
/// Métricas da operação de coleta. As dimensões são só status e tipo_fonte
/// (valores finitos). Id ou nome da Fonte NÃO entram como dimensão: cada valor
/// novo criaria uma série temporal própria e a cardinalidade explodiria.
/// </summary>
public sealed class ColetaMetrics
{
    public const string MeterName = "InvoiSys.Coletas";
    public const string StatusSucesso = "sucesso";
    public const string StatusFalha = "falha";

    private readonly Counter<long> executadas;
    private readonly Histogram<double> duracao;
    private readonly Counter<long> documentos;
    private readonly ILogger<ColetaMetrics> logger;

    public ColetaMetrics(IMeterFactory meterFactory, ILogger<ColetaMetrics> logger)
    {
        this.logger = logger;

        var meter = meterFactory.Create(MeterName);
        executadas = meter.CreateCounter<long>(
            "invoisys.coletas.executadas",
            unit: "{coleta}",
            description: "Execuções de coleta encerradas, por status e tipo de fonte.");
        duracao = meter.CreateHistogram<double>(
            "invoisys.coletas.duracao",
            unit: "ms",
            description: "Duração das execuções de coleta.");
        documentos = meter.CreateCounter<long>(
            "invoisys.documentos.coletados",
            unit: "{documento}",
            description: "Documentos gravados pelas coletas concluídas.");
    }

    public void RegistrarSucesso(TipoFonte tipoFonte, TimeSpan duracaoColeta, int documentosGravados) =>
        Emitir(() =>
        {
            var tipo = Tipo(tipoFonte);
            executadas.Add(1, Status(StatusSucesso), tipo);
            duracao.Record(duracaoColeta.TotalMilliseconds, Status(StatusSucesso), tipo);
            documentos.Add(documentosGravados, tipo);
        });

    public void RegistrarFalha(TipoFonte tipoFonte, TimeSpan duracaoColeta) =>
        Emitir(() =>
        {
            var tipo = Tipo(tipoFonte);
            executadas.Add(1, Status(StatusFalha), tipo);
            duracao.Record(duracaoColeta.TotalMilliseconds, Status(StatusFalha), tipo);
        });

    // Emitir métrica nunca pode alterar o resultado da coleta.
    private void Emitir(Action emissao)
    {
        try
        {
            emissao();
        }
        catch (Exception exception)
        {
            logger.LogWarning(exception, "Falha ao registrar as métricas da coleta.");
        }
    }

    private static KeyValuePair<string, object?> Status(string valor) => new("status", valor);

    private static KeyValuePair<string, object?> Tipo(TipoFonte tipoFonte) =>
        new("tipo_fonte", tipoFonte.ToString().ToLowerInvariant());
}