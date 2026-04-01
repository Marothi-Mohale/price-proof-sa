using Microsoft.AspNetCore.Mvc;
using PriceProof.Application.Abstractions.Services;
using PriceProof.Application.Auth;

namespace PriceProof.Api.Controllers;

[ApiController]
[Route("auth")]
public sealed class AuthController : ControllerBase
{
    [HttpPost("sign-up")]
    [ProducesResponseType(typeof(AuthSessionDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<AuthSessionDto>> SignUpAsync(
        [FromBody] SignUpRequest request,
        [FromServices] IAuthService authService,
        CancellationToken cancellationToken)
    {
        var result = await authService.SignUpAsync(request, cancellationToken);
        return Ok(result);
    }

    [HttpPost("sign-in")]
    [ProducesResponseType(typeof(AuthSessionDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<AuthSessionDto>> SignInAsync(
        [FromBody] SignInRequest request,
        [FromServices] IAuthService authService,
        CancellationToken cancellationToken)
    {
        var result = await authService.SignInAsync(request, cancellationToken);
        return Ok(result);
    }

    [HttpGet("me/{userId:guid}")]
    [ProducesResponseType(typeof(CurrentUserDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<CurrentUserDto>> GetCurrentUserAsync(
        Guid userId,
        [FromServices] IAuthService authService,
        CancellationToken cancellationToken)
    {
        var result = await authService.GetCurrentUserAsync(userId, cancellationToken);
        return Ok(result);
    }
}
