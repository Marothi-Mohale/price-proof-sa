using System.Diagnostics;
using PriceProof.Application.Abstractions.Diagnostics;
using Serilog.Context;

namespace PriceProof.Api.Middleware;

public sealed class CorrelationIdMiddleware
{
    private const int MaxCorrelationIdLength = 64;

    private readonly RequestDelegate _next;

    public CorrelationIdMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var correlationId = ResolveCorrelationId(context.Request.Headers[RequestContextConstants.CorrelationIdHeaderName]);

        context.TraceIdentifier = correlationId;
        context.Items[RequestContextConstants.CorrelationIdItemKey] = correlationId;
        context.Response.Headers[RequestContextConstants.CorrelationIdHeaderName] = correlationId;

        Activity.Current?.SetTag("correlation.id", correlationId);

        using (LogContext.PushProperty("CorrelationId", correlationId))
        {
            await _next(context);
        }
    }

    public static string GetCorrelationId(HttpContext context)
    {
        if (context.Items.TryGetValue(RequestContextConstants.CorrelationIdItemKey, out var value) &&
            value is string correlationId &&
            !string.IsNullOrWhiteSpace(correlationId))
        {
            return correlationId;
        }

        return string.IsNullOrWhiteSpace(context.TraceIdentifier)
            ? Guid.NewGuid().ToString("N")
            : context.TraceIdentifier;
    }

    private static string ResolveCorrelationId(string? candidate)
    {
        if (string.IsNullOrWhiteSpace(candidate))
        {
            return Guid.NewGuid().ToString("N");
        }

        var sanitized = new string(candidate
            .Trim()
            .Where(character => !char.IsControl(character) && (char.IsLetterOrDigit(character) || character is '-' or '_' or '.'))
            .Take(MaxCorrelationIdLength)
            .ToArray());

        return string.IsNullOrWhiteSpace(sanitized)
            ? Guid.NewGuid().ToString("N")
            : sanitized;
    }
}
