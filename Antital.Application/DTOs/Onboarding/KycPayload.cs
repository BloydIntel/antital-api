using Antital.Domain.Enums;

namespace Antital.Application.DTOs.Onboarding;

/// <summary>
/// Payload for the KYC step. Residential address comes from User.
/// Covers: Government ID upload, Proof of address, Selfie verification, Income verification (optional).
/// </summary>
public record KycPayload(
    KycIdType IdType,
    string? Nin,
    string? Bvn,
    string? GovernmentIdDocumentPathOrKey,
    string? ProofOfAddressDocumentPathOrKey,
    string? SelfieVerificationPathOrKey,
    string? IncomeVerificationPathOrKey,
    string? IncomeVerificationDocumentTypesCommaSeparated
);
