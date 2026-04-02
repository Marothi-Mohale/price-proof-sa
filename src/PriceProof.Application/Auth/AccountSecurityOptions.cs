namespace PriceProof.Application.Auth;

public sealed class AccountSecurityOptions
{
    public const string SectionName = "AccountSecurity";

    public bool RequireVerifiedEmailForSignIn { get; set; } = true;

    public int EmailVerificationTokenLifetimeHours { get; set; } = 24;

    public int PasswordResetTokenLifetimeMinutes { get; set; } = 60;

    public int MaxFailedSignInAttempts { get; set; } = 5;

    public int LockoutDurationMinutes { get; set; } = 15;

    public string? PublicAppUrl { get; set; }
}
