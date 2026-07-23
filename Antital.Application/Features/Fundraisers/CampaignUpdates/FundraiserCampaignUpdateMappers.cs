using Antital.Application.DTOs.Fundraisers;
using Antital.Domain.Enums;
using Antital.Domain.Models;

namespace Antital.Application.Features.Fundraisers.CampaignUpdates;

internal static class FundraiserCampaignUpdateMappers
{
    public static FundraiserCampaignUpdateDto ToDto(OfferingUpdate update) =>
        new(
            update.Id,
            update.Title,
            update.Body,
            ToStatusString(update.Status),
            update.PublishedAt,
            update.LikeCount);

    public static string ToStatusString(OfferingUpdateStatus status) =>
        status switch
        {
            OfferingUpdateStatus.Draft => "draft",
            OfferingUpdateStatus.Published => "published",
            _ => status.ToString().ToLowerInvariant(),
        };

    public static bool TryParseStatusFilter(string? status, out OfferingUpdateStatus? parsed, out string? error)
    {
        parsed = null;
        error = null;

        if (string.IsNullOrWhiteSpace(status) || status.Equals("all", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        if (status.Equals("draft", StringComparison.OrdinalIgnoreCase))
        {
            parsed = OfferingUpdateStatus.Draft;
            return true;
        }

        if (status.Equals("published", StringComparison.OrdinalIgnoreCase))
        {
            parsed = OfferingUpdateStatus.Published;
            return true;
        }

        error = "Status must be all, draft, or published.";
        return false;
    }
}
