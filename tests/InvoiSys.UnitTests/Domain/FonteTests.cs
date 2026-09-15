using FluentAssertions;
using InvoiSys.Domain.Entities;
using InvoiSys.Domain.Enums;
using InvoiSys.Domain.Exceptions;

namespace InvoiSys.UnitTests.Domain;

public sealed class FonteTests
{
    [Fact]
    public void CriarFonte_DeveIniciarAtiva_QuandoDadosForemValidos()
    {
        var usuarioId = Guid.NewGuid();

        var fonte = new Fonte(
            "Portal Fiscal",
            "https://exemplo.com/rss",
            TipoFonte.Rss,
            30,
            usuarioId);

        fonte.Nome.Should().Be("Portal Fiscal");
        fonte.Status.Should().Be(StatusFonte.Ativa);
        fonte.PeriodicidadeMinutos.Should().Be(30);
        fonte.CriadoPorUsuarioId.Should().Be(usuarioId);
    }

    [Fact]
    public void CriarFonte_DeveFalhar_QuandoUrlForInvalida()
    {
        var act = () => new Fonte(
            "Portal Fiscal",
            "url-invalida",
            TipoFonte.Rss,
            30,
            Guid.NewGuid());

        act.Should()
            .Throw<DomainException>()
            .WithMessage("A URL da fonte é inválida.");
    }

    [Fact]
    public void Desativar_DeveRegistrarUsuarioDaAlteracao()
    {
        var fonte = new Fonte(
            "Portal Fiscal",
            "https://exemplo.com/rss",
            TipoFonte.Rss,
            30,
            Guid.NewGuid());
        var usuarioId = Guid.NewGuid();

        fonte.Desativar(usuarioId);

        fonte.Status.Should().Be(StatusFonte.Inativa);
        fonte.AlteradoPorUsuarioId.Should().Be(usuarioId);
        fonte.AlteradoEm.Should().NotBeNull();
    }
}
