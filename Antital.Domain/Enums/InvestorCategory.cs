using System.ComponentModel;

namespace Antital.Domain.Enums;

/// <summary>
/// Nigerian SEC investor categorization for onboarding (individual + corporate).
/// </summary>
public enum InvestorCategory
{
    [Description("Retail Investor")]
    Retail = 0,

    [Description("Sophisticated Investor")]
    Sophisticated = 1,

    [Description("High Net-Worth Investor (HNI)")]
    HighNetWorth = 2,

    [Description("Qualified Institutional Investor (QII)")]
    QualifiedInstitutionalInvestor = 3,

    [Description("Other Corporate Investor (OCI)")]
    OtherCorporateInvestor = 4
}
