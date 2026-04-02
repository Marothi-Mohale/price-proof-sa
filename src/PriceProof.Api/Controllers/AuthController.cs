using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using PriceProof.Application.Abstractions.Security;
using PriceProof.Application.Abstractions.Services;
using PriceProof.Application.Auth;
using PriceProof.Infrastructure.Options;

namespace PriceProof.Api.Controllers;

[ApiController]
[Route("auth")]
public sealed class AuthController : ControllerBase
{
    [AllowAnonymous]
    [HttpPost("sign-up")]
    [EnableRateLimiting("auth")]
    [ProducesResponseType(typeof(AuthSessionDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<AuthSessionDto>> SignUpAsync(
        [FromBody] SignUpRequest request,
        [FromServices] IAuthService authService,
        [FromServices] ISessionTokenService sessionTokenService,
        [FromServices] IOptions<SessionAuthOptions> sessionOptions,
        CancellationToken cancellationToken)
    {
        var result = await authService.SignUpAsync(request, cancellationToken);
        WriteSessionCookie(result, sessionTokenService, sessionOptions.Value);
        return Ok(result);
    }

    [AllowAnonymous]
    [HttpPost("sign-in")]
    [EnableRateLimiting("auth")]
    [ProducesResponseType(typeof(AuthSessionDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<AuthSessionDto>> SignInAsync(
        [FromBody] SignInRequest request,
        [FromServices] IAuthService authService,
        [FromServices] ISessionTokenService sessionTokenService,
        [FromServices] IOptions<SessionAuthOptions> sessionOptions,
        CancellationToken cancellationToken)
    {
        var result = await authService.SignInAsync(request, cancellationToken);
        WriteSessionCookie(result, sessionTokenService, sessionOptions.Value);
        return Ok(result);
    }

    [Authorize]
    [HttpPost("sign-out")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public IActionResult SignOut(
        [FromServices] IOptions<SessionAuthOptions> sessionOptions)
    {
        Response.Cookies.Delete(sessionOptions.Value.CookieName, BuildCookieOptions(sessionOptions.Value));
        return NoContent();
    }

    [Authorize]
    [HttpGet("me")]
    [ProducesResponseType(typeof(CurrentUserDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<CurrentUserDto>> GetCurrentUserAsync(
        [FromServices] IAuthService authService,
        CancellationToken cancellationToken)
    {
        var result = await authService.GetCurrentUserAsync(cancellationToken);
        return Ok(result);
    }

    private void WriteSessionCookie(
        AuthSessionDto session,
        ISessionTokenService sessionTokenService,
        SessionAuthOptions sessionOptions)
    {
        var expiresAtUtc = session.SignedInAtUtc.AddHours(sessionOptions.SessionLifetimeHours);
        var token = sessionTokenService.CreateToken(
            new SessionTokenPayload(session.UserId, session.Email, session.IsAdmin, session.SignedInAtUtc),
            expiresAtUtc);

        Response.Cookies.Append(sessionOptions.CookieName, token, BuildCookieOptions(sessionOptions, expiresAtUtc));
    }

    private CookieOptions BuildCookieOptions(SessionAuthOptions sessionOptions, DateTimeOffset? expiresAtUtc = null)
    {
        var environment = HttpContext.RequestServices.GetRequiredService<IHostEnvironment>();

        return new CookieOptions
        {
            HttpOnly = true,
            IsEssential = true,
            SameSite = SameSiteMode.Strict,
            Secure = environment.IsProduction(),
            Path = "/",
            Expires = expiresAtUtc
        };
    }
}
