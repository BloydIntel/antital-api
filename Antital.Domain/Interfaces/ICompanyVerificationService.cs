namespace Antital.Domain.Interfaces;

public interface ICompanyVerificationService
{
    Task<CompanyVerificationResult> VerifyCorporateCompanyAsync(
        CorporateCompanyVerificationInput input,
        CancellationToken cancellationToken = default);

    Task<CompanyVerificationResult> VerifyFundraiserCompanyAsync(
        FundraiserCompanyVerificationInput input,
        CancellationToken cancellationToken = default);
}

public sealed record CorporateCompanyVerificationInput(
    string CompanyLegalName,
    string RegistrationType,
    string RegistrationNumber,
    DateTime? DateOfRegistration
);

public sealed record FundraiserCompanyVerificationInput(
    string CompanyLegalName,
    string RegistrationType,
    string RegistrationNumber,
    DateTime? DateOfRegistration
);

public sealed record CompanyVerificationResult(
    string? VerifiedCompanyName,
    string? VerifiedRegistrationNumber,
    string? VerifiedCompanyType,
    string? VerificationStatus,
    DateTime? VerifiedAt,
    DateTime? IncorporationDate
);
