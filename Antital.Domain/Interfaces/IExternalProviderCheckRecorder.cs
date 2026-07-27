namespace Antital.Domain.Interfaces;

public interface IExternalProviderCheckRecorder
{
    Task RecordAsync(ExternalProviderCheckEntry entry, CancellationToken cancellationToken = default);
}

public sealed record ExternalProviderCheckEntry(
    string Provider,
    string Operation,
    int? UserId,
    string? ExternalReference,
    bool Success,
    int StatusCode,
    string? ErrorCode,
    string? RequestFingerprint
);

public static class ExternalProviderNames
{
    public const string Dojah = "Dojah";
    public const string Paystack = "Paystack";
}

public static class DojahOperations
{
    public const string CacLookup = "CacLookup";
    public const string BvnLookup = "BvnLookup";
    public const string NinLookup = "NinLookup";
    public const string PassportLookup = "PassportLookup";
    public const string DriversLicenceLookup = "DriversLicenceLookup";
    public const string LivenessCheck = "LivenessCheck";
    public const string LivenessWidget = "LivenessWidget";
}

public static class ExternalProviderFingerprint
{
    /// <summary>Masks an identifier, keeping at most the last 4 characters.</summary>
    public static string? Mask(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var trimmed = value.Trim();
        if (trimmed.Length <= 4)
        {
            return new string('*', trimmed.Length);
        }

        return new string('*', trimmed.Length - 4) + trimmed[^4..];
    }
}
