using System.Globalization;
using System.Text.RegularExpressions;
using PriceProof.Application.Abstractions.Ocr;

namespace PriceProof.Infrastructure.Ocr;

public sealed partial class ReceiptOcrTextParser
{
    private static readonly string[] DateFormats =
    [
        "yyyy-MM-dd",
        "yyyy/MM/dd",
        "dd/MM/yyyy",
        "dd-MM-yyyy",
        "dd.MM.yyyy",
        "dd MMM yyyy",
        "dd MMMM yyyy"
    ];

    public decimal? ParseTransactionTotal(string? rawText)
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

        foreach (var line in lines.Where(ContainsPriorityKeyword))
        {
            var amount = ParseLargestAmount(line);
            if (amount.HasValue)
            {
                return amount;
            }
        }

        return ParseLargestAmount(normalized);
    }

    public string? ParseMerchantName(string? rawText)
    {
        if (string.IsNullOrWhiteSpace(rawText))
        {
            return null;
        }

        var lines = rawText
            .Replace('\r', '\n')
            .Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        foreach (var line in lines.Take(8))
        {
            if (line.Length is < 2 or > 120)
            {
                continue;
            }

            if (ContainsAdministrativeKeyword(line) || ParseLargestAmount(line).HasValue)
            {
                continue;
            }

            if (line.Count(char.IsLetter) < 2)
            {
                continue;
            }

            return line.Trim();
        }

        return null;
    }

    public DateTimeOffset? ParseTransactionAt(string? rawText)
    {
        if (string.IsNullOrWhiteSpace(rawText))
        {
            return null;
        }

        var normalized = rawText.Replace('\r', '\n');
        var lines = normalized.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        foreach (var line in lines)
        {
            if (TryParseCombinedDateTime(line, out var combined))
            {
                return combined;
            }
        }

        string? dateCandidate = null;
        string? timeCandidate = null;

        foreach (var line in lines)
        {
            dateCandidate ??= ExtractDateCandidate(line);
            timeCandidate ??= ExtractTimeCandidate(line);

            if (dateCandidate is not null && timeCandidate is not null)
            {
                break;
            }
        }

        if (dateCandidate is null || !TryParseDate(dateCandidate, out var date))
        {
            return null;
        }

        if (timeCandidate is not null && TimeSpan.TryParse(timeCandidate, CultureInfo.InvariantCulture, out var time))
        {
            return new DateTimeOffset(date.Add(time), TimeSpan.Zero);
        }

        return new DateTimeOffset(date, TimeSpan.Zero);
    }

    public IReadOnlyCollection<OcrLineItem> ParseLineItems(string? rawText)
    {
        if (string.IsNullOrWhiteSpace(rawText))
        {
            return Array.Empty<OcrLineItem>();
        }

        var lines = rawText
            .Replace('\r', '\n')
            .Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        var items = new List<OcrLineItem>();

        foreach (var line in lines)
        {
            if (ContainsAdministrativeKeyword(line) || IsExplicitTotalLine(line))
            {
                continue;
            }

            var matches = AmountRegex().Matches(line);
            if (matches.Count == 0)
            {
                continue;
            }

            var lastMatch = matches[^1];
            if (!TryParseAmount(lastMatch.Value, out var amount))
            {
                continue;
            }

            var description = line[..lastMatch.Index].Trim(" .:-".ToCharArray());
            if (description.Length < 2 || description.Count(char.IsLetter) < 2)
            {
                continue;
            }

            items.Add(new OcrLineItem(description, amount));

            if (items.Count == 20)
            {
                break;
            }
        }

        return items;
    }

    public decimal? ParseLargestAmount(string input)
    {
        var amounts = AmountRegex().Matches(input)
            .Select(match => TryParseAmount(match.Value, out var value) ? value : (decimal?)null)
            .Where(value => value.HasValue)
            .Select(value => value!.Value)
            .Where(value => value > 0m && value < 1_000_000m)
            .ToArray();

        return amounts.Length == 0 ? null : amounts.Max();
    }

    private static bool TryParseCombinedDateTime(string line, out DateTimeOffset result)
    {
        result = default;

        foreach (var candidate in DateTimeRegex().Matches(line).Select(match => match.Value))
        {
            if (DateTimeOffset.TryParse(candidate, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out result))
            {
                return true;
            }

            if (DateTime.TryParse(candidate, CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsed))
            {
                result = new DateTimeOffset(parsed, TimeSpan.Zero);
                return true;
            }
        }

        return false;
    }

    private static string? ExtractDateCandidate(string line)
    {
        var match = DateRegex().Match(line);
        return match.Success ? match.Value : null;
    }

    private static string? ExtractTimeCandidate(string line)
    {
        var match = TimeRegex().Match(line);
        return match.Success ? match.Value : null;
    }

    private static bool TryParseDate(string input, out DateTime result)
    {
        if (DateTime.TryParseExact(input, DateFormats, CultureInfo.InvariantCulture, DateTimeStyles.None, out result))
        {
            return true;
        }

        if (DateTime.TryParse(input, CultureInfo.InvariantCulture, DateTimeStyles.None, out result))
        {
            result = result.Date;
            return true;
        }

        return false;
    }

    private static bool TryParseAmount(string input, out decimal value)
    {
        return decimal.TryParse(
            input.Replace(',', '.'),
            NumberStyles.AllowDecimalPoint,
            CultureInfo.InvariantCulture,
            out value);
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
               line.Contains("due", StringComparison.OrdinalIgnoreCase) ||
               line.Contains("sale", StringComparison.OrdinalIgnoreCase) ||
               line.Contains("paid", StringComparison.OrdinalIgnoreCase);
    }

    private static bool ContainsAdministrativeKeyword(string line)
    {
        return line.Contains("subtotal", StringComparison.OrdinalIgnoreCase) ||
               line.Contains("tax", StringComparison.OrdinalIgnoreCase) ||
               line.Contains("vat", StringComparison.OrdinalIgnoreCase) ||
               line.Contains("change", StringComparison.OrdinalIgnoreCase) ||
               line.Contains("balance", StringComparison.OrdinalIgnoreCase) ||
               line.Contains("cash", StringComparison.OrdinalIgnoreCase) ||
               line.Contains("card", StringComparison.OrdinalIgnoreCase) ||
               line.Contains("receipt", StringComparison.OrdinalIgnoreCase) ||
               line.Contains("invoice", StringComparison.OrdinalIgnoreCase) ||
               line.Contains("date", StringComparison.OrdinalIgnoreCase) ||
               line.Contains("time", StringComparison.OrdinalIgnoreCase);
    }

    [GeneratedRegex(@"(?<!\d)\d{1,6}(?:[.,]\d{2})(?!\d)")]
    private static partial Regex AmountRegex();

    [GeneratedRegex(@"\b(?:\d{4}[-/]\d{2}[-/]\d{2}|\d{2}[./-]\d{2}[./-]\d{4}|\d{2}\s+[A-Za-z]{3,9}\s+\d{4})\b")]
    private static partial Regex DateRegex();

    [GeneratedRegex(@"\b\d{1,2}:\d{2}(?::\d{2})?\b")]
    private static partial Regex TimeRegex();

    [GeneratedRegex(@"\b(?:\d{4}[-/]\d{2}[-/]\d{2}|\d{2}[./-]\d{2}[./-]\d{4})[ T]\d{1,2}:\d{2}(?::\d{2})?\b")]
    private static partial Regex DateTimeRegex();
}
