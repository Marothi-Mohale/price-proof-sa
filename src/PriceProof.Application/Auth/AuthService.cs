using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using PriceProof.Application.Abstractions.Communication;
using PriceProof.Application.Abstractions.Diagnostics;
using PriceProof.Application.Abstractions.Persistence;
using PriceProof.Application.Abstractions.Security;
using PriceProof.Application.Abstractions.Services;
using PriceProof.Application.Common;
using PriceProof.Application.Common.Exceptions;
using PriceProof.Domain.Entities;

namespace PriceProof.Application.Auth;

internal sealed class AuthService : IAuthService
{
    private readonly IAuditLogWriter _auditLogWriter;
    private readonly ICurrentUserContext _currentUserContext;
    private readonly IApplicationDbContext _dbContext;
    private readonly IEmailDeliveryService _emailDeliveryService;
    private readonly IAccountTokenService _accountTokenService;
    private readonly IAccountWorkflowUrlBuilder _accountWorkflowUrlBuilder;
    private readonly IPasswordHashingService _passwordHashingService;
    private readonly AccountSecurityOptions _accountSecurityOptions;

    public AuthService(
        IApplicationDbContext dbContext,
        IPasswordHashingService passwordHashingService,
        IAuditLogWriter auditLogWriter,
        ICurrentUserContext currentUserContext,
        IEmailDeliveryService emailDeliveryService,
        IAccountTokenService accountTokenService,
        IAccountWorkflowUrlBuilder accountWorkflowUrlBuilder,
        IOptions<AccountSecurityOptions> accountSecurityOptions)
    {
        _dbContext = dbContext;
        _passwordHashingService = passwordHashingService;
        _auditLogWriter = auditLogWriter;
        _currentUserContext = currentUserContext;
        _emailDeliveryService = emailDeliveryService;
        _accountTokenService = accountTokenService;
        _accountWorkflowUrlBuilder = accountWorkflowUrlBuilder;
        _accountSecurityOptions = accountSecurityOptions.Value;
    }

    public async Task<AuthSessionDto> SignUpAsync(SignUpRequest request, CancellationToken cancellationToken)
    {
        request = request with
        {
            Email = InputSanitizer.SanitizeRequiredSingleLine(request.Email, 320),
            DisplayName = InputSanitizer.SanitizeRequiredSingleLine(request.DisplayName, 120),
            Password = request.Password.Trim()
        };

        var normalizedEmail = request.Email.Trim().ToUpperInvariant();
        var existingUser = await _dbContext.Users
            .SingleOrDefaultAsync(entity => entity.NormalizedEmail == normalizedEmail, cancellationToken);

        if (existingUser is not null)
        {
            throw new ConflictException("An account with this email address already exists.");
        }

        var user = User.Create(request.DisplayName, request.Email);
        var now = DateTimeOffset.UtcNow;
        var passwordHash = _passwordHashingService.HashPassword(request.Password);
        user.SetPassword(passwordHash.PasswordHash, passwordHash.PasswordSalt, passwordHash.PasswordIterations, now);

        var verificationToken = IssueEmailVerification(user, now);
        _dbContext.Users.Add(user);
        _auditLogWriter.Write(nameof(User), "UserSignedUp", new { user.Id, user.Email }, now, user.Id);
        await _dbContext.SaveChangesAsync(cancellationToken);
        var verificationEmailSent = await TrySendEmailVerificationAsync(user, verificationToken, cancellationToken);

        return MapPendingSession(
            user,
            verificationEmailSent
                ? "Account created. Check your email to verify your address before signing in."
                : "Account created, but verification email delivery is currently unavailable. Use resend verification to try again.");
    }

    public async Task<AuthSessionDto> SignInAsync(SignInRequest request, CancellationToken cancellationToken)
    {
        request = request with
        {
            Email = InputSanitizer.SanitizeRequiredSingleLine(request.Email, 320),
            Password = request.Password.Trim()
        };

        var normalizedEmail = request.Email.Trim().ToUpperInvariant();
        var user = await _dbContext.Users
            .SingleOrDefaultAsync(entity => entity.NormalizedEmail == normalizedEmail, cancellationToken);

        if (user is null)
        {
            throw new ForbiddenException("The email address or password is incorrect.");
        }

        if (!user.IsActive)
        {
            throw new ConflictException("This account is currently inactive.");
        }

        var now = DateTimeOffset.UtcNow;
        if (user.IsLockedOutAt(now))
        {
            throw new ForbiddenException("This account is temporarily locked because of repeated failed sign-in attempts. Use account recovery or try again later.");
        }

        if (string.IsNullOrWhiteSpace(user.PasswordHash) ||
            string.IsNullOrWhiteSpace(user.PasswordSalt) ||
            !user.PasswordIterations.HasValue ||
            !_passwordHashingService.VerifyPassword(request.Password, user.PasswordHash, user.PasswordSalt, user.PasswordIterations.Value))
        {
            user.RecordFailedSignIn(
                now,
                Math.Max(1, _accountSecurityOptions.MaxFailedSignInAttempts),
                TimeSpan.FromMinutes(Math.Max(1, _accountSecurityOptions.LockoutDurationMinutes)));
            _auditLogWriter.Write(
                nameof(User),
                user.IsLockedOutAt(now) ? "UserLockedOut" : "UserSignInFailed",
                new { user.Id, user.Email, user.FailedSignInCount, user.LockoutEndsUtc },
                now,
                user.Id);
            await _dbContext.SaveChangesAsync(cancellationToken);

            if (user.IsLockedOutAt(now))
            {
                throw new ForbiddenException("This account is temporarily locked because of repeated failed sign-in attempts. Use account recovery or try again later.");
            }

            throw new ForbiddenException("The email address or password is incorrect.");
        }

        if (_accountSecurityOptions.RequireVerifiedEmailForSignIn && !user.IsEmailVerified)
        {
            throw new ConflictException("Verify your email address before signing in, or request a new verification email.");
        }

        user.RecordSignIn(now);
        await _auditLogWriter.WriteAndSaveAsync(
            nameof(User),
            "UserSignedIn",
            new { user.Id, user.Email },
            now,
            user.Id,
            cancellationToken: cancellationToken);

        return MapSession(user, now);
    }

    public async Task<AuthActionResultDto> RequestEmailVerificationAsync(
        RequestEmailVerificationRequest request,
        CancellationToken cancellationToken)
    {
        var user = await FindUserByEmailAsync(request.Email, cancellationToken);
        if (user is null || !user.IsActive || user.IsEmailVerified)
        {
            return new AuthActionResultDto("If that account exists, a verification email will be sent shortly.");
        }

        var now = DateTimeOffset.UtcNow;
        var verificationToken = IssueEmailVerification(user, now);
        _auditLogWriter.Write(nameof(User), "EmailVerificationRequested", new { user.Id, user.Email }, now, user.Id);
        await _dbContext.SaveChangesAsync(cancellationToken);
        await TrySendEmailVerificationAsync(user, verificationToken, cancellationToken);
        return new AuthActionResultDto("If that account exists, a verification email will be sent shortly.");
    }

    public async Task<AuthSessionDto> ConfirmEmailVerificationAsync(
        ConfirmEmailVerificationRequest request,
        CancellationToken cancellationToken)
    {
        request = request with
        {
            Email = InputSanitizer.SanitizeRequiredSingleLine(request.Email, 320),
            Token = InputSanitizer.SanitizeRequiredSingleLine(request.Token, 256)
        };

        var user = await _dbContext.Users
            .SingleOrDefaultAsync(entity => entity.NormalizedEmail == request.Email.Trim().ToUpperInvariant(), cancellationToken);

        if (user is null || !user.IsActive)
        {
            throw new ConflictException("The verification link is invalid or has expired.");
        }

        var now = DateTimeOffset.UtcNow;
        if (!user.IsEmailVerified)
        {
            var hashedToken = _accountTokenService.HashToken(request.Token);
            if (string.IsNullOrWhiteSpace(user.EmailVerificationTokenHash) ||
                !string.Equals(user.EmailVerificationTokenHash, hashedToken, StringComparison.Ordinal) ||
                !user.EmailVerificationTokenExpiresUtc.HasValue ||
                user.EmailVerificationTokenExpiresUtc.Value < now)
            {
                throw new ConflictException("The verification link is invalid or has expired.");
            }

            user.MarkEmailVerified(now);
            _auditLogWriter.Write(nameof(User), "EmailVerified", new { user.Id, user.Email }, now, user.Id);
        }

        user.RecordSignIn(now);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return MapSession(user, now, "Email verified successfully.");
    }

    public async Task<AuthActionResultDto> RequestPasswordResetAsync(
        RequestPasswordResetRequest request,
        CancellationToken cancellationToken)
    {
        var user = await FindUserByEmailAsync(request.Email, cancellationToken);
        if (user is null || !user.IsActive || !user.IsEmailVerified)
        {
            return new AuthActionResultDto("If that account exists, password reset instructions will be sent shortly.");
        }

        var now = DateTimeOffset.UtcNow;
        var resetToken = IssuePasswordReset(user, now);
        _auditLogWriter.Write(nameof(User), "PasswordResetRequested", new { user.Id, user.Email }, now, user.Id);
        await _dbContext.SaveChangesAsync(cancellationToken);
        await TrySendPasswordResetAsync(user, resetToken, cancellationToken);
        return new AuthActionResultDto("If that account exists, password reset instructions will be sent shortly.");
    }

    public async Task<AuthSessionDto> ConfirmPasswordResetAsync(
        ConfirmPasswordResetRequest request,
        CancellationToken cancellationToken)
    {
        request = request with
        {
            Email = InputSanitizer.SanitizeRequiredSingleLine(request.Email, 320),
            Token = InputSanitizer.SanitizeRequiredSingleLine(request.Token, 256),
            NewPassword = request.NewPassword.Trim()
        };

        var normalizedEmail = request.Email.Trim().ToUpperInvariant();
        var user = await _dbContext.Users
            .SingleOrDefaultAsync(entity => entity.NormalizedEmail == normalizedEmail, cancellationToken);

        if (user is null || !user.IsActive || !user.IsEmailVerified)
        {
            throw new ConflictException("The password reset link is invalid or has expired.");
        }

        var now = DateTimeOffset.UtcNow;
        var hashedToken = _accountTokenService.HashToken(request.Token);
        if (string.IsNullOrWhiteSpace(user.PasswordResetTokenHash) ||
            !string.Equals(user.PasswordResetTokenHash, hashedToken, StringComparison.Ordinal) ||
            !user.PasswordResetTokenExpiresUtc.HasValue ||
            user.PasswordResetTokenExpiresUtc.Value < now)
        {
            throw new ConflictException("The password reset link is invalid or has expired.");
        }

        var passwordHash = _passwordHashingService.HashPassword(request.NewPassword);
        user.SetPassword(passwordHash.PasswordHash, passwordHash.PasswordSalt, passwordHash.PasswordIterations, now);
        user.ClearLockout(now);
        user.RecordSignIn(now);
        _auditLogWriter.Write(nameof(User), "PasswordResetCompleted", new { user.Id, user.Email }, now, user.Id);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return MapSession(user, now, "Password updated successfully.");
    }

    public async Task<AuthActionResultDto> RecoverAccountAsync(
        AccountRecoveryRequest request,
        CancellationToken cancellationToken)
    {
        var user = await FindUserByEmailAsync(request.Email, cancellationToken);
        if (user is null || !user.IsActive)
        {
            return new AuthActionResultDto("If that account exists, recovery instructions will be sent shortly.");
        }

        var now = DateTimeOffset.UtcNow;
        if (!user.IsEmailVerified)
        {
            var verificationToken = IssueEmailVerification(user, now);
            _auditLogWriter.Write(nameof(User), "AccountRecoveryVerificationIssued", new { user.Id, user.Email }, now, user.Id);
            await _dbContext.SaveChangesAsync(cancellationToken);
            await TrySendEmailVerificationAsync(user, verificationToken, cancellationToken);
        }
        else
        {
            var resetToken = IssuePasswordReset(user, now);
            _auditLogWriter.Write(nameof(User), "AccountRecoveryPasswordResetIssued", new { user.Id, user.Email }, now, user.Id);
            await _dbContext.SaveChangesAsync(cancellationToken);
            await TrySendPasswordResetAsync(user, resetToken, cancellationToken);
        }

        return new AuthActionResultDto("If that account exists, recovery instructions will be sent shortly.");
    }

    public async Task<CurrentUserDto> GetCurrentUserAsync(CancellationToken cancellationToken)
    {
        var currentUserId = CurrentUserGuards.RequireAuthenticatedUserId(_currentUserContext);
        var user = await _dbContext.Users
            .SingleOrDefaultAsync(entity => entity.Id == currentUserId, cancellationToken);

        if (user is null)
        {
            throw new NotFoundException($"User '{currentUserId}' was not found.");
        }

        return new CurrentUserDto(user.Id, user.Email, user.DisplayName, user.IsActive, user.IsAdmin, user.IsEmailVerified);
    }

    private async Task<User?> FindUserByEmailAsync(string email, CancellationToken cancellationToken)
    {
        var sanitizedEmail = InputSanitizer.SanitizeRequiredSingleLine(email, 320);
        var normalizedEmail = sanitizedEmail.Trim().ToUpperInvariant();
        return await _dbContext.Users
            .SingleOrDefaultAsync(entity => entity.NormalizedEmail == normalizedEmail, cancellationToken);
    }

    private string IssueEmailVerification(User user, DateTimeOffset now)
    {
        var token = _accountTokenService.GenerateToken();
        user.IssueEmailVerification(
            _accountTokenService.HashToken(token),
            now.AddHours(Math.Max(1, _accountSecurityOptions.EmailVerificationTokenLifetimeHours)),
            now);
        return token;
    }

    private string IssuePasswordReset(User user, DateTimeOffset now)
    {
        var token = _accountTokenService.GenerateToken();
        user.IssuePasswordReset(
            _accountTokenService.HashToken(token),
            now.AddMinutes(Math.Max(5, _accountSecurityOptions.PasswordResetTokenLifetimeMinutes)),
            now);
        return token;
    }

    private async Task<bool> TrySendEmailVerificationAsync(User user, string token, CancellationToken cancellationToken)
    {
        try
        {
            var verificationUrl = _accountWorkflowUrlBuilder.BuildEmailVerificationUrl(user.Email, token);
            await _emailDeliveryService.SendAsync(
                new EmailMessage(
                    new EmailRecipient(user.Email, user.DisplayName),
                    "Verify your PriceProof SA email address",
                    $"""
                      Hello {user.DisplayName},

                      Please verify your email address to activate your PriceProof SA account:
                      {verificationUrl}

                      If you did not create this account, you can ignore this message.
                      """),
                cancellationToken);
            return true;
        }
        catch (Exception)
        {
            return false;
        }
    }

    private async Task<bool> TrySendPasswordResetAsync(User user, string token, CancellationToken cancellationToken)
    {
        try
        {
            var resetUrl = _accountWorkflowUrlBuilder.BuildPasswordResetUrl(user.Email, token);
            await _emailDeliveryService.SendAsync(
                new EmailMessage(
                    new EmailRecipient(user.Email, user.DisplayName),
                    "Reset your PriceProof SA password",
                    $"""
                      Hello {user.DisplayName},

                      Use this secure link to reset your PriceProof SA password:
                      {resetUrl}

                      If you did not request this reset, you can ignore this message.
                      """),
                cancellationToken);
            return true;
        }
        catch (Exception)
        {
            return false;
        }
    }

    private static AuthSessionDto MapPendingSession(User user, string message)
    {
        return new AuthSessionDto(
            user.Id,
            user.Email,
            user.DisplayName,
            user.IsActive,
            user.IsAdmin,
            user.IsEmailVerified,
            RequiresEmailVerification: !user.IsEmailVerified,
            message,
            SignedInAtUtc: null);
    }

    private AuthSessionDto MapSession(User user, DateTimeOffset signedInAtUtc, string? message = null)
    {
        return new AuthSessionDto(
            user.Id,
            user.Email,
            user.DisplayName,
            user.IsActive,
            user.IsAdmin,
            user.IsEmailVerified,
            RequiresEmailVerification: _accountSecurityOptions.RequireVerifiedEmailForSignIn && !user.IsEmailVerified,
            message,
            SignedInAtUtc: signedInAtUtc);
    }
}
