using PriceProof.Domain.Common;

namespace PriceProof.Domain.Entities;

public sealed class User : SoftDeletableEntity
{
    private readonly List<DiscrepancyCase> _reportedCases = [];
    private readonly List<PriceCapture> _priceCaptures = [];
    private readonly List<PaymentRecord> _paymentRecords = [];
    private readonly List<ReceiptRecord> _receiptRecords = [];
    private readonly List<AuditLog> _auditLogs = [];

    private User()
    {
    }

    public string DisplayName { get; private set; } = string.Empty;

    public string Email { get; private set; } = string.Empty;

    public string NormalizedEmail { get; private set; } = string.Empty;

    public string? PasswordHash { get; private set; }

    public string? PasswordSalt { get; private set; }

    public int? PasswordIterations { get; private set; }

    public bool IsEmailVerified { get; private set; }

    public DateTimeOffset? EmailVerifiedUtc { get; private set; }

    public string? EmailVerificationTokenHash { get; private set; }

    public DateTimeOffset? EmailVerificationTokenExpiresUtc { get; private set; }

    public DateTimeOffset? EmailVerificationSentUtc { get; private set; }

    public string? PasswordResetTokenHash { get; private set; }

    public DateTimeOffset? PasswordResetTokenExpiresUtc { get; private set; }

    public DateTimeOffset? PasswordResetSentUtc { get; private set; }

    public int FailedSignInCount { get; private set; }

    public DateTimeOffset? LastFailedSignInUtc { get; private set; }

    public DateTimeOffset? LockoutEndsUtc { get; private set; }

    public DateTimeOffset? LastPasswordChangedUtc { get; private set; }

    public DateTimeOffset? LastSignedInUtc { get; private set; }

    public bool IsActive { get; private set; }

    public bool IsAdmin { get; private set; }

    public IReadOnlyCollection<DiscrepancyCase> ReportedCases => _reportedCases;

    public IReadOnlyCollection<PriceCapture> PriceCaptures => _priceCaptures;

    public IReadOnlyCollection<PaymentRecord> PaymentRecords => _paymentRecords;

    public IReadOnlyCollection<ReceiptRecord> ReceiptRecords => _receiptRecords;

    public IReadOnlyCollection<AuditLog> AuditLogs => _auditLogs;

    public static User Create(string displayName, string email, bool isAdmin = false)
    {
        var trimmedEmail = email.Trim();

        return new User
        {
            DisplayName = displayName.Trim(),
            Email = trimmedEmail,
            NormalizedEmail = trimmedEmail.ToUpperInvariant(),
            IsActive = true,
            IsAdmin = isAdmin
        };
    }

    public void SetPassword(string passwordHash, string passwordSalt, int passwordIterations, DateTimeOffset now)
    {
        PasswordHash = passwordHash.Trim();
        PasswordSalt = passwordSalt.Trim();
        PasswordIterations = passwordIterations;
        PasswordResetTokenHash = null;
        PasswordResetTokenExpiresUtc = null;
        PasswordResetSentUtc = null;
        LastPasswordChangedUtc = now;
        UpdatedUtc = now;
    }

    public void RecordSignIn(DateTimeOffset now)
    {
        LastSignedInUtc = now;
        FailedSignInCount = 0;
        LastFailedSignInUtc = null;
        LockoutEndsUtc = null;
        UpdatedUtc = now;
    }

    public void RecordFailedSignIn(DateTimeOffset now, int maxAttempts, TimeSpan lockoutDuration)
    {
        FailedSignInCount = Math.Max(0, FailedSignInCount) + 1;
        LastFailedSignInUtc = now;

        if (FailedSignInCount >= maxAttempts)
        {
            LockoutEndsUtc = now.Add(lockoutDuration);
        }

        UpdatedUtc = now;
    }

    public void ClearLockout(DateTimeOffset now)
    {
        FailedSignInCount = 0;
        LastFailedSignInUtc = null;
        LockoutEndsUtc = null;
        UpdatedUtc = now;
    }

    public bool IsLockedOutAt(DateTimeOffset now)
    {
        return LockoutEndsUtc.HasValue && LockoutEndsUtc.Value > now;
    }

    public void IssueEmailVerification(string tokenHash, DateTimeOffset expiresUtc, DateTimeOffset now)
    {
        EmailVerificationTokenHash = tokenHash.Trim();
        EmailVerificationTokenExpiresUtc = expiresUtc;
        EmailVerificationSentUtc = now;
        UpdatedUtc = now;
    }

    public void MarkEmailVerified(DateTimeOffset now)
    {
        IsEmailVerified = true;
        EmailVerifiedUtc = now;
        EmailVerificationTokenHash = null;
        EmailVerificationTokenExpiresUtc = null;
        EmailVerificationSentUtc = null;
        UpdatedUtc = now;
    }

    public void IssuePasswordReset(string tokenHash, DateTimeOffset expiresUtc, DateTimeOffset now)
    {
        PasswordResetTokenHash = tokenHash.Trim();
        PasswordResetTokenExpiresUtc = expiresUtc;
        PasswordResetSentUtc = now;
        UpdatedUtc = now;
    }

    public void UpdateProfile(string displayName, string email, DateTimeOffset now)
    {
        var trimmedEmail = email.Trim();
        DisplayName = displayName.Trim();
        Email = trimmedEmail;
        NormalizedEmail = trimmedEmail.ToUpperInvariant();
        UpdatedUtc = now;
    }

    public void PromoteToAdmin(DateTimeOffset now)
    {
        IsAdmin = true;
        UpdatedUtc = now;
    }

    public void Deactivate(DateTimeOffset now)
    {
        IsActive = false;
        UpdatedUtc = now;
    }

    public void Reactivate(DateTimeOffset now)
    {
        IsActive = true;
        UpdatedUtc = now;
    }
}
