using System.Text;

namespace PriceProof.Application.Common;

internal static class InputSanitizer
{
    public static string SanitizeRequiredSingleLine(string value, int maxLength)
    {
        var sanitized = SanitizeSingleLine(value, maxLength);
        return string.IsNullOrWhiteSpace(sanitized) ? string.Empty : sanitized;
    }

    public static string? SanitizeSingleLine(string? value, int maxLength)
    {
        var sanitized = Sanitize(value, maxLength);
        if (string.IsNullOrWhiteSpace(sanitized))
        {
            return null;
        }

        var singleLine = sanitized
            .Replace("\r\n", " ", StringComparison.Ordinal)
            .Replace('\r', ' ')
            .Replace('\n', ' ')
            .Replace('\t', ' ');

        return CollapseWhitespace(singleLine);
    }

    public static string? SanitizeMultiline(string? value, int maxLength)
    {
        var sanitized = Sanitize(value, maxLength);
        if (string.IsNullOrWhiteSpace(sanitized))
        {
            return null;
        }

        var builder = new StringBuilder(sanitized.Length);
        var previousWasBlankLine = false;

        foreach (var line in sanitized
                     .Replace("\r\n", "\n", StringComparison.Ordinal)
                     .Replace('\r', '\n')
                     .Split('\n'))
        {
            var normalizedLine = CollapseWhitespace(line.Replace('\t', ' '));
            if (string.IsNullOrWhiteSpace(normalizedLine))
            {
                if (previousWasBlankLine || builder.Length == 0)
                {
                    continue;
                }

                builder.AppendLine();
                previousWasBlankLine = true;
                continue;
            }

            if (builder.Length > 0 && !previousWasBlankLine)
            {
                builder.AppendLine();
            }

            builder.Append(normalizedLine);
            previousWasBlankLine = false;
        }

        var result = builder.ToString().Trim();
        return string.IsNullOrWhiteSpace(result) ? null : result;
    }

    public static string SanitizeCurrencyCode(string value)
    {
        var sanitized = SanitizeRequiredSingleLine(value, 3).ToUpperInvariant();
        return sanitized.Length > 3 ? sanitized[..3] : sanitized;
    }

    public static string? SanitizeHash(string? value, int maxLength)
    {
        var sanitized = SanitizeSingleLine(value, maxLength);
        return string.IsNullOrWhiteSpace(sanitized) ? null : sanitized.ToLowerInvariant();
    }

    private static string? Sanitize(string? value, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var trimmed = value.Trim();
        var builder = new StringBuilder(Math.Min(trimmed.Length, maxLength));

        foreach (var character in trimmed)
        {
            if (builder.Length == maxLength)
            {
                break;
            }

            if (char.IsControl(character) && character is not '\r' and not '\n' and not '\t')
            {
                continue;
            }

            builder.Append(character);
        }

        return builder.ToString().Trim();
    }

    private static string CollapseWhitespace(string value)
    {
        var builder = new StringBuilder(value.Length);
        var previousWasWhitespace = false;

        foreach (var character in value)
        {
            if (char.IsWhiteSpace(character))
            {
                if (previousWasWhitespace)
                {
                    continue;
                }

                builder.Append(' ');
                previousWasWhitespace = true;
                continue;
            }

            builder.Append(character);
            previousWasWhitespace = false;
        }

        return builder.ToString().Trim();
    }
}
