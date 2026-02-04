using Antital.Domain.Enums;
using Antital.Domain.Models;
using FluentAssertions;
using Xunit;

namespace Antital.Test.Domain.Models;

public class UserOnboardingTests
{
    [Fact]
    public void UserOnboarding_Creation_WithAllProperties_ShouldSucceed()
    {
        // Arrange
        var userId = 1;
        var flowType = OnboardingFlowType.IndividualInvestor;
        var currentStep = OnboardingStep.InvestorCategory;
        var status = OnboardingStatus.Draft;
        var submittedAt = (DateTime?)null;

        // Act
        var entity = new UserOnboarding
        {
            UserId = userId,
            FlowType = flowType,
            CurrentStep = currentStep,
            Status = status,
            SubmittedAt = submittedAt
        };

        // Assert
        entity.UserId.Should().Be(userId);
        entity.FlowType.Should().Be(flowType);
        entity.CurrentStep.Should().Be(currentStep);
        entity.Status.Should().Be(status);
        entity.SubmittedAt.Should().BeNull();
    }

    [Fact]
    public void UserOnboarding_WithSubmittedAt_ShouldStoreValue()
    {
        // Arrange
        var submittedAt = DateTime.UtcNow;

        // Act
        var entity = new UserOnboarding
        {
            UserId = 1,
            FlowType = OnboardingFlowType.IndividualInvestor,
            CurrentStep = OnboardingStep.Submitted,
            Status = OnboardingStatus.Submitted,
            SubmittedAt = submittedAt
        };

        // Assert
        entity.SubmittedAt.Should().Be(submittedAt);
        entity.Status.Should().Be(OnboardingStatus.Submitted);
    }

    [Fact]
    public void UserOnboarding_WithAllFlowTypes_ShouldSetCorrectly()
    {
        var individual = new UserOnboarding
        {
            UserId = 1,
            FlowType = OnboardingFlowType.IndividualInvestor,
            CurrentStep = OnboardingStep.InvestorCategory,
            Status = OnboardingStatus.Draft
        };
        individual.FlowType.Should().Be(OnboardingFlowType.IndividualInvestor);

        var startup = new UserOnboarding
        {
            UserId = 2,
            FlowType = OnboardingFlowType.Startup,
            CurrentStep = OnboardingStep.InvestorCategory,
            Status = OnboardingStatus.Draft
        };
        startup.FlowType.Should().Be(OnboardingFlowType.Startup);

        var corporate = new UserOnboarding
        {
            UserId = 3,
            FlowType = OnboardingFlowType.CorporateInvestor,
            CurrentStep = OnboardingStep.InvestorCategory,
            Status = OnboardingStatus.Draft
        };
        corporate.FlowType.Should().Be(OnboardingFlowType.CorporateInvestor);
    }
}
