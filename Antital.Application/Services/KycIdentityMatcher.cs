using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace Antital.Application.Services;

/// <summary>
/// Compares Antital signup profile fields to Dojah identity lookup results.
/// Rules: normalized last name exact; normalized first name exact or first-token match;
/// DOB compared when both sides parse (date-only).
/// </summary>
public static class KycIdentityMatcher
{
    private static readonly string[] DobFormats =
    [
        "yyyy-MM-dd",
        "dd-MM-yyyy",
        "dd/MM/yyyy",
        "d/M/yyyy",
        "yyyy/MM/dd",
        "dd-MMM-yyyy",
        "d-MMM-yyyy",
    ];

    public static bool NamesMatch(
        string profileFirstName,
        string profileLastName,
        string? providerFirstName,
        string? providerLastName)
    {
        var profileFirst = NormalizeName(profileFirstName);
        var profileLast = NormalizeName(profileLastName);
        var providerFirst = NormalizeName(providerFirstName);
        var providerLast = NormalizeName(providerLastName);

        if (string.IsNullOrEmpty(profileFirst) || string.IsNullOrEmpty(profileLast))
        {
            return false;
        }

        if (string.IsNullOrEmpty(providerFirst) || string.IsNullOrEmpty(providerLast))
        {
            return false;
        }

        if (!string.Equals(profileLast, providerLast, StringComparison.Ordinal))
        {
            return false;
        }

        if (string.Equals(profileFirst, providerFirst, StringComparison.Ordinal))
        {
            return true;
        }

        // Allow "JOHN DOE" vs "JOHN" / first token of compound given names.
        var profileFirstToken = FirstToken(profileFirst);
        var providerFirstToken = FirstToken(providerFirst);
        return string.Equals(profileFirstToken, providerFirstToken, StringComparison.Ordinal);
    }

    public static bool DatesOfBirthMatch(DateTime profileDob, string? providerDob)
    {
        if (string.IsNullOrWhiteSpace(providerDob))
        {
            // Provider omitted DOB — do not fail solely on DOB.
            return true;
        }

        if (!TryParseProviderDob(providerDob, out var parsed))
        {
            return false;
        }

        return profileDob.Date == parsed.Date;
    }

    public static bool TryParseProviderDob(string value, out DateTime date)
    {
        var trimmed = value.Trim();
        if (DateTime.TryParseExact(
                trimmed,
                DobFormats,
                CultureInfo.InvariantCulture,
                DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal,
                out date))
        {
            date = DateTime.SpecifyKind(date.Date, DateTimeKind.Utc);
            return true;
        }

        if (DateTime.TryParse(trimmed, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out date))
        {
            date = DateTime.SpecifyKind(date.Date, DateTimeKind.Utc);
            return true;
        }

        date = default;
        return false;
    }

    public static string NormalizeName(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        var collapsed = Regex.Replace(value.Trim(), @"\s+", " ");
        var builder = new StringBuilder(collapsed.Length);
        foreach (var ch in collapsed)
        {
            if (char.IsLetter(ch) || ch == ' ')
            {
                builder.Append(char.ToUpperInvariant(ch));
            }
        }

        return Regex.Replace(builder.ToString(), @"\s+", " ").Trim();
    }

    private static string FirstToken(string normalizedName)
    {
        var space = normalizedName.IndexOf(' ');
        return space < 0 ? normalizedName : normalizedName[..space];
    }
}
