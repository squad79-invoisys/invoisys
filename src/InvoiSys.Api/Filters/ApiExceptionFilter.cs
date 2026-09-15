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

    private void HandleUnknownError(ExceptionContext context)
    {
        logger.LogError(
            context.Exception,
            "Erro não tratado durante a requisição.");

        context.HttpContext.Response.StatusCode =
            StatusCodes.Status500InternalServerError;

        context.Result = new ObjectResult(
            ApiResponse<object?>.Erro("Erro desconhecido."));
    }
}
