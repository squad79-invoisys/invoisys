using FluentAssertions;
using InvoiSys.Application.Coletas;
using InvoiSys.Domain.Enums;
using Microsoft.Extensions.Logging.Abstractions;

namespace InvoiSys.UnitTests.Application.Coletas;

public sealed class ColetaMetricsTests
{
    [Fact]
    public void RegistrarSucesso_DeveEmitirTresInstrumentosSemDimensaoDeFonte()
    {
        using var fabrica = new TestMeterFactory();
        using var medicoes = new MedicoesColeta(fabrica);
        var metrics = new ColetaMetrics(fabrica, NullLogger<ColetaMetrics>.Instance);

        metrics.RegistrarSucesso(TipoFonte.Rss, TimeSpan.FromMilliseconds(250), 3);

        medicoes.Itens.Should().HaveCount(3);
        var executadas = medicoes.Itens.Single(m => m.Instrumento == "invoisys.coletas.executadas");
        executadas.Valor.Should().Be(1);
        executadas.Tags["status"].Should().Be("sucesso");
        executadas.Tags["tipo_fonte"].Should().Be("rss");
        medicoes.Itens.Single(m => m.Instrumento == "invoisys.coletas.duracao")
            .Valor.Should().Be(250);
        medicoes.Itens.Single(m => m.Instrumento == "invoisys.documentos.coletados")
            .Valor.Should().Be(3);
        medicoes.Itens.SelectMany(m => m.Tags.Keys).Distinct()
            .Should().BeSubsetOf(["status", "tipo_fonte"]);
    }

    [Fact]
    public void RegistrarFalha_DeveEmitirStatusFalhaSemContarDocumentos()
    {
        using var fabrica = new TestMeterFactory();
        using var medicoes = new MedicoesColeta(fabrica);
        var metrics = new ColetaMetrics(fabrica, NullLogger<ColetaMetrics>.Instance);

        metrics.RegistrarFalha(TipoFonte.WebHtml, TimeSpan.FromMilliseconds(40));

        medicoes.Itens.Should().HaveCount(2);
        medicoes.Itens.Should().OnlyContain(m => (string)m.Tags["status"]! == "falha");
        medicoes.Itens.Should().NotContain(m => m.Instrumento == "invoisys.documentos.coletados");
    }

    [Fact]
    public void Emissao_QuandoOListenerLanca_NaoDevePropagarExcecao()
    {
        using var fabrica = new TestMeterFactory();
        using var listener = new System.Diagnostics.Metrics.MeterListener();
        listener.InstrumentPublished = (i, l) =>
        {
            if (ReferenceEquals(i.Meter.Scope, fabrica))
                l.EnableMeasurementEvents(i);
        };
        listener.SetMeasurementEventCallback<long>((_, _, _, _) =>
            throw new InvalidOperationException("listener quebrado"));
        listener.Start();
        var metrics = new ColetaMetrics(fabrica, NullLogger<ColetaMetrics>.Instance);

        var act = () => metrics.RegistrarSucesso(TipoFonte.Atom, TimeSpan.FromSeconds(1), 1);

        act.Should().NotThrow();
    }
}