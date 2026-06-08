namespace Antital.Application.DTOs.Investors;

public record InvestorDashboardSummaryDto(
    decimal AvailableBalance,
    decimal TotalInvested,
    decimal TotalReturns,
    string Currency);

public record InvestorDashboardPerformancePointDto(string PeriodLabel, decimal Value);

public record InvestorDashboardActiveDealDto(
    int OfferingId,
    string Slug,
    string Name,
    string LogoUrl,
    decimal Price,
    decimal ChangePercent);

public record InvestorDashboardHoldingDto(
    int OfferingId,
    string Slug,
    string Name,
    string Sector,
    string Risk,
    decimal Invested,
    decimal CurrentValue,
    decimal Returns,
    int UnitHolding,
    DateTime Date);

public record InvestorDashboardResponse(
    InvestorDashboardSummaryDto Summary,
    IReadOnlyList<InvestorDashboardPerformancePointDto> PortfolioPerformance,
    IReadOnlyList<InvestorDashboardActiveDealDto> ActiveDeals,
    IReadOnlyList<InvestorDashboardHoldingDto> Holdings);
