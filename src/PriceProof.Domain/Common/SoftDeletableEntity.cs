namespace PriceProof.Domain.Common;

public abstract class SoftDeletableEntity : AuditableEntity
{
    public bool IsDeleted { get; private set; }

    public DateTimeOffset? DeletedUtc { get; private set; }

    public void MarkDeleted(DateTimeOffset now)
    {
        if (IsDeleted)
        {
            return;
        }

        IsDeleted = true;
        DeletedUtc = now;
        UpdatedUtc = now;
    }
}
