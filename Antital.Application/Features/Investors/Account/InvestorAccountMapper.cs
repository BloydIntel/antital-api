using System.ComponentModel;
using System.Reflection;
using Antital.Application.DTOs.Investors;
using Antital.Domain.Enums;
using Antital.Domain.Models;

namespace Antital.Application.Features.Investors.Account;

internal static class InvestorAccountMapper
{
    private static readonly IReadOnlyList<InvestorComplianceCheckDto> DefaultComplianceChecks =
    [
        new("aml", "Anti-Money Laundering Check", "Passed"),
        new("sanctions", "Sanctions Screening", "Clear"),
        new("pep", "Politically Exposed Person", "Not Applicable"),
    ];

    public static InvestorAccountResponse ToResponse(
        User user,
        UserOnboarding? onboarding,
        UserInvestmentProfile? profile,
        UserKyc? kyc)
    {
        var kycCompletedDate = kyc != null && IsKycCompleted(kyc) ? DeriveKycCompletedDate(kyc) : null;
        var kycStatus = kycCompletedDate.HasValue ? "Completed" : "Pending";

        return new InvestorAccountResponse(
            AccountType: DeriveAccountType(user, profile),
            AccountStatus: DeriveAccountStatus(user, onboarding),
            KycStatus: kycStatus,
            KycCompletedDate: kycCompletedDate,
            InvestorClassification: DeriveInvestorClassification(profile),
            VerificationStatus: user.IsEmailVerified ? "Verified" : "Unverified",
            MemberSince: user.CreatedAt,
            RiskRating: "Low",
            InvestmentLimits: null,
            ComplianceChecks: DefaultComplianceChecks);
    }

    private static string DeriveAccountType(User user, UserInvestmentProfile? profile)
    {
        if (profile != null)
        {
            return profile.InvestorCategory == InvestorCategory.Retail
                ? "Ordinary Investor"
                : GetEnumDescription(profile.InvestorCategory);
        }

        return user.UserType switch
        {
            UserTypeEnum.CorporateInvestor => "Corporate Investor",
            UserTypeEnum.FundRaiser => "Fundraiser",
            _ => "Ordinary Investor",
        };
    }

    private static string DeriveAccountStatus(User user, UserOnboarding? onboarding)
    {
        if (user.IsDeleted)
        {
            return "Suspended";
        }

        var status = onboarding?.Status ?? OnboardingStatus.Draft;
        return status is OnboardingStatus.Draft or OnboardingStatus.UnderReview
            ? "Pending"
            : "Active";
    }

    private static string DeriveInvestorClassification(UserInvestmentProfile? profile)
    {
        if (profile == null)
        {
            return "Ordinary";
        }

        return profile.InvestorCategory switch
        {
            InvestorCategory.Retail => "Ordinary",
            InvestorCategory.Sophisticated => "Sophisticated",
            InvestorCategory.HighNetWorth => "HNI",
            InvestorCategory.QualifiedInstitutionalInvestor => "QII",
            InvestorCategory.OtherCorporateInvestor => "OCI",
            _ => "Ordinary",
        };
    }

    private static bool IsKycCompleted(UserKyc kyc) =>
        kyc.GovernmentIdVerifiedAt.HasValue
        && kyc.ProofOfAddressVerifiedAt.HasValue
        && kyc.SelfieVerifiedAt.HasValue;

    private static DateTime? DeriveKycCompletedDate(UserKyc kyc)
    {
        var dates = new[]
        {
            kyc.GovernmentIdVerifiedAt,
            kyc.ProofOfAddressVerifiedAt,
            kyc.SelfieVerifiedAt,
            kyc.IncomeVerifiedAt,
        }.Where(d => d.HasValue).Select(d => d!.Value).ToList();

        return dates.Count == 0 ? null : dates.Max();
    }

    private static string GetEnumDescription(InvestorCategory category)
    {
        var field = category.GetType().GetField(category.ToString());
        var attribute = field?.GetCustomAttribute<DescriptionAttribute>();
        return attribute?.Description ?? category.ToString();
    }
}
