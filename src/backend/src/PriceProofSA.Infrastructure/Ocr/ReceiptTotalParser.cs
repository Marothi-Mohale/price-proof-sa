using System.Globalization;
using System.Text.RegularExpressions;

namespace PriceProofSA.Infrastructure.Ocr;

public sealed partial class ReceiptTotalParser
{
    public decimal? Parse(string? rawText)
    {
        if (string.IsNullOrWhiteSpace(rawText))
        {
            return null;
        }

        var normalized = rawText.Replace('\r', '\n');
        var lines = normalized.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        foreach (var line in lines.Where(IsExplicitTotalLine))
        {
            var amount = ParseLargestAmount(line);
            if (amount.HasValue)
            {
                return amount;
            }
        }

        foreach (var line in lines)
        {
            if (ContainsPriorityKeyword(line))
            {
                var amount = ParseLargestAmount(line);
                if (amount.HasValue)
                {
                    return amount;
                }
            }
        }

        return ParseLargestAmount(normalized);
    }

    public decimal? ParseLargestAmount(string input)
    {
        var matches = AmountRegex().Matches(input);
        var amounts = matches
            .Select(match => decimal.TryParse(match.Value.Replace(',', '.'), NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out var value)
                ? value
                : (decimal?)null)
            .Where(value => value.HasValue)
            .Select(value => value!.Value)
            .Where(value => value > 0m && value < 1_000_000m)
            .ToArray();

        return amounts.Length == 0 ? null : amounts.Max();
    }

    private static bool IsExplicitTotalLine(string line)
    {
        return line.Contains("total", StringComparison.OrdinalIgnoreCase) &&
               !line.Contains("subtotal", StringComparison.OrdinalIgnoreCase);
    }

    private static bool ContainsPriorityKeyword(string line)
    {
        return line.Contains("total", StringComparison.OrdinalIgnoreCase) ||
               line.Contains("amount", StringComparison.OrdinalIgnoreCase) ||
               line.Contains("card", StringComparison.OrdinalIgnoreCase) ||
               line.Contains("sale", StringComparison.OrdinalIgnoreCase) ||
               line.Contains("due", StringComparison.OrdinalIgnoreCase);
    }

    [GeneratedRegex(@"(?<!\d)\d{1,6}(?:[.,]\d{2})(?!\d)")]
    private static partial Regex AmountRegex();
}
