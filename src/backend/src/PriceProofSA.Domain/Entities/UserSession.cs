using PriceProofSA.Domain.Common;

namespace PriceProofSA.Domain.Entities;

public sealed class UserSession : BaseEntity
{
    private UserSession()
    {
    }

    public Guid UserId { get; private set; }

    public AppUser? User { get; private set; }

    public string TokenHash { get; private set; } = string.Empty;

    public DateTimeOffset CreatedAtUtc { get; private set; }

    public DateTimeOffset ExpiresAtUtc { get; private set; }

    public DateTimeOffset? RevokedAtUtc { get; private set; }

    public static UserSession Create(Guid userId, string tokenHash, DateTimeOffset now, TimeSpan lifetime)
    {
        return new UserSession
        {
            UserId = userId,
            TokenHash = tokenHash,
            CreatedAtUtc = now,
            ExpiresAtUtc = now.Add(lifetime)
        };
    }

    public bool IsActive(DateTimeOffset now)
    {
        return RevokedAtUtc is null && ExpiresAtUtc > now;
    }

    public void Revoke(DateTimeOffset now)
    {
        RevokedAtUtc = now;
    }
}
