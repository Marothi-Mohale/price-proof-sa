using Microsoft.EntityFrameworkCore;
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
    private readonly IPasswordHashingService _passwordHashingService;

    public AuthService(
        IApplicationDbContext dbContext,
        IPasswordHashingService passwordHashingService,
        IAuditLogWriter auditLogWriter,
        ICurrentUserContext currentUserContext)
    {
        _dbContext = dbContext;
        _passwordHashingService = passwordHashingService;
        _auditLogWriter = auditLogWriter;
        _currentUserContext = currentUserContext;
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
        user.RecordSignIn(now);
        _dbContext.Users.Add(user);
        _auditLogWriter.Write(nameof(User), "UserSignedUp", new { user.Id, user.Email }, now, user.Id);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return MapSession(user, now);
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

        if (string.IsNullOrWhiteSpace(user.PasswordHash) ||
            string.IsNullOrWhiteSpace(user.PasswordSalt) ||
            !user.PasswordIterations.HasValue ||
            !_passwordHashingService.VerifyPassword(request.Password, user.PasswordHash, user.PasswordSalt, user.PasswordIterations.Value))
        {
            throw new ForbiddenException("The email address or password is incorrect.");
        }

        var now = DateTimeOffset.UtcNow;
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

    public async Task<CurrentUserDto> GetCurrentUserAsync(CancellationToken cancellationToken)
    {
        var currentUserId = CurrentUserGuards.RequireAuthenticatedUserId(_currentUserContext);
        var user = await _dbContext.Users
            .SingleOrDefaultAsync(entity => entity.Id == currentUserId, cancellationToken);

        if (user is null)
        {
            throw new NotFoundException($"User '{currentUserId}' was not found.");
        }

        return new CurrentUserDto(user.Id, user.Email, user.DisplayName, user.IsActive, user.IsAdmin);
    }

    private AuthSessionDto MapSession(User user, DateTimeOffset signedInAtUtc)
    {
        return new AuthSessionDto(
            user.Id,
            user.Email,
            user.DisplayName,
            user.IsActive,
            user.IsAdmin,
            signedInAtUtc);
    }
}
