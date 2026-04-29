using Antital.Domain.Enums;

namespace Antital.Application.DTOs.Onboarding;

public record CorporateOciProfilePayload(
    bool? HasBoardResolutionOrInternalMandate,
    OciNetAssetValueRange? NetAssetValueRange,
    bool? HasFinancialCapacityToWithstandLoss,
    bool? UnderstandsCrowdfundingHighRiskLoss,
    bool? HasQualifiedInvestmentProfessionalsAccess
);
