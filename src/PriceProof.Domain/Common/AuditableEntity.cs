namespace PriceProof.Domain.Common;

public abstract class AuditableEntity : EntityBase
{
    public DateTimeOffset CreatedUtc { get; set; }

    public DateTimeOffset UpdatedUtc { get; set; }
}
