using System.Xml.Linq;
using Microsoft.AspNetCore.DataProtection.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using PriceProof.Infrastructure.Persistence;
using PriceProof.Infrastructure.Persistence.Entities;

namespace PriceProof.Infrastructure.Auth;

internal sealed class DatabaseXmlRepository : IXmlRepository
{
    private readonly IServiceScopeFactory _serviceScopeFactory;

    public DatabaseXmlRepository(IServiceScopeFactory serviceScopeFactory)
    {
        _serviceScopeFactory = serviceScopeFactory;
    }

    public IReadOnlyCollection<XElement> GetAllElements()
    {
        using var scope = _serviceScopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        return dbContext.DataProtectionKeyRecords
            .AsNoTracking()
            .OrderBy(entity => entity.Id)
            .Select(entity => XElement.Parse(entity.Xml))
            .ToArray();
    }

    public void StoreElement(XElement element, string friendlyName)
    {
        using var scope = _serviceScopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var normalizedFriendlyName = string.IsNullOrWhiteSpace(friendlyName)
            ? $"key-{Guid.NewGuid():N}"
            : friendlyName.Trim();

        var exists = dbContext.DataProtectionKeyRecords
            .Any(entity => entity.FriendlyName == normalizedFriendlyName);

        if (exists)
        {
            return;
        }

        dbContext.DataProtectionKeyRecords.Add(
            DataProtectionKeyRecord.Create(
                normalizedFriendlyName,
                element.ToString(SaveOptions.DisableFormatting),
                DateTimeOffset.UtcNow));
        dbContext.SaveChanges();
    }
}
