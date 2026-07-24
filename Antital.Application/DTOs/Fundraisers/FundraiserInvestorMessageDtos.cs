namespace Antital.Application.DTOs.Fundraisers;

public record FundraiserInvestorMessageAuthorDto(
    int UserId,
    string DisplayName,
    string? AvatarUrl);

public record FundraiserInvestorMessageDto(
    int Id,
    FundraiserInvestorMessageAuthorDto Author,
    string Question,
    DateTime AskedAt,
    string Visibility,
    string? Reply,
    DateTime? RepliedAt,
    string Status);

public record FundraiserInvestorMessagesResponse(
    IReadOnlyList<FundraiserInvestorMessageDto> Items,
    int Page,
    int PageSize,
    int TotalCount,
    int NewCount);

public record ReplyFundraiserInvestorMessageRequest(string Reply);

public record UpdateFundraiserInvestorMessageRequest(
    string? Visibility,
    string? Reply);

public record FundraiserInvestorAnalyticsResponse(
    decimal ResponseRate,
    double? AverageResponseTimeHours,
    int TotalMessages,
    int AnsweredCount,
    int UnansweredCount);
