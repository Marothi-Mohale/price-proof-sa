namespace PriceProof.Application.Abstractions.Diagnostics;

public static class RequestContextConstants
{
    public const string CorrelationIdHeaderName = "X-Correlation-ID";

    public const string CorrelationIdItemKey = "__PriceProof.CorrelationId";
}
