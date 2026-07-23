namespace Antital.Application.DTOs.Fundraisers;

public record FundraiserCampaignUpdateDto(
    int Id,
    string Title,
    string Body,
    string Status,
    DateTime? PublishedAt,
    int LikeCount);

public record FundraiserCampaignUpdatesResponse(
    IReadOnlyList<FundraiserCampaignUpdateDto> Items,
    int Page,
    int PageSize,
    int TotalCount);

public record CreateFundraiserCampaignUpdateRequest(
    string Title,
    string Body,
    bool Publish);

public record UpdateFundraiserCampaignUpdateRequest(
    string? Title,
    string? Body,
    bool? Publish);
