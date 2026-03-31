using PriceProofSA.Domain.Common;
using PriceProofSA.Domain.Enums;

namespace PriceProofSA.Domain.Entities;

public sealed class AppUser : BaseEntity
{
    private readonly List<UserSession> _sessions = [];

    private AppUser()
    {
    }

    public string Email { get; private set; } = string.Empty;

    public string NormalizedEmail { get; private set; } = string.Empty;

    public string DisplayName { get; private set; } = string.Empty;

    public UserRole Role { get; private set; }

    public DateTimeOffset CreatedAtUtc { get; private set; }

    public DateTimeOffset LastSeenAtUtc { get; private set; }

    public IReadOnlyCollection<UserSession> Sessions => _sessions;

    public static AppUser Create(string email, string displayName, UserRole role, DateTimeOffset now)
    {
        return new AppUser
        {
            Email = email.Trim(),
            NormalizedEmail = email.Trim().ToLowerInvariant(),
            DisplayName = displayName.Trim(),
            Role = role,
            CreatedAtUtc = now,
            LastSeenAtUtc = now
        };
    }

    public void Touch(DateTimeOffset now)
    {
        LastSeenAtUtc = now;
    }

    public void AddSession(UserSession session)
    {
        _sessions.Add(session);
    }
}
