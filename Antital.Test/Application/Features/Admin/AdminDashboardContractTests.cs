using Antital.Application.DTOs.Admin;
using Antital.Application.Features.Admin.GetAdminDashboard;
using FluentAssertions;
using Xunit;

namespace Antital.Test.Application.Features.Admin;

public class AdminDashboardContractTests
{
    [Fact]
    public void GetAdminDashboardQuery_DefaultsToLastThirtyDays()
    {
        var query = new GetAdminDashboardQuery();

        query.Period.Should().Be(AdminDashboardPeriodResolver.DefaultPeriod);
    }

    [Fact]
    public void AdminDashboardResponse_SupportsNullableGrowthAndRoutes()
    {
        var response = new AdminDashboardResponse(
            new AdminDashboardSummaryDto(
                TotalInvestors: 0,
                NewInvestorsInPeriod: 0,
                NewInvestorsInPreviousPeriod: 0,
                InvestorGrowthPercent: null,
                ActiveCampaigns: 0,
                ActiveCampaignRaisedAmount: 0m,
                TotalCampaigns: 0,
                TotalFundsRaised: 0m,
                Currency: "NGN"),
            [
                new AdminDashboardActionDto(
                    AdminDashboardActionType.PendingOnboardingReview,
                    "Pending Onboarding Reviews",
                    "No reviews awaiting attention",
                    0,
                    null),
            ],
            [
                new AdminDashboardRecentActivityDto(
                    "investor:1",
                    AdminDashboardActivityType.InvestorRegistered,
                    "Investor registered",
                    "An investor account was created.",
                    new DateTime(2026, 10, 4, 18, 30, 0, DateTimeKind.Utc),
                    null),
            ]);

        response.Summary.InvestorGrowthPercent.Should().BeNull();
        response.Summary.Currency.Should().Be("NGN");
        response.Actions.Should().ContainSingle();
        response.Actions[0].Type.Should().Be(AdminDashboardActionType.PendingOnboardingReview);
        response.RecentActivity.Should().ContainSingle();
        response.RecentActivity[0].OccurredAtUtc.Kind.Should().Be(DateTimeKind.Utc);
    }
}
