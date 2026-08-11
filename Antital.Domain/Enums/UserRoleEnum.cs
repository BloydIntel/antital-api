using System.ComponentModel;

namespace Antital.Domain.Enums;

public enum UserRoleEnum
{
    [Description("User")]
    User = 0,

    [Description("Admin")]
    Admin = 1
}
