using System.Text;
using FluentAssertions;
using InvoiSys.Application.Historico.ExportarHistorico;
using InvoiSys.Domain.Entities;
using InvoiSys.Domain.Enums;
using InvoiSys.Domain.Repositories;

namespace InvoiSys.UnitTests.Application.Historico;

public sealed class ExportarHistoricoUseCaseTests
{
    [Fact]
    public async Task ExecutarAsync_DeveGerarCsvComCabecalhoEEventosDoFiltro()
    {
        var registro = new RegistroAuditoria(
            TipoEventoAuditoria.Autenticacao, "Login realizado", "admin@invoisys.local",
            null, Guid.NewGuid(), "admin@invoisys.local", "192.168.0.10");
        var repository = new RepositoryStub([registro]);
        var useCase = new ExportarHistoricoUseCase(new ExportarHistoricoValidator(), repository);

        var arquivo = await useCase.ExecutarAsync(
            new ExportarHistoricoRequest(Tipo: TipoEventoAuditoria.Autenticacao),
            TestContext.Current.CancellationToken);

        arquivo.ContentType.Should().Be("text/csv");
        arquivo.NomeArquivo.Should().StartWith("historico-").And.EndWith(".csv");
        repository.UltimoFiltro!.Tipo.Should().Be(TipoEventoAuditoria.Autenticacao);
        repository.UltimoLimite.Should().Be(ExportarHistoricoUseCase.LimiteRegistros);

        var linhas = LerLinhas(arquivo);
        linhas[0].Should().Be("Data e hora (UTC);Tipo;Atividade;Objeto;Responsável;Endereço IP");
        linhas[1].Should().EndWith(";Autenticação;Login realizado;admin@invoisys.local;admin@invoisys.local;192.168.0.10");
    }

    [Fact]
    public async Task ExecutarAsync_DeveEscaparSeparadorEFormulas()
    {
        var registro = new RegistroAuditoria(
            TipoEventoAuditoria.Fonte, "Fonte cadastrada", "=HYPERLINK(\"x\";\"y\")",
            null, null, "a;b@invoisys.local", null);
        var useCase = new ExportarHistoricoUseCase(
            new ExportarHistoricoValidator(), new RepositoryStub([registro]));

        var arquivo = await useCase.ExecutarAsync(
            new ExportarHistoricoRequest(),
            TestContext.Current.CancellationToken);

        var linha = LerLinhas(arquivo)[1];
        linha.Should().Contain(";\"'=HYPERLINK(\"\"x\"\";\"\"y\"\")\";");
        linha.Should().EndWith(";\"a;b@invoisys.local\";");
    }

    private static string[] LerLinhas(ArquivoExportado arquivo)
    {
        var bom = Encoding.UTF8.GetPreamble();
        arquivo.Conteudo.Take(bom.Length).Should().Equal(bom);

        return Encoding.UTF8.GetString(arquivo.Conteudo, bom.Length, arquivo.Conteudo.Length - bom.Length)
            .Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries);
    }

    private sealed class RepositoryStub(IReadOnlyList<RegistroAuditoria> itens) : IRegistroAuditoriaRepository
    {
        public FiltroAuditoria? UltimoFiltro { get; private set; }
        public int UltimoLimite { get; private set; }

        public Task AdicionarAsync(RegistroAuditoria registro, CancellationToken token) => Task.CompletedTask;

        public Task<(IReadOnlyList<RegistroAuditoria> Itens, int Total)> ListarAsync(
            FiltroAuditoria filtro, int pagina, int tamanhoPagina, CancellationToken token) =>
            Task.FromResult((itens, itens.Count));

        public Task<IReadOnlyDictionary<TipoEventoAuditoria, int>> ContarPorTipoAsync(
            FiltroAuditoria filtro, CancellationToken token) =>
            Task.FromResult<IReadOnlyDictionary<TipoEventoAuditoria, int>>(
                new Dictionary<TipoEventoAuditoria, int>());

        public Task<IReadOnlyList<RegistroAuditoria>> ListarParaExportacaoAsync(
            FiltroAuditoria filtro, int limite, CancellationToken token)
        {
            UltimoFiltro = filtro;
            UltimoLimite = limite;
            return Task.FromResult(itens);
        }
    }
}
