using FluentAssertions;
using InvoiSys.Application.Common.Abstractions;
using InvoiSys.Application.Common.Exceptions;
using InvoiSys.Application.Common.Fontes;
using InvoiSys.Application.Fontes.CriarFonte;
using InvoiSys.Domain.Entities;
using InvoiSys.Domain.Enums;
using InvoiSys.Domain.Repositories;

namespace InvoiSys.UnitTests.Application.Fontes;

public sealed class CriarFonteUseCaseTests
{
    [Fact]
    public async Task ExecutarAsync_DeveCadastrarFonteComUsuarioAtual()
    {
        var usuarioId = Guid.NewGuid();
        var currentUser = new CurrentUserStub(usuarioId);
        var repository = new FonteRepositoryStub();
        var unitOfWork = new UnitOfWorkStub();
        var fonteUrlValidator = new FonteUrlValidatorStub();
        var useCase = new CriarFonteUseCase(
            new CriarFonteValidator(),
            currentUser,
            fonteUrlValidator,
            repository,
            unitOfWork);
        var request = new CriarFonteRequest(
            "Portal Fiscal",
            "https://exemplo.com/feed.xml",
            TipoFonte.Rss,
            30);

        var response = await useCase.ExecutarAsync(
            request,
            CancellationToken.None);

        repository.Adicionada.Should().NotBeNull();
        repository.Adicionada!.CriadoPorUsuarioId.Should().Be(usuarioId);
        response.Id.Should().Be(repository.Adicionada.Id);
        unitOfWork.Commits.Should().Be(1);
        fonteUrlValidator.Chamadas.Should().Be(1);
    }

    [Fact]
    public async Task ExecutarAsync_NaoDevePersistir_QuandoValidacaoRemotaFalhar()
    {
        var repository = new FonteRepositoryStub();
        var unitOfWork = new UnitOfWorkStub();
        var fonteUrlValidator = new FonteUrlValidatorStub(
            new BusinessException("URL inválida"));
        var useCase = new CriarFonteUseCase(
            new CriarFonteValidator(),
            new CurrentUserStub(Guid.NewGuid()),
            fonteUrlValidator,
            repository,
            unitOfWork);
        var request = new CriarFonteRequest(
            "Portal Fiscal",
            "https://exemplo.com/feed.xml",
            TipoFonte.Rss,
            30);

        var act = () => useCase.ExecutarAsync(
            request,
            TestContext.Current.CancellationToken);

        await act.Should().ThrowAsync<BusinessException>();
        repository.Adicionada.Should().BeNull();
        unitOfWork.Commits.Should().Be(0);
    }

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

        public Task ValidarAsync(
            string url,
            TipoFonte tipo,
            CancellationToken cancellationToken)
        {
            Chamadas++;

            return exception is null
                ? Task.CompletedTask
                : Task.FromException(exception);
        }
    }

    private sealed class FonteRepositoryStub : IFonteRepository
    {
        public Fonte? Adicionada { get; private set; }

        public Task AdicionarAsync(Fonte fonte, CancellationToken cancellationToken)
        {
            Adicionada = fonte;
            return Task.CompletedTask;
        }

        public Task<Fonte?> ObterPorIdAsync(Guid id, CancellationToken cancellationToken) =>
            Task.FromResult<Fonte?>(null);

        public Task<(IReadOnlyList<Fonte> Itens, int Total)> ListarAsync(
            int pagina,
            int tamanhoPagina,
            TipoFonte? tipo,
            StatusFonte? status,
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
