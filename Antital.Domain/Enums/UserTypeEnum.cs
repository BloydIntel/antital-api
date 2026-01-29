using System.ComponentModel;

namespace Antital.Domain.Enums;

public enum UserTypeEnum
{
    [Description("Individual Investor")]
    IndividualInvestor = 0,

    [Description("Corporate Investor")]
    CorporateInvestor = 1,

    [Description("Fund Raiser")]
    FundRaiser = 2
}