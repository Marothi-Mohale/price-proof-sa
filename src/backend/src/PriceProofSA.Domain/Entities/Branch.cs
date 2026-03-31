using PriceProofSA.Domain.Common;

namespace PriceProofSA.Domain.Entities;

public sealed class Branch : BaseEntity
{
    private Branch()
    {
    }

    public Guid MerchantId { get; private set; }

    public Merchant? Merchant { get; private set; }

    public string Name { get; private set; } = string.Empty;

    public string? AddressLine { get; private set; }

    public string? City { get; private set; }

    public string? Province { get; private set; }

    public DateTimeOffset CreatedAtUtc { get; private set; }

    public static Branch Create(Guid merchantId, string name, string? addressLine, string? city, string? province, DateTimeOffset now)
    {
        return new Branch
        {
            MerchantId = merchantId,
            Name = name.Trim(),
            AddressLine = string.IsNullOrWhiteSpace(addressLine) ? null : addressLine.Trim(),
            City = string.IsNullOrWhiteSpace(city) ? null : city.Trim(),
            Province = string.IsNullOrWhiteSpace(province) ? null : province.Trim(),
            CreatedAtUtc = now
        };
    }
}
