using System.ComponentModel;

namespace Antital.Domain.Enums;

public enum OnboardingStatus
{
    [Description("Draft")]
    Draft = 0,

    [Description("Submitted")]
    Submitted = 1,

    [Description("Under Review")]
    UnderReview = 2,

    [Description("Activated")]
    Activated = 3
}
