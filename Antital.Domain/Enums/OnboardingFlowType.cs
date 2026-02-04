using System.ComponentModel;

namespace Antital.Domain.Enums;

public enum OnboardingFlowType
{
    [Description("Individual Investor")]
    IndividualInvestor = 0,

    [Description("Startup")]
    Startup = 1,

    [Description("Corporate Investor")]
    CorporateInvestor = 2
}
