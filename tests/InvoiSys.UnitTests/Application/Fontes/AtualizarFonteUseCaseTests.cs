using FluentAssertions;
using InvoiSys.Application.Common.Abstractions;
using InvoiSys.Application.Common.Exceptions;
using InvoiSys.Application.Common.Fontes;
using InvoiSys.Application.Fontes.AtualizarFonte;
using InvoiSys.Domain.Entities;
using InvoiSys.Domain.Enums;
using InvoiSys.Domain.Repositories;

namespace InvoiSys.UnitTests.Application.Fontes;

public sealed class AtualizarFonteUseCaseTests
{
    [Theory]
    [InlineData(true, false)]
    [InlineData(false, true)]
    [InlineData(true, true)]
    public async Task Execute_DeveValidarRemotamente_QuandoOrigemMudar(
        bool alterarUrl,
        bool alterarTipo)
    {
        var fonte = CriarFonte();
        var validator = new FonteUrlValidatorStub();
        var unitOfWork = new UnitOfWorkStub();
        var useCase = CriarUseCase(fonte, validator, unitOfWork);
        var request = new AtualizarFonteRequest(
            "Portal atualizado",
            alterarUrl ? "https://novo.exemplo.com/feed" : fonte.Url,
            alterarTipo ? TipoFonte.Atom : fonte.Tipo,
            60,
            "Legislação");

        await useCase.Execute(
            fonte.Id,
            request,
            TestContext.Current.CancellationToken);

        validator.Chamadas.Should().Be(1);
        validator.UltimaUrl.Should().Be(request.Url);
        validator.UltimoTipo.Should().Be(request.Tipo);
        unitOfWork.Commits.Should().Be(1);
    }

    [Fact]
    public async Task Execute_NaoDeveValidarRemotamente_QuandoSomenteNomeEPeriodicidadeMudarem()
    {
        var fonte = CriarFonte();
        var validator = new FonteUrlValidatorStub();
        var unitOfWork = new UnitOfWorkStub();
        var useCase = CriarUseCase(fonte, validator, unitOfWork);
        var request = new AtualizarFonteRequest(
            "Outro nome",
            fonte.Url,
            fonte.Tipo,
            120,
            "Legislação");

        await useCase.Execute(
            fonte.Id,
            request,
            TestContext.Current.CancellationToken);

        validator.Chamadas.Should().Be(0);
        fonte.Nome.Should().Be("Outro nome");
        fonte.PeriodicidadeMinutos.Should().Be(120);
        unitOfWork.Commits.Should().Be(1);
    }

    [Fact]
    public async Task Execute_DevePersistirContextoSemRevalidarOrigem()
    {
        var fonte = CriarFonte();
        var validator = new FonteUrlValidatorStub();
        var unitOfWork = new UnitOfWorkStub();
        var useCase = CriarUseCase(fonte, validator, unitOfWork);
        var request = new AtualizarFonteRequest(fonte.Nome, fonte.Url, fonte.Tipo, fonte.PeriodicidadeMinutos, "  MOC  ");

        var response = await useCase.Execute(fonte.Id, request, TestContext.Current.CancellationToken);

        fonte.Contexto.Should().Be("MOC");
        response.Contexto.Should().Be("MOC");
        validator.Chamadas.Should().Be(0);
        unitOfWork.Commits.Should().Be(1);
    }

    [Fact]
    public async Task Execute_NaoDeveValidarRemotamente_QuandoUrlDiferirSomentePorEspacosExternos()
    {
        var fonte = CriarFonte();
        var validator = new FonteUrlValidatorStub();
        var useCase = CriarUseCase(fonte, validator, new UnitOfWorkStub());
        var request = new AtualizarFonteRequest(
            fonte.Nome,
            $"  {fonte.Url}  ",
            fonte.Tipo,
            fonte.PeriodicidadeMinutos,
            "Legislação");

        await useCase.Execute(
            fonte.Id,
            request,
            TestContext.Current.CancellationToken);

        validator.Chamadas.Should().Be(0);
    }

    [Fact]
    public async Task Execute_NaoDeveModificarNemPersistir_QuandoValidacaoNecessariaFalhar()
    {
        var fonte = CriarFonte();
        var nomeOriginal = fonte.Nome;
        var urlOriginal = fonte.Url;
        var tipoOriginal = fonte.Tipo;
        var periodicidadeOriginal = fonte.PeriodicidadeMinutos;
        var validator = new FonteUrlValidatorStub(
            new BusinessException("URL inválida"));
        var unitOfWork = new UnitOfWorkStub();
        var useCase = CriarUseCase(fonte, validator, unitOfWork);
        var request = new AtualizarFonteRequest(
            "Nome modificado",
            "https://novo.exemplo.com/feed",
            TipoFonte.Atom,
            90,
            "Legislação");

        var act = () => useCase.Execute(
            fonte.Id,
            request,
            TestContext.Current.CancellationToken);

        await act.Should().ThrowAsync<BusinessException>();
        fonte.Nome.Should().Be(nomeOriginal);
        fonte.Url.Should().Be(urlOriginal);
        fonte.Tipo.Should().Be(tipoOriginal);
        fonte.PeriodicidadeMinutos.Should().Be(periodicidadeOriginal);
        fonte.AlteradoEm.Should().BeNull();
        validator.Chamadas.Should().Be(1);
        unitOfWork.Commits.Should().Be(0);
    }

    [Fact]
    public async Task Execute_DevePreservarNotFoundSemValidacaoRemota()
    {
        var validator = new FonteUrlValidatorStub();
        var useCase = CriarUseCase(null, validator, new UnitOfWorkStub());
        var request = new AtualizarFonteRequest(
            "Portal",
            "https://exemplo.com/feed",
            TipoFonte.Rss,
            30,
            "Legislação");

        var act = () => useCase.Execute(
            Guid.NewGuid(),
            request,
            TestContext.Current.CancellationToken);

        await act.Should()
            .ThrowAsync<NotFoundException>()
            .WithMessage("Fonte não encontrada.");
        validator.Chamadas.Should().Be(0);
    }

    private static Fonte CriarFonte() =>
        new(
            "Portal Fiscal",
            "https://exemplo.com/feed",
            TipoFonte.Rss,
            30,
            Guid.NewGuid(),
            "Legislação");

    private static AtualizarFonteUseCase CriarUseCase(
        Fonte? fonte,
        IFonteUrlValidator validator,
        IUnitOfWork unitOfWork) =>
        new(
            new AtualizarFonteValidator(),
            new CurrentUserStub(Guid.NewGuid()),
            validator,
            new FonteRepositoryStub(fonte),
            unitOfWork);

    private sealed class CurrentUserStub(Guid usuarioId) : ICurrentUser
    {
        public Guid? UsuarioId { get; } = usuarioId;
        public string? Email => "teste@invoisys.local";
        public bool EstaAutenticado => true;
    }

    private sealed class FonteUrlValidatorStub(
        Exception? exception = null) : IFonteUrlValidator
    {
        public int Chamadas { get; private set; }
        public string? UltimaUrl { get; private set; }
        public TipoFonte? UltimoTipo { get; private set; }

        public Task ValidarAsync(
            string url,
            TipoFonte tipo,
            CancellationToken cancellationToken)
        {
            Chamadas++;
            UltimaUrl = url;
            UltimoTipo = tipo;

            return exception is null
                ? Task.CompletedTask
                : Task.FromException(exception);
        }
    }

    private sealed class FonteRepositoryStub(Fonte? fonte) : IFonteRepository
    {
        public Task AdicionarAsync(
            Fonte fonte,
            CancellationToken cancellationToken) =>
            Task.CompletedTask;

        public Task<Fonte?> ObterPorIdAsync(
            Guid id,
            CancellationToken cancellationToken) =>
            Task.FromResult(fonte?.Id == id ? fonte : null);

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
}
