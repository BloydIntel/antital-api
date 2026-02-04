using System.ComponentModel;

namespace Antital.Domain.Enums;

/// <summary>
/// Steps for the individual investor onboarding flow.
/// </summary>
public enum OnboardingStep
{
    [Description("Investor Category")]
    InvestorCategory = 0,

    [Description("Investment Profile")]
    InvestmentProfile = 1,

    [Description("Identity Verification (KYC)")]
    Kyc = 2,

    [Description("Review")]
    Review = 3,

    [Description("Submitted")]
    Submitted = 4
}
