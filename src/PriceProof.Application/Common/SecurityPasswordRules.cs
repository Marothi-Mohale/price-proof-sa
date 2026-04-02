namespace PriceProof.Application.Common;

internal static class SecurityPasswordRules
{
    public const string PasswordRequirementsMessage =
        "Use at least 12 characters with uppercase, lowercase, number, and symbol characters.";

    public static bool IsStrongPassword(string? password)
    {
        if (string.IsNullOrWhiteSpace(password) || password.Length < 12)
        {
            return false;
        }

        var hasUpper = false;
        var hasLower = false;
        var hasDigit = false;
        var hasSymbol = false;

        foreach (var character in password)
        {
            if (char.IsUpper(character))
            {
                hasUpper = true;
                continue;
            }

            if (char.IsLower(character))
            {
                hasLower = true;
                continue;
            }

            if (char.IsDigit(character))
            {
                hasDigit = true;
                continue;
            }

            if (!char.IsWhiteSpace(character))
            {
                hasSymbol = true;
            }
        }

        return hasUpper && hasLower && hasDigit && hasSymbol;
    }
}
