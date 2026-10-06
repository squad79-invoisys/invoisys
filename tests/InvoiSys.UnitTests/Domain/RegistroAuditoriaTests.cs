using FluentAssertions;
using InvoiSys.Domain.Entities;
using InvoiSys.Domain.Enums;
using InvoiSys.Domain.Exceptions;

namespace InvoiSys.UnitTests.Domain;

public sealed class RegistroAuditoriaTests
{
    [Fact]
    public void Construtor_DeveNormalizarCampos_QuandoDadosValidos()
    {
        var usuarioId = Guid.NewGuid();

        var registro = new RegistroAuditoria(
            TipoEventoAuditoria.Fonte,
            "  Fonte alterada ",
            " Portal Fiscal ",
            Guid.Empty,
            usuarioId,
            " admin@invoisys.local ",
            "  ");

        registro.Atividade.Should().Be("Fonte alterada");
        registro.Objeto.Should().Be("Portal Fiscal");
        registro.ObjetoId.Should().BeNull();
        registro.UsuarioId.Should().Be(usuarioId);
        registro.Responsavel.Should().Be("admin@invoisys.local");
        registro.EnderecoIp.Should().BeNull();
        registro.OcorridoEm.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void Construtor_DeveTruncarObjeto_QuandoExcederTamanhoMaximo()
    {
        var objeto = new string('a', RegistroAuditoria.TamanhoMaximoObjeto + 50);

        var registro = new RegistroAuditoria(
            TipoEventoAuditoria.Autenticacao, "Tentativa de login recusada", objeto, null, null, "x", null);

        registro.Objeto.Should().HaveLength(RegistroAuditoria.TamanhoMaximoObjeto);
    }

    [Theory]
    [InlineData("", "objeto", "responsavel")]
    [InlineData("atividade", " ", "responsavel")]
    [InlineData("atividade", "objeto", "")]
    public void Construtor_DeveLancarDomainException_QuandoCampoObrigatorioVazio(
        string atividade, string objeto, string responsavel)
    {
        var act = () => new RegistroAuditoria(
            TipoEventoAuditoria.Coleta, atividade, objeto, null, null, responsavel, null);

        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void Construtor_DeveLancarDomainException_QuandoTipoInvalido()
    {
        var act = () => new RegistroAuditoria(
            (TipoEventoAuditoria)99, "atividade", "objeto", null, null, "responsavel", null);

        act.Should().Throw<DomainException>();
    }
}
