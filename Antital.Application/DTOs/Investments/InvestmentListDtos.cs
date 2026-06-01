namespace Antital.Application.DTOs.Investments;

public record InvestmentListItemDto(
    int Id,
    string Slug,
    string Name,
    string Category,
    string Tagline,
    string CoverImageUrl,
    string Risk,
    int InvestorCount,
    int? DaysLeft,
    decimal MinInvestment,
    decimal RaisedAmount,
    decimal FundingGoal,
    int FundingProgressPercent);

public record InvestmentListResponse(
    IReadOnlyList<InvestmentListItemDto> Items,
    int Page,
    int PageSize,
    int TotalCount);
