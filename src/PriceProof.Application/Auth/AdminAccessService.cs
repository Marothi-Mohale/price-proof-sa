using Microsoft.EntityFrameworkCore;
using PriceProof.Application.Abstractions.Persistence;
using PriceProof.Application.Abstractions.Security;
using PriceProof.Application.Abstractions.Services;
using PriceProof.Application.Common.Exceptions;

namespace PriceProof.Application.Auth;

internal sealed class AdminAccessService : IAdminAccessService
{
    private readonly IApplicationDbContext _dbContext;
    private readonly ISessionTokenService _sessionTokenService;

    public AdminAccessService(IApplicationDbContext dbContext, ISessionTokenService sessionTokenService)
    {
        _dbContext = dbContext;
        _sessionTokenService = sessionTokenService;
    }

    public async Task<CurrentUserDto> RequireAdminAsync(string? authorizationHeader, CancellationToken cancellationToken)
    {
        var token = ExtractBearerToken(authorizationHeader);

        if (!_sessionTokenService.TryReadToken(token, out var payload) || payload is null)
        {
            throw new ForbiddenException("A valid admin session is required.");
        }

        var user = await _dbContext.Users
            .AsNoTracking()
            .SingleOrDefaultAsync(entity => entity.Id == payload.UserId, cancellationToken);

        if (user is null || !user.IsActive || !user.IsAdmin)
        {
            throw new ForbiddenException("A valid admin session is required.");
        }

        return new CurrentUserDto(user.Id, user.Email, user.DisplayName, user.IsActive, user.IsAdmin);
    }

    private static string ExtractBearerToken(string? authorizationHeader)
    {
        if (string.IsNullOrWhiteSpace(authorizationHeader))
        {
            throw new ForbiddenException("A valid admin session is required.");
        }

        const string prefix = "Bearer ";

        return authorizationHeader.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)
            ? authorizationHeader[prefix.Length..].Trim()
            : throw new ForbiddenException("A valid admin session is required.");
    }
}
