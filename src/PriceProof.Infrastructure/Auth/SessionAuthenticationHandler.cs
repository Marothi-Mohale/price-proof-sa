using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PriceProof.Application.Abstractions.Persistence;
using PriceProof.Application.Abstractions.Security;
using PriceProof.Application.Auth;
using PriceProof.Infrastructure.Options;

namespace PriceProof.Infrastructure.Auth;

public sealed class SessionAuthenticationHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    public const string SchemeName = "PriceProofSession";

    private readonly IApplicationDbContext _dbContext;
    private readonly AccountSecurityOptions _accountSecurityOptions;
    private readonly ISessionTokenService _sessionTokenService;
    private readonly SessionAuthOptions _sessionAuthOptions;

    public SessionAuthenticationHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder,
        IApplicationDbContext dbContext,
        ISessionTokenService sessionTokenService,
        IOptions<SessionAuthOptions> sessionAuthOptions,
        IOptions<AccountSecurityOptions> accountSecurityOptions)
        : base(options, logger, encoder)
    {
        _dbContext = dbContext;
        _sessionTokenService = sessionTokenService;
        _sessionAuthOptions = sessionAuthOptions.Value;
        _accountSecurityOptions = accountSecurityOptions.Value;
    }

    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var token = TryReadAuthorizationHeader();

        if (string.IsNullOrWhiteSpace(token))
        {
            Request.Cookies.TryGetValue(_sessionAuthOptions.CookieName, out token);
        }

        if (string.IsNullOrWhiteSpace(token))
        {
            return AuthenticateResult.NoResult();
        }

        if (!_sessionTokenService.TryReadToken(token, out var payload) || payload is null)
        {
            return AuthenticateResult.Fail("The session token is invalid or expired.");
        }

        var user = await _dbContext.Users.FindAsync([payload.UserId], Context.RequestAborted);
        if (user is null || !user.IsActive)
        {
            return AuthenticateResult.Fail("The session is no longer valid.");
        }

        if (_accountSecurityOptions.RequireVerifiedEmailForSignIn && !user.IsEmailVerified)
        {
            return AuthenticateResult.Fail("The session requires a verified email address.");
        }

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Email, user.Email),
            new(ClaimTypes.Name, user.DisplayName)
        };

        if (user.IsAdmin)
        {
            claims.Add(new Claim(ClaimTypes.Role, "Admin"));
        }

        var identity = new ClaimsIdentity(claims, SchemeName);
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, SchemeName);
        return AuthenticateResult.Success(ticket);
    }

    private string? TryReadAuthorizationHeader()
    {
        var header = Request.Headers.Authorization.ToString();
        const string prefix = "Bearer ";

        return header.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)
            ? header[prefix.Length..].Trim()
            : null;
    }
}
