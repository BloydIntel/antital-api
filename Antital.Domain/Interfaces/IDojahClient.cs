namespace Antital.Domain.Interfaces;

public interface IDojahClient
{
    /// <summary>CAC company lookup by RC number and company type.</summary>
    Task<DojahCacLookupResult> LookupCacAsync(
        string registrationNumber,
        string companyType,
        CancellationToken cancellationToken = default);

    /// <summary>BVN full lookup. Sandbox test BVN: <c>22222222222</c>.</summary>
    Task<DojahIdentityLookupResult> LookupBvnAsync(string bvn, CancellationToken cancellationToken = default);

    /// <summary>NIN lookup. Sandbox test NIN: <c>70123456789</c>.</summary>
    Task<DojahIdentityLookupResult> LookupNinAsync(string nin, CancellationToken cancellationToken = default);

    /// <summary>Passport lookup (requires surname). Sandbox test: <c>A00123456</c>.</summary>
    Task<DojahIdentityLookupResult> LookupPassportAsync(
        string passportNumber,
        string surname,
        CancellationToken cancellationToken = default);

    /// <summary>Driver's licence lookup. Sandbox test: <c>FKJ494A2133</c>.</summary>
    Task<DojahIdentityLookupResult> LookupDriversLicenceAsync(
        string licenseNumber,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Passive liveness check from a selfie image (Base64).
    /// Docs: POST /api/v1/ml/liveness/ — probability &gt; 50 means live person.
    /// </summary>
    Task<DojahLookupResult> CheckLivenessAsync(string imageBase64, CancellationToken cancellationToken = default);

    /// <summary>
    /// Fetch EasyOnboard / widget verification details by reference id.
    /// Docs: GET /api/v1/kyc/verification?reference_id=
    /// </summary>
    Task<DojahWidgetVerificationResult> GetWidgetVerificationAsync(
        string referenceId,
        CancellationToken cancellationToken = default);
}

public sealed record DojahCacLookupResult(
    bool IsSuccess,
    int StatusCode,
    string? CompanyName,
    string? RegistrationNumber,
    string? CompanyType,
    string? Status,
    string? IncorporationDate,
    string? RawBody,
    string? ErrorMessage
)
{
    public static DojahCacLookupResult Fail(int statusCode, string? rawBody, string errorMessage) =>
        new(false, statusCode, null, null, null, null, null, rawBody, errorMessage);
}

/// <summary>Raw Dojah HTTP outcome (liveness and fallbacks).</summary>
public sealed record DojahLookupResult(
    bool IsSuccess,
    int StatusCode,
    string? RawBody,
    string? ErrorMessage
);

/// <summary>Parsed EasyOnboard widget verification outcome.</summary>
public sealed record DojahWidgetVerificationResult(
    bool IsSuccess,
    int StatusCode,
    string? ReferenceId,
    string? VerificationStatus,
    bool OverallStatus,
    bool SelfiePassed,
    string? SelfieUrl,
    string? RawBody,
    string? ErrorMessage
)
{
    public static DojahWidgetVerificationResult Fail(int statusCode, string? rawBody, string errorMessage) =>
        new(false, statusCode, null, null, false, false, null, rawBody, errorMessage);
}

/// <summary>Parsed government-ID / BVN identity fields from Dojah <c>entity</c>.</summary>
public sealed record DojahIdentityLookupResult(
    bool IsSuccess,
    int StatusCode,
    string? FirstName,
    string? MiddleName,
    string? LastName,
    /// <summary>Provider DOB string as returned (formats vary by endpoint).</summary>
    string? DateOfBirth,
    string? PhotoBase64,
    string? RawBody,
    string? ErrorMessage
)
{
    public static DojahIdentityLookupResult Fail(int statusCode, string? rawBody, string errorMessage) =>
        new(false, statusCode, null, null, null, null, null, rawBody, errorMessage);
}
