namespace PriceProof.Application.Abstractions.Diagnostics;

public interface IRequestContextAccessor
{
    string CorrelationId { get; }
}
