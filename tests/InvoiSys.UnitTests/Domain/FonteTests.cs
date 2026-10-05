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
            usuarioId,
            "Legislação");

        fonte.Nome.Should().Be("Portal Fiscal");
        fonte.Status.Should().Be(StatusFonte.Ativa);
        fonte.PeriodicidadeMinutos.Should().Be(30);
        fonte.CriadoPorUsuarioId.Should().Be(usuarioId);
        fonte.Contexto.Should().Be("Legislação");
    }

    [Fact]
    public void CriarFonte_DeveFalhar_QuandoUrlForInvalida()
    {
        var act = () => new Fonte(
            "Portal Fiscal",
            "url-invalida",
            TipoFonte.Rss,
            30,
            Guid.NewGuid(),
            "Legislação");

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
            Guid.NewGuid(),
            "Legislação");
        var usuarioId = Guid.NewGuid();

        fonte.Desativar(usuarioId);

        fonte.Status.Should().Be(StatusFonte.Inativa);
        fonte.AlteradoPorUsuarioId.Should().Be(usuarioId);
        fonte.AlteradoEm.Should().NotBeNull();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void CriarFonte_DeveRejeitarContextoVazio(string contexto)
    {
        var act = () => new Fonte("Portal", "https://exemplo.com", TipoFonte.Rss, 30, Guid.NewGuid(), contexto);
        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void CriarEAtualizar_DeveValidarContextoAposTrim()
    {
        var fonte = new Fonte("Portal", "https://exemplo.com", TipoFonte.Rss, 30, Guid.NewGuid(), $"  {new string('A', 100)}  ");
        fonte.Contexto.Should().HaveLength(100);

        fonte.Atualizar("Portal", fonte.Url, fonte.Tipo, 30, Guid.NewGuid(), "  MOC  ");
        fonte.Contexto.Should().Be("MOC");

        var act = () => fonte.Atualizar("Portal", fonte.Url, fonte.Tipo, 30, Guid.NewGuid(), new string('A', 101));
        act.Should().Throw<DomainException>();
        fonte.Contexto.Should().Be("MOC");
    }
}
