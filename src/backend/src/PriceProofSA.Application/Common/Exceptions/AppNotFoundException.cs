namespace PriceProofSA.Application.Common.Exceptions;

public sealed class AppNotFoundException : Exception
{
    public AppNotFoundException(string message)
        : base(message)
    {
    }
}
