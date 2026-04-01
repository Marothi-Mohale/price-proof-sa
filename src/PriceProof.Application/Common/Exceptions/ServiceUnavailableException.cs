namespace PriceProof.Application.Common.Exceptions;

public sealed class ServiceUnavailableException : Exception
{
    public ServiceUnavailableException(string message)
        : base(message)
    {
    }
}
