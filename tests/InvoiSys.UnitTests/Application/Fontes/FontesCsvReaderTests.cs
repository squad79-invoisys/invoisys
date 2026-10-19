using FluentAssertions;
using InvoiSys.Application.Common.Exceptions;
using InvoiSys.Application.Fontes.ImportarFontes;

namespace InvoiSys.UnitTests.Application.Fontes;

public sealed class FontesCsvReaderTests
{
    [Fact]
    public void Ler_DeveSepararCabecalhoERegistros_ComVirgula()
    {
        var conteudo = FontesCsvReader.Ler("nome, url ,tipo\nA,B,C\nD,E,F\n");

        conteudo.Cabecalho.Should().Equal("nome", "url", "tipo");
        conteudo.Registros.Should().HaveCount(2);
        conteudo.Registros[0].Linha.Should().Be(2);
        conteudo.Registros[0].Campos.Should().Equal("A", "B", "C");
        conteudo.Registros[1].Linha.Should().Be(3);
    }

    [Fact]
    public void Ler_DeveDetectarPontoEVirgula_QuandoCabecalhoNaoTemVirgula()
    {
        var conteudo = FontesCsvReader.Ler("nome;url\r\nA;B\r\n");

        conteudo.Cabecalho.Should().Equal("nome", "url");
        conteudo.Registros.Should().ContainSingle()
            .Which.Campos.Should().Equal("A", "B");
    }

    [Fact]
    public void Ler_DeveUsarVirgula_QuandoCabecalhoTemAmbosSeparadores()
    {
        var conteudo = FontesCsvReader.Ler("nome,url;x\nA,B;C\n");

        conteudo.Cabecalho.Should().Equal("nome", "url;x");
    }

    [Fact]
    public void Ler_DeveInterpretarCamposEntreAspas()
    {
        var conteudo = FontesCsvReader.Ler("a,b\n\"x,y\",\"diz \"\"oi\"\"\"\n");

        conteudo.Registros.Should().ContainSingle()
            .Which.Campos.Should().Equal("x,y", "diz \"oi\"");
    }

    [Fact]
    public void Ler_DeveManterQuebraDeLinhaDentroDeAspas_ENumerarPelaLinhaDeInicio()
    {
        var conteudo = FontesCsvReader.Ler("a,b\n\"x\ny\",1\nz,2\n");

        conteudo.Registros.Should().HaveCount(2);
        conteudo.Registros[0].Linha.Should().Be(2);
        conteudo.Registros[0].Campos.Should().Equal("x\ny", "1");
        conteudo.Registros[1].Linha.Should().Be(4);
    }

    [Fact]
    public void Ler_DeveIgnorarLinhasEmBranco_EPreservarNumeracaoFisica()
    {
        var conteudo = FontesCsvReader.Ler("a,b\r\n\r\n   \r\nx,y\r\n\r\n");

        conteudo.Registros.Should().ContainSingle();
        conteudo.Registros[0].Linha.Should().Be(4);
    }

    [Fact]
    public void Ler_DeveLerUltimoRegistro_SemQuebraDeLinhaFinal()
    {
        var conteudo = FontesCsvReader.Ler("a,b\nx,y");

        conteudo.Registros.Should().ContainSingle()
            .Which.Campos.Should().Equal("x", "y");
    }

    [Fact]
    public void Ler_DeveManterCamposVazios()
    {
        var conteudo = FontesCsvReader.Ler("a,b,c\n,,\n");

        conteudo.Registros.Should().ContainSingle()
            .Which.Campos.Should().Equal("", "", "");
    }

    [Fact]
    public void Ler_DeveLancarBusinessException_QuandoAspasNaoFechadas()
    {
        var act = () => FontesCsvReader.Ler("a,b\n\"x,y\n");

        act.Should().Throw<BusinessException>();
    }

    [Fact]
    public void Ler_DeveLancarBusinessException_QuandoSoTemEspacos()
    {
        var act = () => FontesCsvReader.Ler("  \n \n");

        act.Should().Throw<BusinessException>();
    }
}
