using System.Text.RegularExpressions;
using Antital.Domain.Enums;

namespace Antital.Application.Features.Onboarding;

/// <summary>Server-side format rules for KYC ID number / BVN (mirrored on UI in CP5).</summary>
public static class KycIdNumberRules
{
    private static readonly Regex ElevenDigits = new(@"^\d{11}$", RegexOptions.Compiled);
    private static readonly Regex PassportNumber = new(@"^[A-Za-z]\d{8}$", RegexOptions.Compiled);
    private static readonly Regex DriversLicenceNumber = new(@"^[A-Za-z0-9]{5,15}$", RegexOptions.Compiled);

    public static bool IsValidBvn(string? bvn) =>
        string.IsNullOrWhiteSpace(bvn) || ElevenDigits.IsMatch(bvn.Trim());

    public static bool IsValidIdNumber(KycIdType idType, string? idNumber)
    {
        if (string.IsNullOrWhiteSpace(idNumber))
        {
            return true;
        }

        var value = idNumber.Trim();
        return idType switch
        {
            KycIdType.NationalIdCard => ElevenDigits.IsMatch(value),
            KycIdType.InternationalPassport => PassportNumber.IsMatch(value),
            KycIdType.DriversLicence => DriversLicenceNumber.IsMatch(value),
            _ => false,
        };
    }

    public static string IdNumberErrorMessage(KycIdType idType) =>
        idType switch
        {
            KycIdType.NationalIdCard => "NIN must be exactly 11 digits.",
            KycIdType.InternationalPassport => "Passport number must be 1 letter followed by 8 digits (e.g. A00123456).",
            KycIdType.DriversLicence => "Driver's licence number must be 5–15 alphanumeric characters.",
            _ => "ID number format is invalid.",
        };
}
