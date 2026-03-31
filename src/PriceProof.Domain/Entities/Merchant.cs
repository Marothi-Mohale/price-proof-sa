using PriceProof.Domain.Common;

namespace PriceProof.Domain.Entities;

public sealed class Merchant : SoftDeletableEntity
{
    private readonly List<Branch> _branches = [];
    private readonly List<DiscrepancyCase> _cases = [];

    private Merchant()
    {
    }

    public string Name { get; private set; } = string.Empty;

    public string NormalizedName { get; private set; } = string.Empty;

    public string? Category { get; private set; }

    public string? WebsiteUrl { get; private set; }

    public IReadOnlyCollection<Branch> Branches => _branches;

    public IReadOnlyCollection<DiscrepancyCase> Cases => _cases;

    public static Merchant Create(string name, string? category, string? websiteUrl)
    {
        var trimmedName = name.Trim();

        return new Merchant
        {
            Name = trimmedName,
            NormalizedName = trimmedName.ToUpperInvariant(),
            Category = Normalize(category),
            WebsiteUrl = Normalize(websiteUrl)
        };
    }

    public void Rename(string name, string? category, string? websiteUrl, DateTimeOffset now)
    {
        var trimmedName = name.Trim();
        Name = trimmedName;
        NormalizedName = trimmedName.ToUpperInvariant();
        Category = Normalize(category);
        WebsiteUrl = Normalize(websiteUrl);
        UpdatedUtc = now;
    }

    private static string? Normalize(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
}
