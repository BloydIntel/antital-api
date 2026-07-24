using Antital.Application.DTOs.Fundraisers;
using Antital.Domain.Models;

namespace Antital.Application.Features.Fundraisers.Settings;

internal static class FundraiserNotificationPreferencesMapper
{
    public static FundraiserNotificationPreferencesResponse ToResponse(
        FundraiserNotificationPreferences? entity) =>
        entity is null
            ? Defaults()
            : new FundraiserNotificationPreferencesResponse(
                Email: new FundraiserEmailNotificationPrefsDto(
                    entity.EmailCampaignUpdates,
                    entity.EmailNewInvestments,
                    entity.EmailSecurityAlerts,
                    entity.EmailMuted),
                InApp: new FundraiserInAppNotificationPrefsDto(
                    entity.InAppRealTimeActivity,
                    entity.InAppChatMessages,
                    entity.InAppSystemStatus,
                    entity.InAppMuted),
                Marketing: new FundraiserMarketingNotificationPrefsDto(
                    entity.MarketingProductNews,
                    entity.MarketingInvestorTips,
                    entity.MarketingPartner,
                    entity.MarketingMuted));

    public static FundraiserNotificationPreferencesResponse Defaults() =>
        new(
            Email: new FundraiserEmailNotificationPrefsDto(true, true, true, false),
            InApp: new FundraiserInAppNotificationPrefsDto(true, true, true, false),
            Marketing: new FundraiserMarketingNotificationPrefsDto(true, true, false, false));

    public static void ApplyUpdate(
        FundraiserNotificationPreferences entity,
        UpdateFundraiserNotificationPreferencesRequest request)
    {
        entity.EmailCampaignUpdates = request.Email.CampaignUpdates;
        entity.EmailNewInvestments = request.Email.NewInvestments;
        entity.EmailSecurityAlerts = request.Email.SecurityAlerts;
        entity.EmailMuted = request.Email.Muted;

        entity.InAppRealTimeActivity = request.InApp.RealTimeActivity;
        entity.InAppChatMessages = request.InApp.ChatMessages;
        entity.InAppSystemStatus = request.InApp.SystemStatus;
        entity.InAppMuted = request.InApp.Muted;

        entity.MarketingProductNews = request.Marketing.ProductNews;
        entity.MarketingInvestorTips = request.Marketing.InvestorTips;
        entity.MarketingPartner = request.Marketing.Partner;
        entity.MarketingMuted = request.Marketing.Muted;
    }
}
