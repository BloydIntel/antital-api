using Antital.Domain.Interfaces;
using BuildingBlocks.Application.Exceptions;
using BuildingBlocks.Application.Features;
using BuildingBlocks.Domain.Interfaces;

namespace Antital.Application.Features.Investors.RemoveFromWatchlist;

public record RemoveFromWatchlistCommand(int OfferingId) : ICommandQuery;

public class RemoveFromWatchlistCommandHandler(
    IInvestorUserAccess investorUserAccess,
    IInvestorWatchlistRepository watchlistRepository,
    IAntitalUnitOfWork unitOfWork,
    ICurrentUser currentUser
) : ICommandQueryHandler<RemoveFromWatchlistCommand>
{
    public async Task<Result> Handle(RemoveFromWatchlistCommand request, CancellationToken cancellationToken)
    {
        var (userId, _) = await investorUserAccess.RequireAuthenticatedUserAsync(cancellationToken);

        var item = await watchlistRepository.GetActiveByUserAndOfferingAsync(userId, request.OfferingId, cancellationToken);
        if (item == null)
        {
            throw new NotFoundException("Watchlist item not found.");
        }

        item.Deleted(ResolveActor());
        await watchlistRepository.UpdateAsync(item, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        var result = new Result();
        result.OK();
        return result;
    }

    private string ResolveActor() =>
        !string.IsNullOrEmpty(currentUser.UserName)
            ? currentUser.UserName
            : (!string.IsNullOrEmpty(currentUser.IPAddress) ? currentUser.IPAddress : "System");
}
