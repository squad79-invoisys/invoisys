using System.Diagnostics;
using FluentValidation;
using InvoiSys.Application.Common.Exceptions;
using InvoiSys.Application.Common.Responses;
using InvoiSys.Domain.Exceptions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace InvoiSys.Api.Filters;

public sealed class ApiExceptionFilter(
    ILogger<ApiExceptionFilter> logger) : IExceptionFilter
{
    public void OnException(ExceptionContext context)
    {
        if (context.Exception is ValidationException validationException)
        {
            HandleValidationException(context, validationException);
        }
        else if (context.Exception is InvoiSysException)
        {
            HandleProjectException(context);
        }
        else if (context.Exception is DomainException domainException)
        {
            HandleDomainException(context, domainException);
        }
        else
        {
            HandleUnknownError(context);
        }

        context.ExceptionHandled = true;
        RegistrarExcecaoTratada(context);
    }

    private void RegistrarExcecaoTratada(ExceptionContext context)
    {
        var httpContext = context.HttpContext;

        // Validação define só o Result; as demais definem o Response.StatusCode.
        var status = (context.Result as ObjectResult)?.StatusCode
            ?? httpContext.Response.StatusCode;

        var traceId = Activity.Current?.TraceId.ToString()
            ?? httpContext.TraceIdentifier;

        var rota = $"{httpContext.Request.Method} " +
            (context.ActionDescriptor.AttributeRouteInfo?.Template
                ?? httpContext.Request.Path.Value);

        if (status >= StatusCodes.Status500InternalServerError)
        {
            logger.LogError(
                context.Exception,
                "Exceção tratada em {Rota} respondeu {StatusCode} (TraceId {TraceId})",
                rota, status, traceId);
            return;
        }

        logger.LogWarning(
            "Exceção tratada em {Rota} respondeu {StatusCode} (TraceId {TraceId}): {TipoErro} - {Mensagem}",
            rota, status, traceId,
            context.Exception.GetType().Name,
            context.Exception.Message);
    }

    private static void HandleValidationException(
        ExceptionContext context,
        ValidationException exception)
    {
        context.Result = new BadRequestObjectResult(
            ApiResponse<object?>.Erro(string.Join(
                " ",
                exception.Errors
                    .Select(error => error.ErrorMessage)
                    .Distinct())));
    }

    private static void HandleProjectException(ExceptionContext context)
    {
        var exception = (InvoiSysException)context.Exception;
        var errorResponse = ApiResponse<object?>.Erro(
            string.Join(" ", exception.GetErrors()));

        context.HttpContext.Response.StatusCode = exception.StatusCode;
        context.Result = new ObjectResult(errorResponse);
    }

    private static void HandleDomainException(
        ExceptionContext context,
        DomainException exception)
    {
        context.HttpContext.Response.StatusCode =
            StatusCodes.Status400BadRequest;

        context.Result = new ObjectResult(
            ApiResponse<object?>.Erro(exception.Message));
    }

    private static void HandleUnknownError(ExceptionContext context)
    {
        context.HttpContext.Response.StatusCode =
            StatusCodes.Status500InternalServerError;

        context.Result = new ObjectResult(
            ApiResponse<object?>.Erro("Erro desconhecido."));
    }
}