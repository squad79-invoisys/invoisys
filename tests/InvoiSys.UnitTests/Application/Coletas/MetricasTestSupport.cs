using System.Diagnostics.Metrics;
using InvoiSys.Application.Coletas;
using Microsoft.Extensions.Logging.Abstractions;

namespace InvoiSys.UnitTests.Application.Coletas;

internal sealed class TestMeterFactory : IMeterFactory
{
    private readonly List<Meter> meters = [];

    public Meter Create(MeterOptions options)
    {
        var meter = new Meter(options.Name, options.Version, options.Tags, scope: this);
        meters.Add(meter);
        return meter;
    }

    public void Dispose()
    {
        foreach (var meter in meters)
            meter.Dispose();
        meters.Clear();
    }
}

internal static class TestMetrics
{
    public static ColetaMetrics Criar() =>
        new(new TestMeterFactory(), NullLogger<ColetaMetrics>.Instance);
}

internal sealed record Medicao(
    string Instrumento, double Valor, IReadOnlyDictionary<string, object?> Tags);

/// <summary>Escuta só os medidores criados pela fábrica informada (testes rodam em paralelo).</summary>
internal sealed class MedicoesColeta : IDisposable
{
    private readonly MeterListener listener = new();

    public List<Medicao> Itens { get; } = [];

    public MedicoesColeta(TestMeterFactory fabrica)
    {
        listener.InstrumentPublished = (instrumento, l) =>
        {
            if (instrumento.Meter.Name == ColetaMetrics.MeterName &&
                ReferenceEquals(instrumento.Meter.Scope, fabrica))
                l.EnableMeasurementEvents(instrumento);
        };
        listener.SetMeasurementEventCallback<long>((i, valor, tags, _) =>
            Itens.Add(new Medicao(i.Name, valor, tags.ToArray().ToDictionary(t => t.Key, t => t.Value))));
        listener.SetMeasurementEventCallback<double>((i, valor, tags, _) =>
            Itens.Add(new Medicao(i.Name, valor, tags.ToArray().ToDictionary(t => t.Key, t => t.Value))));
        listener.Start();
    }

    public void Dispose() => listener.Dispose();
}