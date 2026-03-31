using System.Security.Claims;
using PriceProofSA.Application.Common.Exceptions;

namespace PriceProofSA.Api.Security;

public static class UserContextExtensions
{
    public static Guid RequireUserId(this ClaimsPrincipal principal)
    {
        var rawValue = principal.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(rawValue, out var userId)
            ? userId
            : throw new UnauthorizedAppException("The current session is missing a valid user identifier.");
    }

    public static bool IsAdmin(this ClaimsPrincipal principal)
    {
        return principal.IsInRole("Admin");
    }
}
