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

    [Fact]
    public void CriarFonte_DeveLancarDomainException_QuandoSeletorEmFontePdf()
    {
        var act = () => new Fonte(
            "Manual NF-e",
            "https://exemplo.com/manual.pdf",
            TipoFonte.Pdf,
            30,
            Guid.NewGuid(),
            "Manuais",
            "#conteudo");

        act.Should()
            .Throw<DomainException>()
            .WithMessage("O seletor de conteúdo só pode ser usado em fontes do tipo WebHtml.");
    }

    [Fact]
    public void Atualizar_DeveLancarDomainException_QuandoSeletorEmFontePdf()
    {
        var fonte = new Fonte(
            "Manual NF-e",
            "https://exemplo.com/manual.pdf",
            TipoFonte.Pdf,
            30,
            Guid.NewGuid(),
            "Manuais");

        var act = () => fonte.Atualizar(
            fonte.Nome, fonte.Url, TipoFonte.Pdf, 30, Guid.NewGuid(), fonte.Contexto, "#conteudo");

        act.Should()
            .Throw<DomainException>()
            .WithMessage("O seletor de conteúdo só pode ser usado em fontes do tipo WebHtml.");
        fonte.SeletorConteudo.Should().BeNull();
    }

    [Fact]
    public void PausarERetomarAgendamento_DeveManterStatusERegistrarAlteracao()
    {
        var fonte = new Fonte("Portal", "https://exemplo.com", TipoFonte.Rss, 30, Guid.NewGuid(), "Outro");
        var usuarioId = Guid.NewGuid();

        fonte.PausarAgendamento(usuarioId);

        fonte.AgendamentoPausado.Should().BeTrue();
        fonte.Status.Should().Be(StatusFonte.Ativa);
        fonte.AlteradoPorUsuarioId.Should().Be(usuarioId);

        fonte.RetomarAgendamento(usuarioId);
        fonte.AgendamentoPausado.Should().BeFalse();
    }

    [Fact]
    public void CalcularProximaColeta_DeveSomarPeriodicidadeAUltimaColetaOuACriacao()
    {
        var fonte = new Fonte("Portal", "https://exemplo.com", TipoFonte.Rss, 30, Guid.NewGuid(), "Outro");
        var ultima = new DateTimeOffset(2026, 11, 24, 10, 0, 0, TimeSpan.Zero);

        fonte.CalcularProximaColeta(ultima).Should().Be(ultima.AddMinutes(30));
        fonte.CalcularProximaColeta(null).Should().Be(fonte.CriadoEm.AddMinutes(30));
    }

    [Fact]
    public void CalcularProximaColeta_DeveSerNula_QuandoPausadaOuInativa()
    {
        var pausada = new Fonte("Portal", "https://exemplo.com", TipoFonte.Rss, 30, Guid.NewGuid(), "Outro");
        pausada.PausarAgendamento(Guid.NewGuid());
        var inativa = new Fonte("Portal", "https://exemplo.com", TipoFonte.Rss, 30, Guid.NewGuid(), "Outro");
        inativa.Desativar(Guid.NewGuid());

        pausada.CalcularProximaColeta(DateTimeOffset.UtcNow).Should().BeNull();
        inativa.CalcularProximaColeta(DateTimeOffset.UtcNow).Should().BeNull();
    }

    [Theory]
    [InlineData(1, false)]
    [InlineData(2, false)]
    [InlineData(3, true)]
    [InlineData(4, false)]
    public void DeveAlertarFalhas_SomenteAoAtingirOLimite(int falhas, bool esperado) =>
        Fonte.DeveAlertarFalhas(falhas).Should().Be(esperado);
}
