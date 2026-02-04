using Antital.Domain.Enums;
using Antital.Domain.Models;
using FluentAssertions;
using Xunit;

namespace Antital.Test.Domain.Models;

public class UserInvestmentProfileTests
{
    [Fact]
    public void UserInvestmentProfile_Creation_WithAllProperties_ShouldSucceed()
    {
        // Arrange
        var userId = 1;
        var investorCategory = InvestorCategory.Retail;
        var pastPercent = 10.5m;
        var nextPercent = 15m;
        var annualIncomeRange = "N5m - N10m";
        var netAssets = 5_000_000m;
        var canAffordToLose = true;
        var understandsHighRisk = true;
        var readRiskDisclosure = true;
        var understandsNoGuarantee = true;
        var awareOfLiquidity = true;

        // Act
        var entity = new UserInvestmentProfile
        {
            UserId = userId,
            InvestorCategory = investorCategory,
            HighRiskAllocationPast12MonthsPercent = pastPercent,
            HighRiskAllocationNext12MonthsPercent = nextPercent,
            AnnualIncomeRange = annualIncomeRange,
            NetInvestmentAssetsValue = netAssets,
            CanAffordToLoseWithoutAffectingStability = canAffordToLose,
            UnderstandsCrowdfundingIsHighRisk = understandsHighRisk,
            ReadRiskDisclosureAndSecRules = readRiskDisclosure,
            UnderstandsPastPerformanceNoGuarantee = understandsNoGuarantee,
            AwareOfLimitedLiquidity = awareOfLiquidity
        };

        // Assert
        entity.UserId.Should().Be(userId);
        entity.InvestorCategory.Should().Be(investorCategory);
        entity.HighRiskAllocationPast12MonthsPercent.Should().Be(pastPercent);
        entity.HighRiskAllocationNext12MonthsPercent.Should().Be(nextPercent);
        entity.AnnualIncomeRange.Should().Be(annualIncomeRange);
        entity.NetInvestmentAssetsValue.Should().Be(netAssets);
        entity.CanAffordToLoseWithoutAffectingStability.Should().Be(canAffordToLose);
        entity.UnderstandsCrowdfundingIsHighRisk.Should().Be(understandsHighRisk);
        entity.ReadRiskDisclosureAndSecRules.Should().Be(readRiskDisclosure);
        entity.UnderstandsPastPerformanceNoGuarantee.Should().Be(understandsNoGuarantee);
        entity.AwareOfLimitedLiquidity.Should().Be(awareOfLiquidity);
    }

    [Fact]
    public void UserInvestmentProfile_Creation_WithOptionalNulls_ShouldSucceed()
    {
        // Act
        var entity = new UserInvestmentProfile
        {
            UserId = 1,
            InvestorCategory = InvestorCategory.Sophisticated
        };

        // Assert
        entity.UserId.Should().Be(1);
        entity.InvestorCategory.Should().Be(InvestorCategory.Sophisticated);
        entity.HighRiskAllocationPast12MonthsPercent.Should().BeNull();
        entity.HighRiskAllocationNext12MonthsPercent.Should().BeNull();
        entity.AnnualIncomeRange.Should().BeNull();
        entity.NetInvestmentAssetsValue.Should().BeNull();
        entity.CanAffordToLoseWithoutAffectingStability.Should().BeNull();
    }

    [Fact]
    public void UserInvestmentProfile_WithAllInvestorCategories_ShouldSetCorrectly()
    {
        foreach (InvestorCategory category in Enum.GetValues(typeof(InvestorCategory)))
        {
            var entity = new UserInvestmentProfile
            {
                UserId = 1,
                InvestorCategory = category
            };
            entity.InvestorCategory.Should().Be(category);
        }
    }
}
