using PriceProof.Domain.Common;
using PriceProof.Domain.Enums;

namespace PriceProof.Domain.Entities;

public sealed class Merchant : SoftDeletableEntity
{
    private readonly List<Branch> _branches = [];
    private readonly List<DiscrepancyCase> _cases = [];
    private readonly List<MerchantRiskSnapshot> _riskSnapshots = [];

    private Merchant()
    {
    }

    public string Name { get; private set; } = string.Empty;

    public string NormalizedName { get; private set; } = string.Empty;

    public string? Category { get; private set; }

    public string? WebsiteUrl { get; private set; }

    public decimal? CurrentRiskScore { get; private set; }

    public RiskLabel? CurrentRiskLabel { get; private set; }

    public DateTimeOffset? RiskUpdatedUtc { get; private set; }

    public IReadOnlyCollection<Branch> Branches => _branches;

    public IReadOnlyCollection<DiscrepancyCase> Cases => _cases;

    public IReadOnlyCollection<MerchantRiskSnapshot> RiskSnapshots => _riskSnapshots;

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

    public void ApplyRiskSnapshot(MerchantRiskSnapshot snapshot, DateTimeOffset now)
    {
        _riskSnapshots.Add(snapshot);
        CurrentRiskScore = snapshot.Score;
        CurrentRiskLabel = snapshot.Label;
        RiskUpdatedUtc = snapshot.CalculatedUtc;
        UpdatedUtc = now;
    }

    private static string? Normalize(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
}
