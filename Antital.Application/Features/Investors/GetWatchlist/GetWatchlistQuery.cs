using Antital.Application.DTOs.Investors;
using Antital.Application.Features.Investors.Watchlist;
using Antital.Domain.Interfaces;
using BuildingBlocks.Application.Features;

namespace Antital.Application.Features.Investors.GetWatchlist;

public record GetWatchlistQuery : ICommandQuery<WatchlistResponse>;

public class GetWatchlistQueryHandler(
    IInvestorUserAccess investorUserAccess,
    IInvestorWatchlistRepository watchlistRepository
) : ICommandQueryHandler<GetWatchlistQuery, WatchlistResponse>
{
    public async Task<Result<WatchlistResponse>> Handle(
        GetWatchlistQuery request,
        CancellationToken cancellationToken)
    {
        var (userId, _) = await investorUserAccess.RequireAuthenticatedUserAsync(cancellationToken);

        var items = await watchlistRepository.ListByUserAsync(userId, cancellationToken);
        var offeringIds = items.Select(i => i.OfferingId).ToList();
        var latestUpdates = await watchlistRepository.GetLatestUpdatesByOfferingIdsAsync(offeringIds, cancellationToken);

        var mapped = items
            .Select(item =>
            {
                latestUpdates.TryGetValue(item.OfferingId, out var update);
                return WatchlistMapper.ToItem(item, update);
            })
            .ToList();

        var response = new WatchlistResponse(mapped, WatchlistMapper.ToCounts(mapped));
        var result = new Result<WatchlistResponse>();
        result.AddValue(response);
        result.OK();
        return result;
    }
}
