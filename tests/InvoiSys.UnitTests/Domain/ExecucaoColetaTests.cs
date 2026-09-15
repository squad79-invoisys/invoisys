using FluentAssertions;
using InvoiSys.Domain.Entities;
using InvoiSys.Domain.Enums;
using InvoiSys.Domain.Exceptions;

namespace InvoiSys.UnitTests.Domain;

public sealed class ExecucaoColetaTests
{
    [Fact]
    public void IniciarManual_DeveRegistrarUsuarioSolicitante()
    {
        var usuarioId = Guid.NewGuid();

        var execucao = ExecucaoColeta.IniciarManual(
            Guid.NewGuid(),
            usuarioId);

        execucao.TipoExecucao.Should().Be(TipoExecucao.Manual);
        execucao.SolicitadaPorUsuarioId.Should().Be(usuarioId);
        execucao.Status.Should().Be(StatusExecucaoColeta.EmAndamento);
    }

    [Fact]
    public void IniciarAutomatica_NaoDevePossuirUsuarioSolicitante()
    {
        var execucao = ExecucaoColeta.IniciarAutomatica(Guid.NewGuid());

        execucao.TipoExecucao.Should().Be(TipoExecucao.Automatica);
        execucao.SolicitadaPorUsuarioId.Should().BeNull();
    }

    [Fact]
    public void Concluir_DeveRegistrarQuantidadeEFinalizacao()
    {
        var execucao = ExecucaoColeta.IniciarAutomatica(Guid.NewGuid());

        execucao.Concluir(4);

        execucao.Status.Should().Be(StatusExecucaoColeta.Concluida);
        execucao.QuantidadeDocumentos.Should().Be(4);
        execucao.Fim.Should().NotBeNull();
        execucao.MensagemErro.Should().BeNull();
    }

    [Fact]
    public void Concluir_DeveFalhar_QuandoQuantidadeForNegativa()
    {
        var execucao = ExecucaoColeta.IniciarAutomatica(Guid.NewGuid());

        var act = () => execucao.Concluir(-1);

        act.Should().Throw<DomainException>();
    }
}
