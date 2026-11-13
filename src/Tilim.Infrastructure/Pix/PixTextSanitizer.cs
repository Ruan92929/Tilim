using System.Globalization;
using System.Text;

namespace Tilim.Infrastructure.Pix;

/// <summary>
/// Normalizes merchant name/city to plain uppercase ASCII, the charset every
/// Pix-issuing bank expects for BR Code fields 59/60 — so "João" doesn't
/// produce a QR code some banking apps reject.
/// </summary>
public static class PixTextSanitizer
{
    public static string Sanitize(string input, int maxLength)
    {
        var normalized = input.Normalize(NormalizationForm.FormD);
        var builder = new StringBuilder(normalized.Length);

        foreach (var c in normalized)
        {
            var category = CharUnicodeInfo.GetUnicodeCategory(c);
            if (category == UnicodeCategory.NonSpacingMark)
                continue;

            if (char.IsLetterOrDigit(c) || c == ' ')
                builder.Append(char.ToUpperInvariant(c));
        }

        var result = builder.ToString().Trim();
        if (result.Length == 0)
            result = "TILIM";

        return result.Length > maxLength ? result[..maxLength].TrimEnd() : result;
    }
}
