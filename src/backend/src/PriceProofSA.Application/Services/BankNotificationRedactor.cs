using System.Text.RegularExpressions;

namespace PriceProofSA.Application.Services;

public static partial class BankNotificationRedactor
{
    public static string? Redact(string? input)
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            return null;
        }

        var redacted = LongDigitSequenceRegex().Replace(input.Trim(), match =>
        {
            var digits = match.Value;
            return digits.Length <= 4
                ? digits
                : new string('*', digits.Length - 4) + digits[^4..];
        });

        return redacted;
    }

    [GeneratedRegex(@"\d{6,}")]
    private static partial Regex LongDigitSequenceRegex();
}
