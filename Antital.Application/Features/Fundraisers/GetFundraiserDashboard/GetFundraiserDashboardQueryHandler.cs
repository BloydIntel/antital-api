using Antital.Application.DTOs.Fundraisers;
using Antital.Application.Features.Investments;
using Antital.Application.Features.Investors;
using Antital.Domain.Enums;
using Antital.Domain.Interfaces;
using Antital.Domain.Models;
using BuildingBlocks.Application.Features;

namespace Antital.Application.Features.Fundraisers.GetFundraiserDashboard;

public class GetFundraiserDashboardQueryHandler(
    IFundraiserUserAccess fundraiserUserAccess,
    IFundraiserDashboardRepository dashboardRepository
) : ICommandQueryHandler<GetFundraiserDashboardQuery, FundraiserDashboardResponse>
{
    private static readonly (string Label, decimal MinInclusive, decimal? MaxExclusive)[] SizeBuckets =
    [
        ("0 - 5M", 0m, 6_000_000m),
        ("6M - 20M", 6_000_000m, 21_000_000m),
        ("21M - 100M", 21_000_000m, 101_000_000m),
        ("101M - 500M", 101_000_000m, null),
    ];

    public async Task<Result<FundraiserDashboardResponse>> Handle(
        GetFundraiserDashboardQuery request,
        CancellationToken cancellationToken)
    {
        if (!DashboardPeriodResolver.TryResolve(request.Period, out _, out var periodError))
        {
            var invalidResult = new Result<FundraiserDashboardResponse>();
            invalidResult.BadRequest(
                "Invalid period.",
                new Dictionary<string, string[]>
                {
                    ["period"] =
                    [
                        periodError
                        ?? "Period must be this-month, last-month, last-3-months, last-6-months, or last-12-months.",
                    ],
                });
            return invalidResult;
        }

        var (userId, _) = await fundraiserUserAccess.RequireFundraiserAsync(cancellationToken);
        var offering = await dashboardRepository.GetPrimaryOfferingAsync(userId, cancellationToken);

        if (offering?.Funding == null || offering.DealTerms == null)
        {
            var empty = FundraiserDashboardMappers.Empty();
            var emptyResult = new Result<FundraiserDashboardResponse>();
            emptyResult.AddValue(empty);
            emptyResult.OK();
            return emptyResult;
        }

        var holdings = await dashboardRepository.GetHoldingsForOfferingAsync(offering.Id, cancellationToken);
        var response = FundraiserDashboardMappers.ToResponse(offering, holdings, SizeBuckets);

        var result = new Result<FundraiserDashboardResponse>();
        result.AddValue(response);
        result.OK();
        return result;
    }
}

internal static class FundraiserDashboardMappers
{
    public static FundraiserDashboardResponse Empty() =>
        new(
            null,
            null,
            null,
            "NGN",
            new FundraiserDashboardSummaryDto(0m, 0, 0, 0m),
            new FundraiserFundingProgressDto(0m, 0m, 0m, 0m, "week", 0),
            new FundraiserInvestorBreakdownDto(
                "size",
                SizeBucketLabels().Select(label => new FundraiserBreakdownBucketDto(label, 0)).ToList()),
            []);

    public static FundraiserDashboardResponse ToResponse(
        InvestmentOffering offering,
        IReadOnlyList<InvestorHolding> holdings,
        (string Label, decimal MinInclusive, decimal? MaxExclusive)[] sizeBuckets)
    {
        var funding = offering.Funding!;
        var dealTerms = offering.DealTerms!;
        var raised = funding.RaisedAmount;
        var investors = funding.InvestorCount;
        var goal = dealTerms.FundingGoal;
        var threshold = dealTerms.MinimumThreshold;
        var daysRemaining = InvestmentMappers.ComputeDaysLeft(dealTerms.Deadline) ?? 0;
        var average = investors > 0 ? decimal.Round(raised / investors, 2, MidpointRounding.AwayFromZero) : 0m;
        var velocity = ComputeWeeklyVelocity(holdings);
        var confidence = ComputeConfidenceRate(
            raised,
            goal,
            threshold,
            offering.PublishedAt,
            dealTerms.Deadline);

        return new FundraiserDashboardResponse(
            offering.Id,
            offering.Slug,
            offering.Name,
            "NGN",
            new FundraiserDashboardSummaryDto(raised, investors, daysRemaining, average),
            new FundraiserFundingProgressDto(raised, goal, threshold, velocity, "week", confidence),
            new FundraiserInvestorBreakdownDto("size", BuildSizeBuckets(holdings, sizeBuckets)),
            BuildMilestones(offering, raised, goal, dealTerms.Deadline));
    }

    private static IReadOnlyList<string> SizeBucketLabels() =>
    [
        "0 - 5M",
        "6M - 20M",
        "21M - 100M",
        "101M - 500M",
    ];

    private static decimal ComputeWeeklyVelocity(IReadOnlyList<InvestorHolding> holdings)
    {
        var weekStart = DateTime.UtcNow.AddDays(-7);
        return holdings
            .Where(h => h.InvestedAt >= weekStart)
            .Sum(h => h.InvestedAmount);
    }

    private static int ComputeConfidenceRate(
        decimal raised,
        decimal goal,
        decimal threshold,
        DateTime? publishedAt,
        DateTime deadline)
    {
        if (goal <= 0)
        {
            return 0;
        }

        var goalProgress = (double)(raised / goal);
        var start = publishedAt ?? deadline.AddDays(-90);
        var totalDays = Math.Max((deadline - start).TotalDays, 1);
        var elapsedDays = Math.Clamp((DateTime.UtcNow - start).TotalDays, 0, totalDays);
        var timeProgress = elapsedDays / totalDays;
        var paceScore = timeProgress <= 0.01
            ? Math.Min(1, goalProgress)
            : Math.Min(1, goalProgress / timeProgress);

        var confidence = (int)Math.Round(Math.Clamp(paceScore * 100, 0, 100), MidpointRounding.AwayFromZero);
        if (raised >= threshold && threshold > 0)
        {
            confidence = Math.Min(100, confidence + 5);
        }

        return confidence;
    }

    private static IReadOnlyList<FundraiserBreakdownBucketDto> BuildSizeBuckets(
        IReadOnlyList<InvestorHolding> holdings,
        (string Label, decimal MinInclusive, decimal? MaxExclusive)[] sizeBuckets)
    {
        if (holdings.Count == 0)
        {
            return sizeBuckets
                .Select(b => new FundraiserBreakdownBucketDto(b.Label, 0))
                .ToList();
        }

        var total = holdings.Count;
        return sizeBuckets
            .Select(bucket =>
            {
                var count = holdings.Count(h =>
                    h.InvestedAmount >= bucket.MinInclusive
                    && (!bucket.MaxExclusive.HasValue || h.InvestedAmount < bucket.MaxExclusive.Value));
                var percentage = (int)Math.Round(count * 100m / total, MidpointRounding.AwayFromZero);
                return new FundraiserBreakdownBucketDto(bucket.Label, percentage);
            })
            .ToList();
    }

    private static IReadOnlyList<FundraiserMilestoneDto> BuildMilestones(
        InvestmentOffering offering,
        decimal raised,
        decimal goal,
        DateTime deadline)
    {
        var launchCompleted = offering.PublishedAt.HasValue;
        var launchDate = offering.PublishedAt ?? offering.CreatedAt;

        var pct25 = goal * 0.25m;
        var pct50 = goal * 0.50m;
        var pct75 = goal * 0.75m;
        var closed = offering.Status == OfferingStatus.Closed || DateTime.UtcNow >= deadline;

        var funded25 = ResolvePercentStatus(raised, pct25, launchCompleted);
        var funded50 = ResolvePercentStatus(raised, pct50, funded25 == "completed");
        var funded75 = ResolvePercentStatus(raised, pct75, funded50 == "completed");
        var closedStatus = closed ? "completed" : funded75 == "completed" ? "active" : "pending";

        return
        [
            new FundraiserMilestoneDto(
                "launch",
                "Launch",
                "Campaign published",
                FormatDateLabel(launchDate, completed: launchCompleted),
                launchCompleted ? "completed" : "pending"),
            new FundraiserMilestoneDto(
                "funded_25",
                "25% Funded",
                "Quarter of funding goal reached",
                TargetDateLabel(launchDate, deadline, 0.25),
                funded25),
            new FundraiserMilestoneDto(
                "funded_50",
                "50% Funded",
                "Halfway to funding goal",
                TargetDateLabel(launchDate, deadline, 0.50),
                funded50),
            new FundraiserMilestoneDto(
                "funded_75",
                "75% Funded",
                "Three-quarters of funding goal reached",
                TargetDateLabel(launchDate, deadline, 0.75),
                funded75),
            new FundraiserMilestoneDto(
                "campaign_closed",
                "Campaign closed",
                "Fundraising campaign ended",
                $"Target: {deadline:MMM d}",
                closedStatus),
        ];
    }

    private static string ResolvePercentStatus(decimal raised, decimal thresholdAmount, bool previousCompleted)
    {
        if (raised >= thresholdAmount)
        {
            return "completed";
        }

        return previousCompleted ? "active" : "pending";
    }

    private static string FormatDateLabel(DateTime date, bool completed) =>
        completed ? date.ToString("MMM d, yyyy") : $"Target: {date:MMM d}";

    private static string TargetDateLabel(DateTime campaignStart, DateTime deadline, double fractionAlongCampaign)
    {
        var totalDays = (deadline - campaignStart).TotalDays;
        if (totalDays <= 0)
        {
            return $"Target: {deadline:MMM d}";
        }

        var clampedFraction = Math.Clamp(fractionAlongCampaign, 0, 1);
        var target = campaignStart.AddDays(totalDays * clampedFraction);
        return $"Target: {target:MMM d}";
    }
}
