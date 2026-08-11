using Antital.Domain.Interfaces;
using BuildingBlocks.Domain.Interfaces;

namespace Antital.Infrastructure.Integrations.Dojah;

/// <summary>
/// Decorates <see cref="DojahClient"/> to persist an ExternalProviderCheck row per call.
/// </summary>
public sealed class AuditingDojahClient(
    DojahClient inner,
    IExternalProviderCheckRecorder recorder,
    ICurrentUser currentUser
) : IDojahClient
{
    public async Task<DojahCacLookupResult> LookupCacAsync(
        string registrationNumber,
        string companyType,
        CancellationToken cancellationToken = default)
    {
        var result = await inner.LookupCacAsync(registrationNumber, companyType, cancellationToken);
        await recorder.RecordAsync(
            new ExternalProviderCheckEntry(
                ExternalProviderNames.Dojah,
                DojahOperations.CacLookup,
                TryResolveUserId(),
                ExternalReference: null,
                result.IsSuccess,
                result.StatusCode,
                TruncateError(result.ErrorMessage),
                ExternalProviderFingerprint.Mask(registrationNumber)),
            cancellationToken);
        return result;
    }

    public async Task<DojahIdentityLookupResult> LookupBvnAsync(
        string bvn,
        CancellationToken cancellationToken = default)
    {
        var result = await inner.LookupBvnAsync(bvn, cancellationToken);
        await RecordIdentityAsync(DojahOperations.BvnLookup, bvn, result, cancellationToken);
        return result;
    }

    public async Task<DojahIdentityLookupResult> LookupNinAsync(
        string nin,
        CancellationToken cancellationToken = default)
    {
        var result = await inner.LookupNinAsync(nin, cancellationToken);
        await RecordIdentityAsync(DojahOperations.NinLookup, nin, result, cancellationToken);
        return result;
    }

    public async Task<DojahIdentityLookupResult> LookupPassportAsync(
        string passportNumber,
        string surname,
        CancellationToken cancellationToken = default)
    {
        var result = await inner.LookupPassportAsync(passportNumber, surname, cancellationToken);
        await RecordIdentityAsync(DojahOperations.PassportLookup, passportNumber, result, cancellationToken);
        return result;
    }

    public async Task<DojahIdentityLookupResult> LookupDriversLicenceAsync(
        string licenseNumber,
        CancellationToken cancellationToken = default)
    {
        var result = await inner.LookupDriversLicenceAsync(licenseNumber, cancellationToken);
        await RecordIdentityAsync(DojahOperations.DriversLicenceLookup, licenseNumber, result, cancellationToken);
        return result;
    }

    public async Task<DojahLookupResult> CheckLivenessAsync(
        string imageBase64,
        CancellationToken cancellationToken = default)
    {
        var result = await inner.CheckLivenessAsync(imageBase64, cancellationToken);
        await recorder.RecordAsync(
            new ExternalProviderCheckEntry(
                ExternalProviderNames.Dojah,
                DojahOperations.LivenessCheck,
                TryResolveUserId(),
                ExternalReference: null,
                result.IsSuccess,
                result.StatusCode,
                TruncateError(result.ErrorMessage),
                RequestFingerprint: null),
            cancellationToken);
        return result;
    }

    public async Task<DojahWidgetVerificationResult> GetWidgetVerificationAsync(
        string referenceId,
        CancellationToken cancellationToken = default)
    {
        var result = await inner.GetWidgetVerificationAsync(referenceId, cancellationToken);
        await recorder.RecordAsync(
            new ExternalProviderCheckEntry(
                ExternalProviderNames.Dojah,
                DojahOperations.LivenessWidget,
                TryResolveUserId(),
                result.ReferenceId,
                result.IsSuccess,
                result.StatusCode,
                TruncateError(result.ErrorMessage),
                ExternalProviderFingerprint.Mask(referenceId)),
            cancellationToken);
        return result;
    }

    private async Task RecordIdentityAsync(
        string operation,
        string? identifier,
        DojahIdentityLookupResult result,
        CancellationToken cancellationToken)
    {
        await recorder.RecordAsync(
            new ExternalProviderCheckEntry(
                ExternalProviderNames.Dojah,
                operation,
                TryResolveUserId(),
                ExternalReference: null,
                result.IsSuccess,
                result.StatusCode,
                TruncateError(result.ErrorMessage),
                ExternalProviderFingerprint.Mask(identifier)),
            cancellationToken);
    }

    private int? TryResolveUserId()
    {
        if (int.TryParse(currentUser.UserName, out var userId) && userId > 0)
        {
            return userId;
        }

        return null;
    }

    private static string? TruncateError(string? error) =>
        string.IsNullOrWhiteSpace(error)
            ? null
            : error.Length <= 100
                ? error
                : error[..100];
}
