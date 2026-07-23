namespace Antital.Application.DTOs.Fundraisers;

public record FundraiserCampaignResponse(
    int? OfferingId,
    string? OfferingSlug,
    string? OfferingName,
    string? Status,
    string? PublicPath);
