using PriceProofSA.Application.Abstractions.Time;

namespace PriceProofSA.Infrastructure.Time;

public sealed class SystemClock : IClock
{
    public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;
}
