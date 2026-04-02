using FluentAssertions;
using PriceProof.Domain.Entities;

namespace PriceProof.UnitTests.Auth;

public sealed class UserSecurityStateTests
{
    [Fact]
    public void RecordFailedSignIn_LocksUser_WhenThresholdIsReached()
    {
        var user = User.Create("Test User", "user@example.com");
        var now = DateTimeOffset.UtcNow;

        user.RecordFailedSignIn(now, maxAttempts: 3, lockoutDuration: TimeSpan.FromMinutes(15));
        user.RecordFailedSignIn(now.AddMinutes(1), maxAttempts: 3, lockoutDuration: TimeSpan.FromMinutes(15));
        user.RecordFailedSignIn(now.AddMinutes(2), maxAttempts: 3, lockoutDuration: TimeSpan.FromMinutes(15));

        user.FailedSignInCount.Should().Be(3);
        user.IsLockedOutAt(now.AddMinutes(2)).Should().BeTrue();
        user.LockoutEndsUtc.Should().Be(now.AddMinutes(17));
    }

    [Fact]
    public void MarkEmailVerified_ClearsPendingVerificationState()
    {
        var user = User.Create("Test User", "user@example.com");
        var now = DateTimeOffset.UtcNow;

        user.IssueEmailVerification("hashed-token", now.AddHours(4), now);
        user.MarkEmailVerified(now.AddMinutes(5));

        user.IsEmailVerified.Should().BeTrue();
        user.EmailVerifiedUtc.Should().Be(now.AddMinutes(5));
        user.EmailVerificationTokenHash.Should().BeNull();
        user.EmailVerificationTokenExpiresUtc.Should().BeNull();
        user.EmailVerificationSentUtc.Should().BeNull();
    }

    [Fact]
    public void SetPassword_ClearsPasswordResetState_AndUnlocksCanBeHandledSeparately()
    {
        var user = User.Create("Test User", "user@example.com");
        var now = DateTimeOffset.UtcNow;

        user.IssuePasswordReset("reset-token", now.AddMinutes(30), now);
        user.SetPassword("hash", "salt", 1000, now.AddMinutes(1));

        user.PasswordResetTokenHash.Should().BeNull();
        user.PasswordResetTokenExpiresUtc.Should().BeNull();
        user.PasswordResetSentUtc.Should().BeNull();
        user.LastPasswordChangedUtc.Should().Be(now.AddMinutes(1));
    }
}
