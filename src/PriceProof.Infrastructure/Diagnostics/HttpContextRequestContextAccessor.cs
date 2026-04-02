using Microsoft.AspNetCore.Http;
using PriceProof.Application.Abstractions.Diagnostics;

namespace PriceProof.Infrastructure.Diagnostics;

internal sealed class HttpContextRequestContextAccessor : IRequestContextAccessor
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public HttpContextRequestContextAccessor(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public string CorrelationId
    {
        get
        {
            var httpContext = _httpContextAccessor.HttpContext;
            if (httpContext?.Items.TryGetValue(RequestContextConstants.CorrelationIdItemKey, out var value) == true &&
                value is string correlationId &&
                !string.IsNullOrWhiteSpace(correlationId))
            {
                return correlationId;
            }

            if (!string.IsNullOrWhiteSpace(httpContext?.TraceIdentifier))
            {
                return httpContext.TraceIdentifier;
            }

            return "system";
        }
    }
}
