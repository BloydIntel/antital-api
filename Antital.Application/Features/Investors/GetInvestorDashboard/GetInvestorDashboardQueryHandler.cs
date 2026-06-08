using Antital.Application.DTOs.Investors;
using Antital.Domain.Interfaces;
using BuildingBlocks.Application.Features;

namespace Antital.Application.Features.Investors.GetInvestorDashboard;

public class GetInvestorDashboardQueryHandler(
    IInvestorUserAccess investorUserAccess,
    IInvestorDashboardRepository dashboardRepository
) : ICommandQueryHandler<GetInvestorDashboardQuery, InvestorDashboardResponse>
{
    public async Task<Result<InvestorDashboardResponse>> Handle(
        GetInvestorDashboardQuery request,
        CancellationToken cancellationToken)
    {
        if (!DashboardPeriodResolver.TryResolve(request.Period, out var periodRange, out var periodError))
        {
            var invalidResult = new Result<InvestorDashboardResponse>();
            invalidResult.BadRequest(
                "Invalid period.",
                new Dictionary<string, string[]>
                {
                    ["period"] = [periodError ?? "Period must be this-month, last-month, last-3-months, last-6-months, or last-12-months."],
                });
            return invalidResult;
        }

        var (userId, _) = await investorUserAccess.RequireAuthenticatedUserAsync(cancellationToken);

        var wallet = await dashboardRepository.GetWalletAsync(userId, cancellationToken);
        var holdings = await dashboardRepository.GetHoldingsAsync(
            userId,
            periodRange.StartUtc,
            periodRange.EndUtc,
            cancellationToken);
        var watchlist = await dashboardRepository.GetWatchlistAsync(userId, cancellationToken);
        var performancePoints = await dashboardRepository.GetPerformancePointsAsync(
            userId,
            periodRange.StartUtc,
            periodRange.EndUtc,
            cancellationToken);

        var totalInvested = holdings.Sum(h => h.InvestedAmount);
        var totalReturns = holdings.Sum(h => h.Returns);

        var response = new InvestorDashboardResponse(
            new InvestorDashboardSummaryDto(
                wallet?.AvailableBalance ?? 0m,
                totalInvested,
                totalReturns,
                wallet?.Currency ?? "NGN"),
            performancePoints
                .Select(p => new InvestorDashboardPerformancePointDto(
                    DashboardPeriodResolver.ToPeriodLabel(p.Year, p.Month),
                    p.Value))
                .ToList(),
            watchlist.Select(InvestorDashboardMappers.ToActiveDeal).ToList(),
            holdings.Select(InvestorDashboardMappers.ToHolding).ToList());

        var result = new Result<InvestorDashboardResponse>();
        result.AddValue(response);
        result.OK();
        return result;
    }
}
