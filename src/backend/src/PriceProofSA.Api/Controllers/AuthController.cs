using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using PriceProofSA.Api.Contracts;
using PriceProofSA.Api.Security;
using PriceProofSA.Application.Auth;
using PriceProofSA.Infrastructure.Options;

namespace PriceProofSA.Api.Controllers;

[ApiController]
[Route("api/auth")]
public sealed class AuthController : ControllerBase
{
    [HttpPost("sign-up")]
    [AllowAnonymous]
    public async Task<ActionResult<AuthSessionResponse>> SignUpAsync(
        [FromBody] SignUpApiRequest request,
        [FromServices] AuthService authService,
        [FromServices] IOptions<AuthOptions> authOptions,
        CancellationToken cancellationToken)
    {
        var response = await authService.SignUpAsync(
            new SignUpRequest(request.Email, request.DisplayName),
            authOptions.Value.AdminEmails,
            cancellationToken);

        return Ok(response);
    }

    [HttpPost("sign-in")]
    [AllowAnonymous]
    public async Task<ActionResult<AuthSessionResponse>> SignInAsync(
        [FromBody] SignInApiRequest request,
        [FromServices] AuthService authService,
        CancellationToken cancellationToken)
    {
        var response = await authService.SignInAsync(new SignInRequest(request.Email), cancellationToken);
        return Ok(response);
    }

    [HttpGet("me")]
    [Authorize]
    public async Task<ActionResult<CurrentUserDto>> MeAsync(
        [FromServices] AuthService authService,
        CancellationToken cancellationToken)
    {
        var user = await authService.GetCurrentUserAsync(User.RequireUserId(), cancellationToken);
        return user is null ? NotFound() : Ok(user);
    }
}
