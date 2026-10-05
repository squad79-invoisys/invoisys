using FluentAssertions;
using InvoiSys.Api.Filters;
using InvoiSys.Application.Common.Exceptions;
using InvoiSys.Application.Common.Responses;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Logging.Abstractions;

namespace InvoiSys.UnitTests.Api;

public sealed class ApiExceptionFilterTests
{
    [Fact]
    public void OnException_DeveRetornar400ComApiResponse_ParaBusinessException()
    {
        var context = CriarContext(
            new BusinessException("A URL não pôde ser validada"));
        var filter = new ApiExceptionFilter(
            NullLogger<ApiExceptionFilter>.Instance);

        filter.OnException(context);

        context.ExceptionHandled.Should().BeTrue();
        context.HttpContext.Response.StatusCode.Should().Be(
            StatusCodes.Status400BadRequest);
        var result = context.Result.Should().BeOfType<ObjectResult>().Subject;
        var response = result.Value.Should()
            .BeOfType<ApiResponse<object?>>()
            .Subject;
        response.Sucesso.Should().BeFalse();
        response.Mensagem.Should().Be("A URL não pôde ser validada");
        response.Dados.Should().BeNull();
    }

    [Fact]
    public void OnException_DevePreservar500_ParaErroInesperado()
    {
        var context = CriarContext(new InvalidOperationException("erro interno"));
        var filter = new ApiExceptionFilter(
            NullLogger<ApiExceptionFilter>.Instance);

        filter.OnException(context);

        context.HttpContext.Response.StatusCode.Should().Be(
            StatusCodes.Status500InternalServerError);
        var result = context.Result.Should().BeOfType<ObjectResult>().Subject;
        var response = result.Value.Should()
            .BeOfType<ApiResponse<object?>>()
            .Subject;
        response.Mensagem.Should().Be("Erro desconhecido.");
    }

    private static ExceptionContext CriarContext(Exception exception)
    {
        var actionContext = new ActionContext(
            new DefaultHttpContext(),
            new RouteData(),
            new ActionDescriptor());

        return new ExceptionContext(actionContext, [])
        {
            Exception = exception
        };
    }
}
