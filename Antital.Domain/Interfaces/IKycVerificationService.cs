namespace Antital.Domain.Interfaces;

/// <summary>
/// Abstraction for identity/KYC verification. Implementations can be pass-through (store client-provided paths)
/// or integrate a third-party provider (e.g. Veriff, Onfido). Swap by registering a different implementation in DI.
/// </summary>
public interface IKycVerificationService
{
    /// <summary>
    /// Process KYC submission: either pass through client-provided paths or submit to a third-party and return their references.
    /// </summary>
    Task<KycVerificationResult> ProcessAsync(KycVerificationInput input, CancellationToken cancellationToken = default);
}

/// <summary>
/// Input for KYC verification. Maps from API payload (e.g. KycPayload).
/// </summary>
public record KycVerificationInput(
    int UserId,
    int IdType,
    string? Nin,
    string? Bvn,
    string? GovernmentIdDocumentPathOrKey,
    string? ProofOfAddressDocumentPathOrKey,
    string? SelfieVerificationPathOrKey,
    string? IncomeVerificationPathOrKey,
    string? IncomeVerificationDocumentTypes
);

/// <summary>
/// Result of processing: paths/keys to persist and optional verification timestamps from the provider.
/// </summary>
public record KycVerificationResult(
    string? GovernmentIdDocumentPathOrKey,
    string? ProofOfAddressDocumentPathOrKey,
    string? SelfieVerificationPathOrKey,
    string? IncomeVerificationPathOrKey,
    DateTime? GovernmentIdVerifiedAt,
    DateTime? ProofOfAddressVerifiedAt,
    DateTime? SelfieVerifiedAt,
    DateTime? IncomeVerifiedAt
);
