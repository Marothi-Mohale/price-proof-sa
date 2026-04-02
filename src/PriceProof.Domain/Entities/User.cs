using PriceProof.Domain.Common;

namespace PriceProof.Domain.Entities;

public sealed class User : SoftDeletableEntity
{
    private readonly List<DiscrepancyCase> _reportedCases = [];
    private readonly List<PriceCapture> _priceCaptures = [];
    private readonly List<PaymentRecord> _paymentRecords = [];
    private readonly List<ReceiptRecord> _receiptRecords = [];
    private readonly List<AuditLog> _auditLogs = [];

    private User()
    {
    }

    public string DisplayName { get; private set; } = string.Empty;

    public string Email { get; private set; } = string.Empty;

    public string NormalizedEmail { get; private set; } = string.Empty;

    public bool IsActive { get; private set; }

    public bool IsAdmin { get; private set; }

    public IReadOnlyCollection<DiscrepancyCase> ReportedCases => _reportedCases;

    public IReadOnlyCollection<PriceCapture> PriceCaptures => _priceCaptures;

    public IReadOnlyCollection<PaymentRecord> PaymentRecords => _paymentRecords;

    public IReadOnlyCollection<ReceiptRecord> ReceiptRecords => _receiptRecords;

    public IReadOnlyCollection<AuditLog> AuditLogs => _auditLogs;

    public static User Create(string displayName, string email, bool isAdmin = false)
    {
        var trimmedEmail = email.Trim();

        return new User
        {
            DisplayName = displayName.Trim(),
            Email = trimmedEmail,
            NormalizedEmail = trimmedEmail.ToUpperInvariant(),
            IsActive = true,
            IsAdmin = isAdmin
        };
    }

    public void Deactivate(DateTimeOffset now)
    {
        IsActive = false;
        UpdatedUtc = now;
    }

    public void Reactivate(DateTimeOffset now)
    {
        IsActive = true;
        UpdatedUtc = now;
    }
}
