using System.ComponentModel;
using Antital.Domain.Enums;
using FluentAssertions;
using Xunit;

namespace Antital.Test.Domain.Enums;

public class OnboardingEnumsTests
{
    [Theory]
    [InlineData(OnboardingFlowType.IndividualInvestor, 0, "Individual Investor")]
    [InlineData(OnboardingFlowType.Startup, 1, "Startup")]
    [InlineData(OnboardingFlowType.CorporateInvestor, 2, "Corporate Investor")]
    public void OnboardingFlowType_ShouldHaveExpectedValueAndDescription(OnboardingFlowType value, int expectedInt, string expectedDescription)
    {
        ((int)value).Should().Be(expectedInt);
        GetEnumDescription(value).Should().Be(expectedDescription);
    }

    [Theory]
    [InlineData(OnboardingStep.InvestorCategory, 0)]
    [InlineData(OnboardingStep.InvestmentProfile, 1)]
    [InlineData(OnboardingStep.Kyc, 2)]
    [InlineData(OnboardingStep.Review, 3)]
    [InlineData(OnboardingStep.Submitted, 4)]
    public void OnboardingStep_ShouldHaveExpectedValue(OnboardingStep value, int expectedInt)
    {
        ((int)value).Should().Be(expectedInt);
    }

    [Theory]
    [InlineData(OnboardingStatus.Draft, 0, "Draft")]
    [InlineData(OnboardingStatus.Submitted, 1, "Submitted")]
    [InlineData(OnboardingStatus.UnderReview, 2, "Under Review")]
    [InlineData(OnboardingStatus.Activated, 3, "Activated")]
    public void OnboardingStatus_ShouldHaveExpectedValueAndDescription(OnboardingStatus value, int expectedInt, string expectedDescription)
    {
        ((int)value).Should().Be(expectedInt);
        GetEnumDescription(value).Should().Be(expectedDescription);
    }

    [Theory]
    [InlineData(InvestorCategory.Retail, 0, "Retail Investor")]
    [InlineData(InvestorCategory.Sophisticated, 1, "Sophisticated Investor")]
    [InlineData(InvestorCategory.HighNetWorth, 2, "High Net-Worth Investor (HNI)")]
    public void InvestorCategory_ShouldHaveExpectedValueAndDescription(InvestorCategory value, int expectedInt, string expectedDescription)
    {
        ((int)value).Should().Be(expectedInt);
        GetEnumDescription(value).Should().Be(expectedDescription);
    }

    [Theory]
    [InlineData(KycIdType.NationalIdCard, 0, "National ID Card")]
    [InlineData(KycIdType.InternationalPassport, 1, "International Passport")]
    [InlineData(KycIdType.DriversLicence, 2, "Driver's Licence")]
    public void KycIdType_ShouldHaveExpectedValueAndDescription(KycIdType value, int expectedInt, string expectedDescription)
    {
        ((int)value).Should().Be(expectedInt);
        GetEnumDescription(value).Should().Be(expectedDescription);
    }

    private static string GetEnumDescription(Enum value)
    {
        var field = value.GetType().GetField(value.ToString());
        if (field == null) return value.ToString();
        var attribute = (DescriptionAttribute?)Attribute.GetCustomAttribute(field, typeof(DescriptionAttribute));
        return attribute?.Description ?? value.ToString();
    }
}
