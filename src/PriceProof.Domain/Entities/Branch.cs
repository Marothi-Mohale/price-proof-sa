using PriceProof.Domain.Common;
using PriceProof.Domain.Enums;

namespace PriceProof.Domain.Entities;

public sealed class Branch : SoftDeletableEntity
{
    private readonly List<DiscrepancyCase> _cases = [];
    private readonly List<BranchRiskSnapshot> _riskSnapshots = [];

    private Branch()
    {
    }

    public Guid MerchantId { get; private set; }

    public Merchant? Merchant { get; private set; }

    public string Name { get; private set; } = string.Empty;

    public string? Code { get; private set; }

    public string AddressLine1 { get; private set; } = string.Empty;

    public string? AddressLine2 { get; private set; }

    public string City { get; private set; } = string.Empty;

    public string Province { get; private set; } = string.Empty;

    public string? PostalCode { get; private set; }

    public decimal? CurrentRiskScore { get; private set; }

    public RiskLabel? CurrentRiskLabel { get; private set; }

    public DateTimeOffset? RiskUpdatedUtc { get; private set; }

    public IReadOnlyCollection<DiscrepancyCase> Cases => _cases;

    public IReadOnlyCollection<BranchRiskSnapshot> RiskSnapshots => _riskSnapshots;

    public static Branch Create(
        Guid merchantId,
        string name,
        string addressLine1,
        string city,
        string province,
        string? code = null,
        string? addressLine2 = null,
        string? postalCode = null)
    {
        return new Branch
        {
            MerchantId = merchantId,
            Name = name.Trim(),
            Code = Normalize(code),
            AddressLine1 = addressLine1.Trim(),
            AddressLine2 = Normalize(addressLine2),
            City = city.Trim(),
            Province = province.Trim(),
            PostalCode = Normalize(postalCode)
        };
    }

    public void UpdateLocation(
        string name,
        string addressLine1,
        string city,
        string province,
        DateTimeOffset now,
        string? code = null,
        string? addressLine2 = null,
        string? postalCode = null)
    {
        Name = name.Trim();
        Code = Normalize(code);
        AddressLine1 = addressLine1.Trim();
        AddressLine2 = Normalize(addressLine2);
        City = city.Trim();
        Province = province.Trim();
        PostalCode = Normalize(postalCode);
        UpdatedUtc = now;
    }

    public void ApplyRiskSnapshot(BranchRiskSnapshot snapshot, DateTimeOffset now)
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
