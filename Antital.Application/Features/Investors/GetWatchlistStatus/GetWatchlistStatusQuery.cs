using Antital.Application.DTOs.Investors;
using Antital.Domain.Interfaces;
using BuildingBlocks.Application.Features;

namespace Antital.Application.Features.Investors.GetWatchlistStatus;

public record GetWatchlistStatusQuery(int OfferingId) : ICommandQuery<WatchlistStatusResponse>;

public class GetWatchlistStatusQueryHandler(
    IInvestorUserAccess investorUserAccess,
    IInvestorWatchlistRepository watchlistRepository
) : ICommandQueryHandler<GetWatchlistStatusQuery, WatchlistStatusResponse>
{
    public async Task<Result<WatchlistStatusResponse>> Handle(
        GetWatchlistStatusQuery request,
        CancellationToken cancellationToken)
    {
        var (userId, _) = await investorUserAccess.RequireAuthenticatedUserAsync(cancellationToken);

        var isWatchlisted = await watchlistRepository.IsWatchlistedAsync(userId, request.OfferingId, cancellationToken);
        var response = new WatchlistStatusResponse(isWatchlisted);
        var result = new Result<WatchlistStatusResponse>();
        result.AddValue(response);
        result.OK();
        return result;
    }
}
