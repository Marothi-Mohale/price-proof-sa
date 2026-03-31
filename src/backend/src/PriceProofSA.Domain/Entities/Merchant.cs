using PriceProofSA.Domain.Common;

namespace PriceProofSA.Domain.Entities;

public sealed class Merchant : BaseEntity
{
    private readonly List<Branch> _branches = [];

    private Merchant()
    {
    }

    public string Name { get; private set; } = string.Empty;

    public string NormalizedName { get; private set; } = string.Empty;

    public string? Category { get; private set; }

    public DateTimeOffset CreatedAtUtc { get; private set; }

    public MerchantRiskScore? RiskScore { get; private set; }

    public IReadOnlyCollection<Branch> Branches => _branches;

    public static Merchant Create(string name, string? category, DateTimeOffset now)
    {
        return new Merchant
        {
            Name = name.Trim(),
            NormalizedName = name.Trim().ToLowerInvariant(),
            Category = string.IsNullOrWhiteSpace(category) ? null : category.Trim(),
            CreatedAtUtc = now
        };
    }

    public Branch AddBranch(string name, string? addressLine, string? city, string? province, DateTimeOffset now)
    {
        var branch = Branch.Create(Id, name, addressLine, city, province, now);
        _branches.Add(branch);
        return branch;
    }

    public void AttachRiskScore(MerchantRiskScore score)
    {
        RiskScore = score;
    }
}
