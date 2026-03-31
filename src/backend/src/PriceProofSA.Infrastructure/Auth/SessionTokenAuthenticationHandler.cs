using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PriceProofSA.Application.Auth;
using PriceProofSA.Infrastructure.Persistence;

namespace PriceProofSA.Infrastructure.Auth;

public sealed class SessionTokenAuthenticationHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    public const string SchemeName = "SessionToken";

    private readonly PriceProofDbContext _dbContext;

    public SessionTokenAuthenticationHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder,
        PriceProofDbContext dbContext)
        : base(options, logger, encoder)
    {
        _dbContext = dbContext;
    }

    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var token = TryReadToken(Request);
        if (string.IsNullOrWhiteSpace(token))
        {
            return AuthenticateResult.NoResult();
        }

        var hashedToken = SessionTokenHasher.Hash(token);
        var session = await _dbContext.UserSessions
            .Include(static item => item.User)
            .SingleOrDefaultAsync(item => item.TokenHash == hashedToken, Context.RequestAborted);

        if (session?.User is null || !session.IsActive(DateTimeOffset.UtcNow))
        {
            return AuthenticateResult.Fail("Session token is invalid or expired.");
        }

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, session.UserId.ToString()),
            new(ClaimTypes.Name, session.User.DisplayName),
            new(ClaimTypes.Email, session.User.Email),
            new(ClaimTypes.Role, session.User.Role.ToString())
        };

        var identity = new ClaimsIdentity(claims, SchemeName);
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, SchemeName);
        return AuthenticateResult.Success(ticket);
    }

    private static string? TryReadToken(HttpRequest request)
    {
        if (request.Headers.TryGetValue("X-Session-Token", out var directHeader))
        {
            return directHeader.ToString();
        }

        var authorizationHeader = request.Headers.Authorization.ToString();
        return authorizationHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase)
            ? authorizationHeader["Bearer ".Length..].Trim()
            : null;
    }
}
