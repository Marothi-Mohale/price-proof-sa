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
        WriteSessionCookieIfPresent(result, sessionTokenService, sessionOptions.Value);
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
        WriteSessionCookieIfPresent(result, sessionTokenService, sessionOptions.Value);
        return Ok(result);
    }

    [AllowAnonymous]
    [HttpPost("email-verification/request")]
    [EnableRateLimiting("auth")]
    [ProducesResponseType(typeof(AuthActionResultDto), StatusCodes.Status202Accepted)]
    public async Task<ActionResult<AuthActionResultDto>> RequestEmailVerificationAsync(
        [FromBody] RequestEmailVerificationRequest request,
        [FromServices] IAuthService authService,
        CancellationToken cancellationToken)
    {
        var result = await authService.RequestEmailVerificationAsync(request, cancellationToken);
        return Accepted(result);
    }

    [AllowAnonymous]
    [HttpPost("email-verification/confirm")]
    [EnableRateLimiting("auth")]
    [ProducesResponseType(typeof(AuthSessionDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<AuthSessionDto>> ConfirmEmailVerificationAsync(
        [FromBody] ConfirmEmailVerificationRequest request,
        [FromServices] IAuthService authService,
        [FromServices] ISessionTokenService sessionTokenService,
        [FromServices] IOptions<SessionAuthOptions> sessionOptions,
        CancellationToken cancellationToken)
    {
        var result = await authService.ConfirmEmailVerificationAsync(request, cancellationToken);
        WriteSessionCookieIfPresent(result, sessionTokenService, sessionOptions.Value);
        return Ok(result);
    }

    [AllowAnonymous]
    [HttpPost("password-reset/request")]
    [EnableRateLimiting("auth")]
    [ProducesResponseType(typeof(AuthActionResultDto), StatusCodes.Status202Accepted)]
    public async Task<ActionResult<AuthActionResultDto>> RequestPasswordResetAsync(
        [FromBody] RequestPasswordResetRequest request,
        [FromServices] IAuthService authService,
        CancellationToken cancellationToken)
    {
        var result = await authService.RequestPasswordResetAsync(request, cancellationToken);
        return Accepted(result);
    }

    [AllowAnonymous]
    [HttpPost("password-reset/confirm")]
    [EnableRateLimiting("auth")]
    [ProducesResponseType(typeof(AuthSessionDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<AuthSessionDto>> ConfirmPasswordResetAsync(
        [FromBody] ConfirmPasswordResetRequest request,
        [FromServices] IAuthService authService,
        [FromServices] ISessionTokenService sessionTokenService,
        [FromServices] IOptions<SessionAuthOptions> sessionOptions,
        CancellationToken cancellationToken)
    {
        var result = await authService.ConfirmPasswordResetAsync(request, cancellationToken);
        WriteSessionCookieIfPresent(result, sessionTokenService, sessionOptions.Value);
        return Ok(result);
    }

    [AllowAnonymous]
    [HttpPost("account-recovery")]
    [EnableRateLimiting("auth")]
    [ProducesResponseType(typeof(AuthActionResultDto), StatusCodes.Status202Accepted)]
    public async Task<ActionResult<AuthActionResultDto>> RecoverAccountAsync(
        [FromBody] AccountRecoveryRequest request,
        [FromServices] IAuthService authService,
        CancellationToken cancellationToken)
    {
        var result = await authService.RecoverAccountAsync(request, cancellationToken);
        return Accepted(result);
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

    private void WriteSessionCookieIfPresent(
        AuthSessionDto session,
        ISessionTokenService sessionTokenService,
        SessionAuthOptions sessionOptions)
    {
        if (!session.SignedInAtUtc.HasValue)
        {
            return;
        }

        var expiresAtUtc = session.SignedInAtUtc.Value.AddHours(sessionOptions.SessionLifetimeHours);
        var token = sessionTokenService.CreateToken(
            new SessionTokenPayload(session.UserId, session.Email, session.IsAdmin, session.SignedInAtUtc.Value),
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
