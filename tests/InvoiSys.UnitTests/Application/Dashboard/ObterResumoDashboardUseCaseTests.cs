using FluentAssertions;
using InvoiSys.Application.Dashboard.ObterResumo;
using InvoiSys.Domain.Entities;
using InvoiSys.Domain.Enums;
using InvoiSys.Domain.Repositories;

namespace InvoiSys.UnitTests.Application.Dashboard;

public sealed class ObterResumoDashboardUseCaseTests
{
    [Fact]
    public async Task Execute_DeveDevolverContadoresEListasDoRepositorio()
    {
        var execucaoId = Guid.NewGuid();
        var fonteId = Guid.NewGuid();
        var repository = new DashboardRepositoryStub
        {
            FontesPorStatus =
            {
                [StatusFonte.Ativa] = 7,
                [StatusFonte.Inativa] = 2
            },
            ColetasTotais = 12,
            ColetasComFalha = 3,
            TotalDocumentos = 148,
            ExecucoesRecentes =
            [
                new ExecucaoRecente(
                    execucaoId,
                    fonteId,
                    "Portal DF-e",
                    DateTimeOffset.UtcNow.AddMinutes(-10),
                    DateTimeOffset.UtcNow.AddMinutes(-9),
                    StatusExecucaoColeta.Concluida,
                    24)
            ],
            DocumentosRecentes = [CriarDocumento("Nota técnica 2026.002")]
        };
        var useCase = new ObterResumoDashboardUseCase(repository);

        var resumo = await useCase.Execute(CancellationToken.None);

        resumo.FontesAtivas.Should().Be(7);
        resumo.FontesInativas.Should().Be(2);
        resumo.ColetasUltimas24h.Should().Be(12);
        resumo.ColetasComFalhaUltimas24h.Should().Be(3);
        resumo.TotalDocumentos.Should().Be(148);

        resumo.ExecucoesRecentes.Should().ContainSingle();
        resumo.ExecucoesRecentes[0].Id.Should().Be(execucaoId);
        resumo.ExecucoesRecentes[0].FonteId.Should().Be(fonteId);
        resumo.ExecucoesRecentes[0].FonteNome.Should().Be("Portal DF-e");
        resumo.ExecucoesRecentes[0].QuantidadeDocumentos.Should().Be(24);

        resumo.DocumentosRecentes.Should().ContainSingle();
        resumo.DocumentosRecentes[0].Titulo.Should().Be("Nota técnica 2026.002");
    }

    [Fact]
    public async Task Execute_ComBaseVazia_DeveDevolverZerosEListasVazias()
    {
        var useCase = new ObterResumoDashboardUseCase(new DashboardRepositoryStub());

        var resumo = await useCase.Execute(CancellationToken.None);

        resumo.FontesAtivas.Should().Be(0);
        resumo.FontesInativas.Should().Be(0);
        resumo.ColetasUltimas24h.Should().Be(0);
        resumo.ColetasComFalhaUltimas24h.Should().Be(0);
        resumo.TotalDocumentos.Should().Be(0);
        resumo.ExecucoesRecentes.Should().BeEmpty();
        resumo.DocumentosRecentes.Should().BeEmpty();
    }

    [Fact]
    public async Task Execute_DeveContarColetasDasUltimas24Horas()
    {
        var repository = new DashboardRepositoryStub();
        var useCase = new ObterResumoDashboardUseCase(repository);
        var antes = DateTimeOffset.UtcNow;

        await useCase.Execute(CancellationToken.None);

        repository.ConsultasDeColeta.Should().HaveCount(2);
        repository.ConsultasDeColeta.Should().OnlyContain(consulta =>
            consulta.Desde >= antes.AddHours(-24).AddSeconds(-5) &&
            consulta.Desde <= antes.AddHours(-24).AddSeconds(5));
    }

    [Fact]
    public async Task Execute_DeveContarFalhasApenasComStatusFalhou()
    {
        var repository = new DashboardRepositoryStub();
        var useCase = new ObterResumoDashboardUseCase(repository);

        await useCase.Execute(CancellationToken.None);

        repository.ConsultasDeColeta.Should().ContainSingle(consulta => consulta.Status == null);
        repository.ConsultasDeColeta.Should().ContainSingle(consulta =>
            consulta.Status == StatusExecucaoColeta.Falhou);
    }

    [Fact]
    public async Task Execute_DevePedirCincoRegistrosNasListasRecentes()
    {
        var repository = new DashboardRepositoryStub();
        var useCase = new ObterResumoDashboardUseCase(repository);

        await useCase.Execute(CancellationToken.None);

        repository.QuantidadeExecucoesPedida.Should().Be(5);
        repository.QuantidadeDocumentosPedida.Should().Be(5);
    }

    private static Documento CriarDocumento(string titulo) =>
        new(
            Guid.NewGuid(),
            titulo,
            "https://dfe-portal.svrs.rs.gov.br/aviso",
            "Notícia",
            "Portal DF-e",
            "Legislação",
            DateTimeOffset.UtcNow.AddDays(-1),
            "Conteúdo de exemplo.",
            null,
            "hash-de-exemplo");

    private sealed record ConsultaDeColeta(
        DateTimeOffset Desde,
        StatusExecucaoColeta? Status);

    private sealed class DashboardRepositoryStub : IDashboardRepository
    {
        public Dictionary<StatusFonte, int> FontesPorStatus { get; } = [];
        public int ColetasTotais { get; init; }
        public int ColetasComFalha { get; init; }
        public int TotalDocumentos { get; init; }
        public IReadOnlyList<ExecucaoRecente> ExecucoesRecentes { get; init; } = [];
        public IReadOnlyList<Documento> DocumentosRecentes { get; init; } = [];

        public List<ConsultaDeColeta> ConsultasDeColeta { get; } = [];
        public int QuantidadeExecucoesPedida { get; private set; }
        public int QuantidadeDocumentosPedida { get; private set; }

        public Task<int> ContarFontesPorStatusAsync(
            StatusFonte status,
            CancellationToken cancellationToken) =>
            Task.FromResult(FontesPorStatus.GetValueOrDefault(status));

        public Task<int> ContarColetasDesdeAsync(
            DateTimeOffset desde,
            StatusExecucaoColeta? status,
            CancellationToken cancellationToken)
        {
            ConsultasDeColeta.Add(new ConsultaDeColeta(desde, status));
            return Task.FromResult(
                status == StatusExecucaoColeta.Falhou ? ColetasComFalha : ColetasTotais);
        }

        public Task<int> ContarDocumentosAsync(CancellationToken cancellationToken) =>
            Task.FromResult(TotalDocumentos);

        public Task<IReadOnlyList<ExecucaoRecente>> ListarExecucoesRecentesAsync(
            int quantidade,
            CancellationToken cancellationToken)
        {
            QuantidadeExecucoesPedida = quantidade;
            return Task.FromResult(ExecucoesRecentes);
        }

        public Task<IReadOnlyList<Documento>> ListarDocumentosRecentesAsync(
            int quantidade,
            CancellationToken cancellationToken)
        {
            QuantidadeDocumentosPedida = quantidade;
            return Task.FromResult(DocumentosRecentes);
        }
    }
}
