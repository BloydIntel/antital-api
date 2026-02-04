using System.ComponentModel;

namespace Antital.Domain.Enums;

/// <summary>
/// Source of wealth (AML - multi-select across profiles).
/// </summary>
public enum SourceOfWealth
{
    [Description("Employment")]
    Employment = 0,

    [Description("Business Ownership")]
    BusinessOwnership = 1,

    [Description("Investments & Capital")]
    InvestmentsAndCapital = 2,

    [Description("Inheritance")]
    Inheritance = 3,

    [Description("Sale of Major Assets")]
    SaleOfMajorAssets = 4,

    [Description("Legal Settlement")]
    LegalSettlement = 5,

    [Description("Gifts/Donations")]
    GiftsDonations = 6,

    [Description("Other")]
    Other = 7
}
