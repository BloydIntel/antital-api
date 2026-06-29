namespace Antital.Application.DTOs.Investors;

public record InvestorAccountResponse(
    string AccountType,
    string AccountStatus,
    string KycStatus,
    DateTime? KycCompletedDate,
    string InvestorClassification,
    string VerificationStatus,
    DateTime MemberSince,
    string RiskRating,
    InvestorInvestmentLimitsDto? InvestmentLimits,
    IReadOnlyList<InvestorComplianceCheckDto> ComplianceChecks);

public record InvestorInvestmentLimitsDto(
    decimal AnnualLimit,
    decimal UsedPercentage,
    decimal PerProjectLimit,
    decimal LifetimeLimit);

public record InvestorComplianceCheckDto(
    string Id,
    string Label,
    string Status);
