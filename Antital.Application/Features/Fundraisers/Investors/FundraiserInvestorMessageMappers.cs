using Antital.Application.DTOs.Fundraisers;
using Antital.Domain.Enums;
using Antital.Domain.Models;

namespace Antital.Application.Features.Fundraisers.Investors;

internal static class FundraiserInvestorMessageMappers
{
    public static FundraiserInvestorMessageDto ToDto(OfferingInvestorMessage message) =>
        new(
            message.Id,
            new FundraiserInvestorMessageAuthorDto(
                message.AskerUserId,
                ResolveDisplayName(message.AskerUser),
                AvatarUrl: null),
            message.Question,
            message.AskedAt,
            ToVisibilityString(message.Visibility),
            message.Reply,
            message.RepliedAt,
            message.RepliedAt == null ? "unanswered" : "answered");

    public static string ToVisibilityString(OfferingInvestorMessageVisibility visibility) =>
        visibility switch
        {
            OfferingInvestorMessageVisibility.Public => "public",
            OfferingInvestorMessageVisibility.Private => "private",
            _ => visibility.ToString().ToLowerInvariant(),
        };

    public static bool TryParseMessageStatusFilter(string? status, out bool? answered, out string? error)
    {
        answered = null;
        error = null;

        if (string.IsNullOrWhiteSpace(status) || status.Equals("all", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        if (status.Equals("answered", StringComparison.OrdinalIgnoreCase))
        {
            answered = true;
            return true;
        }

        if (status.Equals("unanswered", StringComparison.OrdinalIgnoreCase))
        {
            answered = false;
            return true;
        }

        error = "Status must be all, answered, or unanswered.";
        return false;
    }

    public static bool TryParseVisibility(string? visibility, out OfferingInvestorMessageVisibility parsed, out string? error)
    {
        parsed = OfferingInvestorMessageVisibility.Private;
        error = null;

        if (string.IsNullOrWhiteSpace(visibility))
        {
            error = "Visibility is required.";
            return false;
        }

        if (visibility.Equals("public", StringComparison.OrdinalIgnoreCase))
        {
            parsed = OfferingInvestorMessageVisibility.Public;
            return true;
        }

        if (visibility.Equals("private", StringComparison.OrdinalIgnoreCase))
        {
            parsed = OfferingInvestorMessageVisibility.Private;
            return true;
        }

        error = "Visibility must be public or private.";
        return false;
    }

    private static string ResolveDisplayName(User? user)
    {
        if (user == null)
        {
            return "Investor";
        }

        if (!string.IsNullOrWhiteSpace(user.PreferredName))
        {
            return user.PreferredName.Trim();
        }

        var fullName = $"{user.FirstName} {user.LastName}".Trim();
        return string.IsNullOrWhiteSpace(fullName) ? user.Email : fullName;
    }
}
