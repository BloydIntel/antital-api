namespace Antital.Application.DTOs.Fundraisers;

public record FundraiserEmailNotificationPrefsDto(
    bool CampaignUpdates,
    bool NewInvestments,
    bool SecurityAlerts,
    bool Muted);

public record FundraiserInAppNotificationPrefsDto(
    bool RealTimeActivity,
    bool ChatMessages,
    bool SystemStatus,
    bool Muted);

public record FundraiserMarketingNotificationPrefsDto(
    bool ProductNews,
    bool InvestorTips,
    bool Partner,
    bool Muted);

public record FundraiserNotificationPreferencesResponse(
    FundraiserEmailNotificationPrefsDto Email,
    FundraiserInAppNotificationPrefsDto InApp,
    FundraiserMarketingNotificationPrefsDto Marketing);

public record UpdateFundraiserNotificationPreferencesRequest(
    FundraiserEmailNotificationPrefsDto Email,
    FundraiserInAppNotificationPrefsDto InApp,
    FundraiserMarketingNotificationPrefsDto Marketing);
