using System.Net;
using FluentAssertions;
using InvoiSys.Collectors.Http;

namespace InvoiSys.UnitTests.Collectors;

public sealed class EnderecoPublicoTests
{
    [Theory]
    [InlineData("127.0.0.1")]
    [InlineData("127.10.20.30")]
    [InlineData("10.0.0.5")]
    [InlineData("172.16.0.1")]
    [InlineData("172.31.255.255")]
    [InlineData("192.168.1.10")]
    [InlineData("169.254.169.254")]
    [InlineData("100.64.0.1")]
    [InlineData("0.0.0.0")]
    [InlineData("192.0.0.8")]
    [InlineData("198.18.0.1")]
    [InlineData("224.0.0.1")]
    [InlineData("255.255.255.255")]
    [InlineData("::1")]
    [InlineData("::")]
    [InlineData("fe80::1")]
    [InlineData("fc00::1")]
    [InlineData("fd12:3456::1")]
    [InlineData("ff02::1")]
    [InlineData("::ffff:127.0.0.1")]
    [InlineData("::ffff:10.0.0.1")]
    [InlineData("::ffff:169.254.169.254")]
    [InlineData("64:ff9b::7f00:1")]
    [InlineData("2002:7f00:1::1")]
    public void Permitido_DeveBarrarEnderecosInternos(string endereco)
    {
        EnderecoPublico.Permitido(IPAddress.Parse(endereco)).Should().BeFalse();
    }

    [Theory]
    [InlineData("8.8.8.8")]
    [InlineData("200.152.38.155")]
    [InlineData("172.15.255.255")]
    [InlineData("172.32.0.1")]
    [InlineData("100.63.255.255")]
    [InlineData("100.128.0.1")]
    [InlineData("2606:4700:4700::1111")]
    [InlineData("::ffff:8.8.8.8")]
    [InlineData("64:ff9b::808:808")]
    public void Permitido_DeveLiberarEnderecosPublicos(string endereco)
    {
        EnderecoPublico.Permitido(IPAddress.Parse(endereco)).Should().BeTrue();
    }

    [Theory]
    [InlineData("http://127.0.0.1/arquivo.pdf")]
    [InlineData("http://localhost/arquivo.pdf")]
    [InlineData("http://169.254.169.254/latest/meta-data/")]
    [InlineData("http://[::1]/arquivo.pdf")]
    [InlineData("http://10.1.2.3:8080/arquivo.pdf")]
    public async Task ManipuladorSeguro_DeveRecusarDestinosInternos(string url)
    {
        using var cliente = new HttpClient(ManipuladorSeguro.Criar())
        {
            Timeout = TimeSpan.FromSeconds(10)
        };

        var act = () => cliente.GetAsync(url, TestContext.Current.CancellationToken);

        // A mensagem prova que foi o bloqueio, e não uma conexão que falhou por outro motivo.
        var erro = await act.Should().ThrowAsync<HttpRequestException>();
        erro.Which.ToString().Should().Contain("não é um endereço público");
    }
}
