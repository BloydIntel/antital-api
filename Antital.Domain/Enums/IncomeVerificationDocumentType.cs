using System.ComponentModel;

namespace Antital.Domain.Enums;

/// <summary>
/// Income verification document types (KYC sub-step - optional, multi-select).
/// </summary>
public enum IncomeVerificationDocumentType
{
    [Description("Salary slip (last 3 months)")]
    SalarySlip = 0,

    [Description("Tax Return Certificate")]
    TaxReturnCertificate = 1,

    [Description("Employment Letter")]
    EmploymentLetter = 2,

    [Description("Bank statement (Last 3 months)")]
    BankStatement = 3
}
