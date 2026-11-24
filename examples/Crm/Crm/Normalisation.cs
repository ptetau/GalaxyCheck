namespace Crm;

public static class EmailAddress
{
    /// <summary>
    /// The canonical form of an email address, or null if the text is not an address. The canonical form has no
    /// white space at the ends, is in lower case, and has no sub-address: "Ann+news@Example.com" becomes
    /// "ann@example.com".
    /// </summary>
    public static string? Normalise(string? raw)
    {
        if (raw is null)
        {
            return null;
        }

        var text = raw.Trim().ToLowerInvariant();
        var at = text.IndexOf('@');
        if (at <= 0 || at != text.LastIndexOf('@') || text.Any(char.IsWhiteSpace))
        {
            return null;
        }

        var local = text[..at];
        var domain = text[(at + 1)..];
        var plus = local.IndexOf('+');
        if (plus == 0)
        {
            return null;
        }

        if (plus > 0)
        {
            local = local[..plus];
        }

        var labels = domain.Split('.');
        if (labels.Length < 2 || labels.Any(l => l.Length == 0))
        {
            return null;
        }

        return local + "@" + domain;
    }
}

public static class PhoneNumber
{
    private const string Separators = " -.()";

    /// <summary>
    /// The E.164 form of a phone number ("+" and 8 to 15 digits), or null if the text is not a phone number. A
    /// number that starts with "+" or "00" is international. Another number is national: it loses one leading 0
    /// (a trunk prefix) and gets <paramref name="countryCode"/>. Spaces, dashes, dots, and parentheses are ignored.
    /// </summary>
    public static string? Normalise(string? raw, string countryCode = "1")
    {
        if (raw is null)
        {
            return null;
        }

        var text = raw.Trim();
        var international = text.StartsWith('+');
        if (international)
        {
            text = text[1..];
        }

        if (text.Any(c => !char.IsAsciiDigit(c) && !Separators.Contains(c)))
        {
            return null;
        }

        var digits = new string(text.Where(char.IsAsciiDigit).ToArray());
        if (!international && digits.StartsWith("00"))
        {
            digits = digits[2..];
            international = true;
        }

        if (!international)
        {
            if (digits.StartsWith('0'))
            {
                digits = digits[1..];
            }

            // In North America the national form can repeat the country code: 1-200-555-0100.
            if (countryCode == "1" && digits.Length == 11 && digits[0] == '1')
            {
                digits = digits[1..];
            }

            digits = countryCode + digits;
        }

        return digits.Length is >= 8 and <= 15 && digits[0] != '0' ? "+" + digits : null;
    }
}
