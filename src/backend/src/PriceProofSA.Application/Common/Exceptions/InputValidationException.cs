namespace PriceProofSA.Application.Common.Exceptions;

public sealed class InputValidationException : Exception
{
    public InputValidationException(string message, IReadOnlyDictionary<string, string[]> errors)
        : base(message)
    {
        Errors = errors;
    }

    public IReadOnlyDictionary<string, string[]> Errors { get; }
}
