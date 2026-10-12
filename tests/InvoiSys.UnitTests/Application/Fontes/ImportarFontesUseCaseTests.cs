using System.Text;
using FluentAssertions;
using InvoiSys.Application.Common.Abstractions;
using InvoiSys.Application.Common.Auditoria;
using InvoiSys.Application.Common.Exceptions;
using InvoiSys.Application.Common.Fontes;
using InvoiSys.Application.Fontes.CriarFonte;
using InvoiSys.Application.Fontes.ImportarFontes;
using InvoiSys.Domain.Entities;
using InvoiSys.Domain.Enums;
using InvoiSys.Domain.Repositories;

namespace InvoiSys.UnitTests.Application.Fontes;

public sealed class ImportarFontesUseCaseTests
{
    private const string Cabecalho = "nome,url,tipo,periodicidade";

    [Fact]
    public async Task ExecutarAsync_DeveCadastrarTodasAsLinhas_QuandoCsvTemSomenteAsQuatroColunas()
    {
        var ambiente = new Ambiente();

        var response = await ambiente.ImportarAsync(
            Cabecalho + "\nPortal A,https://a.com/feed.xml,Rss,30\nPortal B,https://b.com/atom,atom,60\n");

        response.TotalLinhas.Should().Be(2);
        response.Cadastradas.Should().Be(2);
        response.Rejeitadas.Should().BeEmpty();
        ambiente.Repository.Adicionadas.Should().HaveCount(2);
        var primeira = ambiente.Repository.Adicionadas[0];
        primeira.Nome.Should().Be("Portal A");
        primeira.Url.Should().Be("https://a.com/feed.xml");
        primeira.Tipo.Should().Be(TipoFonte.Rss);
        primeira.PeriodicidadeMinutos.Should().Be(30);
        primeira.Contexto.Should().Be(ImportarFontesUseCase.ContextoPadrao).And.Be("Outro");
        primeira.CriadoPorUsuarioId.Should().Be(ambiente.UsuarioId);
        ambiente.Repository.Adicionadas[1].Tipo.Should().Be(TipoFonte.Atom);
        ambiente.UnitOfWork.Commits.Should().Be(2);
    }

    [Fact]
    public async Task ExecutarAsync_DeveCadastrarTodasAsLinhas_QuandoSeparadorEhPontoEVirgulaComBom()
    {
        var ambiente = new Ambiente();
        var csv = "﻿nome;url;tipo;periodicidade\r\nPortal A;https://a.com/feed;Rss;30\r\n";

        var response = await ambiente.ImportarAsync(csv);

        response.Cadastradas.Should().Be(1);
        response.Rejeitadas.Should().BeEmpty();
        ambiente.Repository.Adicionadas.Should().ContainSingle()
            .Which.Nome.Should().Be("Portal A");
    }

    [Fact]
    public async Task ExecutarAsync_DeveCadastrarAsValidas_QuandoHaLinhasInvalidasNoMeio()
    {
        var ambiente = new Ambiente(urlsQueFalhamNaRede: ["https://rede-falha.com/8"]);
        var csv = string.Join("\n",
            Cabecalho,
            "Valida 1,https://a.com/1,Rss,30",              // 2
            "Tipo xml,https://a.com/2,Xml,30",               // 3
            "Tipo num,https://a.com/3,1,30",                 // 4
            "Per abc,https://a.com/4,Rss,abc",               // 5
            "Per zero,https://a.com/5,Rss,0",                // 6
            "Ftp,ftp://a.com/6,Rss,30",                      // 7
            ",https://a.com/7,Rss,30",                       // 8
            "Rede,https://rede-falha.com/8,Rss,30",          // 9
            "Curta,https://a.com/9,Rss",                     // 10
            "Valida 2,https://a.com/10,Atom,60",             // 11
            "");

        var response = await ambiente.ImportarAsync(csv);

        response.TotalLinhas.Should().Be(10);
        response.Cadastradas.Should().Be(2);
        ambiente.Repository.Adicionadas.Select(f => f.Nome)
            .Should().Equal("Valida 1", "Valida 2");
        ambiente.UnitOfWork.Commits.Should().Be(2);
        response.Rejeitadas.Select(r => r.Linha)
            .Should().Equal(3, 4, 5, 6, 7, 8, 9, 10);

        Motivo(response, 3).Should().Be("Tipo inválido: 'Xml'. Use Rss, Atom ou WebHtml.");
        Motivo(response, 4).Should().Be("Tipo inválido: '1'. Use Rss, Atom ou WebHtml.");
        Motivo(response, 5).Should().Be(
            "Periodicidade inválida: 'abc'. Informe um número inteiro de minutos.");
        Motivo(response, 6).Should().Contain("Periodicidade");
        Motivo(response, 7).Should().Be("A URL deve usar http ou https");
        Motivo(response, 8).Should().Contain("Nome");
        Motivo(response, 9).Should().Be("Feed inacessível");
        Motivo(response, 10).Should().Be("A linha tem 3 colunas, mas o cabeçalho tem 4.");
    }

    [Fact]
    public async Task ExecutarAsync_DeveRejeitarSegundaOcorrencia_QuandoUrlRepetidaNoArquivo()
    {
        var ambiente = new Ambiente();
        var csv = Cabecalho + "\nA,https://a.com/x,Rss,30\nB, https://a.com/x ,Rss,30\n";

        var response = await ambiente.ImportarAsync(csv);

        response.Cadastradas.Should().Be(1);
        var rejeitada = response.Rejeitadas.Should().ContainSingle().Subject;
        rejeitada.Linha.Should().Be(3);
        rejeitada.Motivo.Should().Be(
            "URL repetida no arquivo (já cadastrada pela linha 2).");
    }

    [Fact]
    public async Task ExecutarAsync_NaoDeveBloquearSegundaOcorrencia_QuandoPrimeiraFoiRejeitada()
    {
        var ambiente = new Ambiente(urlsQueFalhamNaRede: []);
        var csv = Cabecalho + "\nA,https://a.com/x,Rss,abc\nB,https://a.com/x,Rss,30\n";

        var response = await ambiente.ImportarAsync(csv);

        response.Cadastradas.Should().Be(1);
        response.Rejeitadas.Should().ContainSingle().Which.Linha.Should().Be(2);
        ambiente.Repository.Adicionadas.Should().ContainSingle()
            .Which.Nome.Should().Be("B");
    }

    [Fact]
    public async Task ExecutarAsync_DeveInterpretarAspasQuebrasDeLinhaECabecalhoEmMaiusculas()
    {
        var ambiente = new Ambiente();
        var csv = "NOME, URL ,Tipo,PERIODICIDADE\r\n"
            + "\r\n"
            + "\"Portal, \"\"Fiscal\"\"\",https://a.com/1,webhtml,15\r\n"
            + "   \r\n"
            + "Outro,https://a.com/2,Rss,x\r\n";

        var response = await ambiente.ImportarAsync(csv);

        response.TotalLinhas.Should().Be(2);
        response.Cadastradas.Should().Be(1);
        ambiente.Repository.Adicionadas.Should().ContainSingle().Which.Should().Match<Fonte>(
            f => f.Nome == "Portal, \"Fiscal\"" && f.Tipo == TipoFonte.WebHtml);
        response.Rejeitadas.Should().ContainSingle().Which.Linha.Should().Be(5);
    }

    [Fact]
    public async Task ExecutarAsync_DeveUsarColunaContexto_QuandoPresente()
    {
        var ambiente = new Ambiente();
        var csv = "nome,url,tipo,periodicidade,contexto,extra\n"
            + "A,https://a.com/1,Rss,30,Legislação,x\n"
            + "B,https://a.com/2,Rss,30,  ,y\n";

        var response = await ambiente.ImportarAsync(csv);

        response.Cadastradas.Should().Be(2);
        ambiente.Repository.Adicionadas.Select(f => f.Contexto)
            .Should().Equal("Legislação", "Outro");
    }

    [Fact]
    public async Task ExecutarAsync_DeveRejeitarArquivoInteiro_QuandoVazio()
    {
        var ambiente = new Ambiente();

        await ambiente.ImportarEsperandoErroAsync(new MemoryStream());
    }

    [Fact]
    public async Task ExecutarAsync_DeveRejeitarArquivoInteiro_QuandoSoTemCabecalho()
    {
        var ambiente = new Ambiente();

        var act = await ambiente.ImportarEsperandoErroAsync(Corpo(Cabecalho + "\n"));

        act.Message.Should().Be("O arquivo não tem linhas de dados.");
    }

    [Fact]
    public async Task ExecutarAsync_DeveRejeitarArquivoInteiro_QuandoColunaObrigatoriaAusente()
    {
        var ambiente = new Ambiente();

        var act = await ambiente.ImportarEsperandoErroAsync(
            Corpo("nome,url\nA,https://a.com\n"));

        act.Message.Should().Be("Colunas obrigatórias ausentes: tipo, periodicidade.");
    }

    [Fact]
    public async Task ExecutarAsync_DeveRejeitarArquivoInteiro_QuandoColunaRepetida()
    {
        var ambiente = new Ambiente();

        var act = await ambiente.ImportarEsperandoErroAsync(
            Corpo("nome,url,tipo,periodicidade,URL\nA,https://a.com,Rss,30,x\n"));

        act.Message.Should().Be("A coluna 'url' aparece mais de uma vez no cabeçalho.");
    }

    [Fact]
    public async Task ExecutarAsync_DeveRejeitarArquivoInteiro_QuandoMaisDeCemLinhas()
    {
        var ambiente = new Ambiente();
        var csv = new StringBuilder(Cabecalho).Append('\n');
        for (var i = 1; i <= 101; i++)
            csv.Append($"F{i},https://a.com/{i},Rss,30\n");

        var act = await ambiente.ImportarEsperandoErroAsync(Corpo(csv.ToString()));

        act.Message.Should().Be("O arquivo deve ter no máximo 100 linhas de dados.");
    }

    [Fact]
    public async Task ExecutarAsync_DeveAceitarCemLinhas()
    {
        var ambiente = new Ambiente();
        var csv = new StringBuilder(Cabecalho).Append('\n');
        for (var i = 1; i <= 100; i++)
            csv.Append($"F{i},https://a.com/{i},Rss,30\n");

        var response = await ambiente.ImportarAsync(csv.ToString());

        response.Cadastradas.Should().Be(100);
    }

    [Fact]
    public async Task ExecutarAsync_DeveRejeitarArquivoInteiro_QuandoMaiorQueUmMegabyte()
    {
        var ambiente = new Ambiente();
        var bytes = new byte[ImportarFontesUseCase.TamanhoMaximoBytes + 1];
        Array.Fill(bytes, (byte)'a');

        var act = await ambiente.ImportarEsperandoErroAsync(new MemoryStream(bytes));

        act.Message.Should().Be("O arquivo deve ter no máximo 1 MB.");
    }

    [Fact]
    public async Task ExecutarAsync_DeveRejeitarArquivoInteiro_QuandoBytesNaoSaoUtf8()
    {
        var ambiente = new Ambiente();
        var bytes = Encoding.UTF8.GetBytes(Cabecalho + "\nA,https://a.com,Rss,30\n")
            .Concat(new byte[] { 0xC3, 0x28 })
            .ToArray();

        var act = await ambiente.ImportarEsperandoErroAsync(new MemoryStream(bytes));

        act.Message.Should().Be("O arquivo deve estar em UTF-8.");
    }

    [Fact]
    public async Task ExecutarAsync_DeveRejeitarArquivoInteiro_QuandoAspasNaoFechadas()
    {
        var ambiente = new Ambiente();

        await ambiente.ImportarEsperandoErroAsync(
            Corpo(Cabecalho + "\n\"A,https://a.com,Rss,30\n"));
    }

    [Fact]
    public async Task ExecutarAsync_DeveLancarUnauthorized_QuandoUsuarioNaoAutenticado()
    {
        var ambiente = new Ambiente(autenticado: false);

        var act = () => ambiente.UseCase.ExecutarAsync(
            Corpo(Cabecalho + "\nA,https://a.com,Rss,30\n"),
            TestContext.Current.CancellationToken);

        await act.Should().ThrowAsync<UnauthorizedException>();
        ambiente.Repository.Adicionadas.Should().BeEmpty();
        ambiente.UnitOfWork.Commits.Should().Be(0);
    }

    [Fact]
    public async Task ExecutarAsync_NaoDeveCapturarFalhaInesperada()
    {
        var ambiente = new Ambiente(excecaoInesperada: new InvalidOperationException("banco"));

        var act = () => ambiente.UseCase.ExecutarAsync(
            Corpo(Cabecalho + "\nA,https://a.com,Rss,30\n"),
            TestContext.Current.CancellationToken);

        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    private static string Motivo(ImportarFontesResponse response, int linha) =>
        response.Rejeitadas.Single(r => r.Linha == linha).Motivo;

    private static MemoryStream Corpo(string csv) =>
        new(new UTF8Encoding(false).GetBytes(csv));

    private sealed class Ambiente
    {
        public Ambiente(
            string[]? urlsQueFalhamNaRede = null,
            bool autenticado = true,
            Exception? excecaoInesperada = null)
        {
            Repository = new FonteRepositoryStub();
            UnitOfWork = new UnitOfWorkStub();
            var criar = new CriarFonteUseCase(
                new CriarFonteValidator(),
                new CurrentUserStub(autenticado ? UsuarioId : null),
                new FonteUrlValidatorStub(urlsQueFalhamNaRede ?? [], excecaoInesperada),
                Repository,
                new AuditoriaServiceStub(),
                UnitOfWork);
            UseCase = new ImportarFontesUseCase(
                criar,
                new CurrentUserStub(autenticado ? UsuarioId : null));
        }

        public Guid UsuarioId { get; } = Guid.NewGuid();
        public FonteRepositoryStub Repository { get; }
        public UnitOfWorkStub UnitOfWork { get; }
        public ImportarFontesUseCase UseCase { get; }

        public Task<ImportarFontesResponse> ImportarAsync(string csv)
        {
            var bytes = new UTF8Encoding(false).GetBytes(csv);
            return UseCase.ExecutarAsync(
                new MemoryStream(bytes),
                TestContext.Current.CancellationToken);
        }

        public async Task<BusinessException> ImportarEsperandoErroAsync(Stream arquivo)
        {
            var act = () => UseCase.ExecutarAsync(
                arquivo,
                TestContext.Current.CancellationToken);

            var erro = (await act.Should().ThrowAsync<BusinessException>()).Which;
            Repository.Adicionadas.Should().BeEmpty();
            UnitOfWork.Commits.Should().Be(0);
            return erro;
        }
    }

    private sealed class CurrentUserStub(Guid? usuarioId) : ICurrentUser
    {
        public Guid? UsuarioId { get; } = usuarioId;
        public string? Email => "teste@invoisys.local";
        public bool EstaAutenticado => UsuarioId is not null;
    }

    private sealed class FonteUrlValidatorStub(
        string[] urlsQueFalham,
        Exception? excecaoInesperada) : IFonteUrlValidator
    {
        public Task ValidarAsync(
            string url,
            TipoFonte tipo,
            CancellationToken cancellationToken)
        {
            if (excecaoInesperada is not null)
                return Task.FromException(excecaoInesperada);

            return urlsQueFalham.Contains(url)
                ? Task.FromException(new BusinessException("Feed inacessível"))
                : Task.CompletedTask;
        }
    }

    private sealed class FonteRepositoryStub : IFonteRepository
    {
        public List<Fonte> Adicionadas { get; } = [];

        public Task AdicionarAsync(Fonte fonte, CancellationToken cancellationToken)
        {
            Adicionadas.Add(fonte);
            return Task.CompletedTask;
        }

        public Task<Fonte?> ObterPorIdAsync(Guid id, CancellationToken cancellationToken) =>
            Task.FromResult<Fonte?>(null);

        public Task<(IReadOnlyList<Fonte> Itens, int Total)> ListarAsync(
            int pagina,
            int tamanhoPagina,
            TipoFonte? tipo,
            StatusFonte? status,
            string? contexto,
            string? busca,
            CancellationToken cancellationToken) =>
            Task.FromResult<(IReadOnlyList<Fonte>, int)>(([], 0));
    }

    private sealed class UnitOfWorkStub : IUnitOfWork
    {
        public int Commits { get; private set; }

        public Task<int> CommitAsync(CancellationToken cancellationToken)
        {
            Commits++;
            return Task.FromResult(1);
        }
    }

    private sealed class AuditoriaServiceStub : IAuditoriaService
    {
        public Task RegistrarAsync(TipoEventoAuditoria tipo, string atividade, string objeto,
            Guid? objetoId, CancellationToken cancellationToken) => Task.CompletedTask;

        public Task RegistrarAsync(TipoEventoAuditoria tipo, string atividade, string objeto,
            Guid? objetoId, Guid? usuarioId, string responsavel, CancellationToken cancellationToken) =>
            Task.CompletedTask;
    }
}
