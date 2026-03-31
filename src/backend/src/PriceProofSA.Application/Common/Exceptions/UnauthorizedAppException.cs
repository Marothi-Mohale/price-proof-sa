namespace PriceProofSA.Application.Common.Exceptions;

public sealed class UnauthorizedAppException : Exception
{
    public UnauthorizedAppException(string message)
        : base(message)
    {
    }
}
