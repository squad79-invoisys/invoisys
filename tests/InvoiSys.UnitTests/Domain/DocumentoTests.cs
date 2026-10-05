using FluentAssertions;
using InvoiSys.Domain.Entities;
using InvoiSys.Domain.Exceptions;

namespace InvoiSys.UnitTests.Domain;

public sealed class DocumentoTests
{
    [Fact]
    public void Criar_DeveNormalizarContexto()
    {
        var documento = Criar($"  {new string('A', 100)}  ");
        documento.Contexto.Should().HaveLength(100);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Criar_DeveRejeitarContextoVazio(string contexto)
    {
        var act = () => Criar(contexto);
        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void Criar_DeveRejeitarContextoAcimaDoLimite()
    {
        var act = () => Criar(new string('A', 101));
        act.Should().Throw<DomainException>();
    }

    private static Documento Criar(string contexto) =>
        new(Guid.NewGuid(), "Título", "https://exemplo.com/documento", "PDF", "Portal",
            contexto, null, null, null, "hash");
}
