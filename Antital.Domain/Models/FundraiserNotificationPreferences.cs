using BuildingBlocks.Domain.Models;

namespace Antital.Domain.Models;

/// <summary>
/// Fundraiser notification preferences for settings (email, in-app, marketing).
/// One row per fundraiser user; missing row means defaults.
/// </summary>
public class FundraiserNotificationPreferences : TrackableEntity
{
    public int UserId { get; set; }

    public bool EmailCampaignUpdates { get; set; } = true;
    public bool EmailNewInvestments { get; set; } = true;
    public bool EmailSecurityAlerts { get; set; } = true;
    public bool EmailMuted { get; set; }

    public bool InAppRealTimeActivity { get; set; } = true;
    public bool InAppChatMessages { get; set; } = true;
    public bool InAppSystemStatus { get; set; } = true;
    public bool InAppMuted { get; set; }

    public bool MarketingProductNews { get; set; } = true;
    public bool MarketingInvestorTips { get; set; } = true;
    public bool MarketingPartner { get; set; }
    public bool MarketingMuted { get; set; }

    public virtual User User { get; set; } = null!;
}
