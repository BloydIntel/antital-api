using Antital.Domain.Interfaces;

namespace Antital.Test.Fakes;

public class FakeDojahClient : IDojahClient
{
    public Func<string, string, DojahCacLookupResult>? LookupCacHandler { get; set; }
    public Func<string, DojahIdentityLookupResult>? LookupBvnHandler { get; set; }
    public Func<string, DojahIdentityLookupResult>? LookupNinHandler { get; set; }
    public Func<string, string, DojahIdentityLookupResult>? LookupPassportHandler { get; set; }
    public Func<string, DojahIdentityLookupResult>? LookupDriversLicenceHandler { get; set; }
    public Func<string, DojahLookupResult>? CheckLivenessHandler { get; set; }
    public Func<string, DojahWidgetVerificationResult>? GetWidgetVerificationHandler { get; set; }

    public Task<DojahCacLookupResult> LookupCacAsync(
        string registrationNumber,
        string companyType,
        CancellationToken cancellationToken = default)
    {
        if (LookupCacHandler != null)
        {
            return Task.FromResult(LookupCacHandler(registrationNumber, companyType));
        }

        return Task.FromResult(new DojahCacLookupResult(
            true, 200, null, registrationNumber, companyType, "ACTIVE", null, "{}", null));
    }

    public Task<DojahIdentityLookupResult> LookupBvnAsync(string bvn, CancellationToken cancellationToken = default)
    {
        if (LookupBvnHandler != null)
        {
            return Task.FromResult(LookupBvnHandler(bvn));
        }

        return Task.FromResult(new DojahIdentityLookupResult(
            true, 200, "JOHN", null, "ADAMU", "2000-05-01", null, "{}", null));
    }

    public Task<DojahIdentityLookupResult> LookupNinAsync(string nin, CancellationToken cancellationToken = default)
    {
        if (LookupNinHandler != null)
        {
            return Task.FromResult(LookupNinHandler(nin));
        }

        return Task.FromResult(new DojahIdentityLookupResult(
            true, 200, "JOHN", null, "ADAMU", "1990-01-01", null, "{}", null));
    }

    public Task<DojahIdentityLookupResult> LookupPassportAsync(
        string passportNumber,
        string surname,
        CancellationToken cancellationToken = default)
    {
        if (LookupPassportHandler != null)
        {
            return Task.FromResult(LookupPassportHandler(passportNumber, surname));
        }

        return Task.FromResult(new DojahIdentityLookupResult(
            true, 200, "JOHN", null, surname.ToUpperInvariant(), "1990-01-01", null, "{}", null));
    }

    public Task<DojahIdentityLookupResult> LookupDriversLicenceAsync(
        string licenseNumber,
        CancellationToken cancellationToken = default)
    {
        if (LookupDriversLicenceHandler != null)
        {
            return Task.FromResult(LookupDriversLicenceHandler(licenseNumber));
        }

        return Task.FromResult(new DojahIdentityLookupResult(
            true, 200, "JOHN", null, "ADAMU", "1990-01-01", null, "{}", null));
    }

    public Task<DojahLookupResult> CheckLivenessAsync(string imageBase64, CancellationToken cancellationToken = default)
    {
        if (CheckLivenessHandler != null)
        {
            return Task.FromResult(CheckLivenessHandler(imageBase64));
        }

        return Task.FromResult(new DojahLookupResult(true, 200, "{}", null));
    }

    public Task<DojahWidgetVerificationResult> GetWidgetVerificationAsync(
        string referenceId,
        CancellationToken cancellationToken = default)
    {
        if (GetWidgetVerificationHandler != null)
        {
            return Task.FromResult(GetWidgetVerificationHandler(referenceId));
        }

        return Task.FromResult(new DojahWidgetVerificationResult(
            true, 200, referenceId, "success", true, true, "https://example.com/selfie.jpg", "{}", null));
    }
}
