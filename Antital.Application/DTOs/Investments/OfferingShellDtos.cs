namespace Antital.Application.DTOs.Investments;

public record OfferingSummaryDto(
    int Id,
    string Slug,
    string Name,
    string Category,
    string Tagline,
    string CoverImageUrl,
    string Risk,
    int? DaysLeft,
    string Status);

public record OfferingFundingDto(
    decimal RaisedAmount,
    decimal FundingGoal,
    int InvestorCount,
    decimal SharePrice,
    decimal? TargetRating,
    decimal MinInvestment,
    decimal MaxInvestment,
    int FundingProgressPercent);

public record DealTermsDto(
    long TotalSharesOffered,
    decimal PricePerShare,
    decimal MinimumInvestment,
    decimal MaximumInvestment,
    decimal MinimumThreshold,
    decimal FundingGoal,
    DateTime Deadline);

public record CorporateProfileDto(
    string EntityType,
    string Jurisdiction,
    int IncorporationYear,
    string RegistrationId,
    string? AdditionalNotes);

public record OfferingShellResponse(
    OfferingSummaryDto Offering,
    OfferingFundingDto Funding,
    DealTermsDto DealTerms,
    CorporateProfileDto? CorporateProfile);
