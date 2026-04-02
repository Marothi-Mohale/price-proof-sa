using System.Diagnostics;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using PriceProof.Application.Abstractions.Diagnostics;
using PriceProof.Application.Common.Exceptions;

namespace PriceProof.Api.Middleware;

public sealed class GlobalExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionMiddleware> _logger;
    private readonly IWebHostEnvironment _environment;

    public GlobalExceptionMiddleware(
        RequestDelegate next,
        ILogger<GlobalExceptionMiddleware> logger,
        IWebHostEnvironment environment)
    {
        _next = next;
        _logger = logger;
        _environment = environment;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (ValidationException exception)
        {
            _logger.LogWarning(exception, "Validation failure for {Method} {Path}", context.Request.Method, context.Request.Path);
            var errors = exception.Errors
                .GroupBy(error => error.PropertyName)
                .ToDictionary(group => group.Key, group => group.Select(error => error.ErrorMessage).ToArray());

            await WriteProblemAsync(context, StatusCodes.Status400BadRequest, "Validation failed.", errors, _environment, exception);
        }
        catch (BadRequestException exception)
        {
            _logger.LogWarning(exception, "Bad request for {Method} {Path}", context.Request.Method, context.Request.Path);
            await WriteProblemAsync(context, StatusCodes.Status400BadRequest, exception.Message, null, _environment, exception);
        }
        catch (NotFoundException exception)
        {
            _logger.LogWarning(exception, "Resource not found for {Method} {Path}", context.Request.Method, context.Request.Path);
            await WriteProblemAsync(context, StatusCodes.Status404NotFound, exception.Message, null, _environment, exception);
        }
        catch (ConflictException exception)
        {
            _logger.LogWarning(exception, "Conflict for {Method} {Path}", context.Request.Method, context.Request.Path);
            await WriteProblemAsync(context, StatusCodes.Status409Conflict, exception.Message, null, _environment, exception);
        }
        catch (ForbiddenException exception)
        {
            _logger.LogWarning(exception, "Forbidden for {Method} {Path}", context.Request.Method, context.Request.Path);
            await WriteProblemAsync(context, StatusCodes.Status403Forbidden, exception.Message, null, _environment, exception);
        }
        catch (ServiceUnavailableException exception)
        {
            _logger.LogWarning(exception, "Service unavailable for {Method} {Path}", context.Request.Method, context.Request.Path);
            await WriteProblemAsync(context, StatusCodes.Status503ServiceUnavailable, exception.Message, null, _environment, exception);
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Unhandled exception for {Method} {Path}", context.Request.Method, context.Request.Path);
            await WriteProblemAsync(context, StatusCodes.Status500InternalServerError, "An unexpected server error occurred.", null, _environment, exception);
        }
    }

    private static async Task WriteProblemAsync(
        HttpContext context,
        int statusCode,
        string title,
        IDictionary<string, string[]>? errors,
        IWebHostEnvironment environment,
        Exception? exception = null)
    {
        context.Response.StatusCode = statusCode;
        context.Response.ContentType = "application/problem+json";

        var details = new ProblemDetails
        {
            Status = statusCode,
            Title = title,
            Type = $"https://httpstatuses.com/{statusCode}",
            Instance = context.Request.Path
        };

        var correlationId = CorrelationIdMiddleware.GetCorrelationId(context);
        details.Extensions["traceId"] = Activity.Current?.Id ?? context.TraceIdentifier;
        details.Extensions["correlationId"] = correlationId;
        context.Response.Headers[RequestContextConstants.CorrelationIdHeaderName] = correlationId;

        if (errors is not null)
        {
            details.Extensions["errors"] = errors;
        }

        if ((environment.IsDevelopment() || environment.IsEnvironment("Testing")) && exception is not null)
        {
            details.Detail = exception.Message;
            details.Extensions["exception"] = exception.GetType().Name;
        }

        await context.Response.WriteAsJsonAsync(details);
    }
}
