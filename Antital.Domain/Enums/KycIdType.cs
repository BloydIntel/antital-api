using System.ComponentModel;

namespace Antital.Domain.Enums;

public enum KycIdType
{
    [Description("National ID Card")]
    NationalIdCard = 0,

    [Description("International Passport")]
    InternationalPassport = 1,

    [Description("Driver's Licence")]
    DriversLicence = 2
}
