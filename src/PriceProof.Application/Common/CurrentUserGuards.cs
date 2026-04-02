using PriceProof.Application.Abstractions.Security;
using PriceProof.Application.Common.Exceptions;

namespace PriceProof.Application.Common;

public static class CurrentUserGuards
{
    public static Guid RequireAuthenticatedUserId(ICurrentUserContext currentUserContext)
    {
        if (!currentUserContext.IsAuthenticated || !currentUserContext.UserId.HasValue)
        {
            throw new ForbiddenException("A valid signed-in session is required.");
        }

        return currentUserContext.UserId.Value;
    }

    public static void EnsureCanAccessCase(ICurrentUserContext currentUserContext, Guid caseOwnerUserId)
    {
        var currentUserId = RequireAuthenticatedUserId(currentUserContext);
        if (currentUserContext.IsAdmin || currentUserId == caseOwnerUserId)
        {
            return;
        }

        throw new ForbiddenException("You do not have access to this case.");
    }
}
