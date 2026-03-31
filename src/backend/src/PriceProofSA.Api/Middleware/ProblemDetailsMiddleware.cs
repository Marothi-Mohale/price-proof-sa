using System.Text.Json;
using PriceProofSA.Application.Common.Exceptions;

namespace PriceProofSA.Api.Middleware;

public sealed class ProblemDetailsMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ProblemDetailsMiddleware> _logger;

    public ProblemDetailsMiddleware(RequestDelegate next, ILogger<ProblemDetailsMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception exception)
        {
            await WriteProblemDetailsAsync(context, exception);
        }
    }

    private async Task WriteProblemDetailsAsync(HttpContext context, Exception exception)
    {
        _logger.LogError(exception, "Unhandled exception for {Method} {Path}", context.Request.Method, context.Request.Path);

        var (statusCode, title, extensions) = exception switch
        {
            InputValidationException validationException => (
                StatusCodes.Status400BadRequest,
                validationException.Message,
                new Dictionary<string, object?>
                {
                    ["errors"] = validationException.Errors
                }),
            AppNotFoundException notFoundException => (
                StatusCodes.Status404NotFound,
                notFoundException.Message,
                new Dictionary<string, object?>()),
            UnauthorizedAppException unauthorizedException => (
                StatusCodes.Status403Forbidden,
                unauthorizedException.Message,
                new Dictionary<string, object?>()),
            _ => (
                StatusCodes.Status500InternalServerError,
                "An unexpected error occurred.",
                new Dictionary<string, object?>())
        };

        context.Response.StatusCode = statusCode;
        context.Response.ContentType = "application/problem+json";

        var payload = new Dictionary<string, object?>
        {
            ["type"] = $"https://httpstatuses.com/{statusCode}",
            ["title"] = title,
            ["status"] = statusCode,
            ["traceId"] = context.TraceIdentifier
        };

        foreach (var extension in extensions)
        {
            payload[extension.Key] = extension.Value;
        }

        await context.Response.WriteAsync(JsonSerializer.Serialize(payload));
    }
}
