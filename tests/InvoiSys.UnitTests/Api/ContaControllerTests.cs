using System.Security.Claims;
using FluentAssertions;
using InvoiSys.Api.Controllers;
using InvoiSys.Application.Common.Responses;
using InvoiSys.Application.Usuarios.AlterarPropriaSenha;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace InvoiSys.UnitTests.Api;

public sealed class ContaControllerTests
{
    [Fact]
    public async Task AlterarSenhaAsync_DeveEnviarSessaoAtualDoToken()
    {
        var sessaoId = Guid.NewGuid();
        var useCase = new AlterarPropriaSenhaUseCaseStub();
        var controller = CriarController(useCase, new Claim("sid", sessaoId.ToString()));
        var request = new AlterarPropriaSenhaRequest("Senha@Atual1", "Senha@Nova22");

        var resultado = await controller.AlterarSenhaAsync(
            request,
            TestContext.Current.CancellationToken);

        var ok = resultado.Should().BeOfType<OkObjectResult>().Subject;
        ok.Value.Should().BeOfType<ApiResponse<object>>()
            .Which.Mensagem.Should().Be("Senha alterada com sucesso.");
        useCase.Request.Should().BeSameAs(request);
        useCase.SessaoAtualId.Should().Be(sessaoId);
    }

    [Fact]
    public async Task AlterarSenhaAsync_DeveEnviarSessaoNula_QuandoTokenNaoTiverSid()
    {
        var useCase = new AlterarPropriaSenhaUseCaseStub();
        var controller = CriarController(useCase);

        await controller.AlterarSenhaAsync(
            new AlterarPropriaSenhaRequest("Senha@Atual1", "Senha@Nova22"),
            TestContext.Current.CancellationToken);

        useCase.SessaoAtualId.Should().BeNull();
    }

    private static ContaController CriarController(
        IAlterarPropriaSenhaUseCase useCase,
        params Claim[] claims) =>
        new(useCase)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = new ClaimsPrincipal(new ClaimsIdentity(claims, "Teste"))
                }
            }
        };

    private sealed class AlterarPropriaSenhaUseCaseStub : IAlterarPropriaSenhaUseCase
    {
        public AlterarPropriaSenhaRequest? Request { get; private set; }
        public Guid? SessaoAtualId { get; private set; }

        public Task Execute(
            AlterarPropriaSenhaRequest request,
            Guid? sessaoAtualId,
            CancellationToken cancellationToken)
        {
            Request = request;
            SessaoAtualId = sessaoAtualId;
            return Task.CompletedTask;
        }
    }
}
