using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using PriceProof.Application.Abstractions.Security;
using PriceProof.Application.Auth;

namespace PriceProof.Infrastructure.Auth;

internal sealed class AccountWorkflowUrlBuilder : IAccountWorkflowUrlBuilder
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly AccountSecurityOptions _options;

    public AccountWorkflowUrlBuilder(
        IHttpContextAccessor httpContextAccessor,
        IOptions<AccountSecurityOptions> options)
    {
        _httpContextAccessor = httpContextAccessor;
        _options = options.Value;
    }

    public string BuildEmailVerificationUrl(string email, string token)
    {
        return BuildUrl("/auth/verify", email, token);
    }

    public string BuildPasswordResetUrl(string email, string token)
    {
        return BuildUrl("/auth/reset-password", email, token);
    }

    private string BuildUrl(string path, string email, string token)
    {
        var baseUrl = ResolveBaseUrl();
        return $"{baseUrl}{path}?email={Uri.EscapeDataString(email)}&token={Uri.EscapeDataString(token)}";
    }

    private string ResolveBaseUrl()
    {
        if (!string.IsNullOrWhiteSpace(_options.PublicAppUrl))
        {
            return _options.PublicAppUrl!.TrimEnd('/');
        }

        var request = _httpContextAccessor.HttpContext?.Request;
        if (request is null)
        {
            return "http://localhost:3000";
        }

        if (request.Headers.TryGetValue("Origin", out var origin) && Uri.TryCreate(origin.ToString(), UriKind.Absolute, out var originUri))
        {
            return originUri.GetLeftPart(UriPartial.Authority).TrimEnd('/');
        }

        return $"{request.Scheme}://{request.Host.Value}".TrimEnd('/');
    }
}
