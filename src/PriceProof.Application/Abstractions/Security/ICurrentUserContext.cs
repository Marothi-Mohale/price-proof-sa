namespace PriceProof.Application.Abstractions.Security;

public interface ICurrentUserContext
{
    bool IsAuthenticated { get; }

    Guid? UserId { get; }

    string? Email { get; }

    bool IsAdmin { get; }
}
