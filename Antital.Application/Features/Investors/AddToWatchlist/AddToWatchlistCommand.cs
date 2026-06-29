using Antital.Application.DTOs.Investors;
using Antital.Application.Features.Investors.Watchlist;
using Antital.Domain.Interfaces;
using Antital.Domain.Models;
using BuildingBlocks.Application.Exceptions;
using BuildingBlocks.Application.Features;
using BuildingBlocks.Domain.Interfaces;

namespace Antital.Application.Features.Investors.AddToWatchlist;

public record AddToWatchlistCommand(int OfferingId) : ICommandQuery<WatchlistItemDto>;

public class AddToWatchlistCommandHandler(
    IInvestorUserAccess investorUserAccess,
    IInvestorWatchlistRepository watchlistRepository,
    IInvestmentOfferingRepository offeringRepository,
    IAntitalUnitOfWork unitOfWork,
    ICurrentUser currentUser
) : ICommandQueryHandler<AddToWatchlistCommand, WatchlistItemDto>
{
    public async Task<Result<WatchlistItemDto>> Handle(
        AddToWatchlistCommand request,
        CancellationToken cancellationToken)
    {
        var (userId, _) = await investorUserAccess.RequireAuthenticatedUserAsync(cancellationToken);

        var offering = await offeringRepository.GetPublishedShellByIdAsync(request.OfferingId, cancellationToken);
        if (offering == null)
        {
            throw new NotFoundException("Investment offering not found.");
        }

        var active = await watchlistRepository.GetActiveByUserAndOfferingAsync(userId, request.OfferingId, cancellationToken);
        if (active != null)
        {
            throw new ConflictException("Offering is already on your watchlist.");
        }

        var actor = ResolveActor();
        var now = DateTime.UtcNow;

        var item = new InvestorWatchlistItem
        {
            UserId = userId,
            OfferingId = request.OfferingId,
            ChangePercent = 0m,
            AddedAt = now,
        };
        item.Created(actor);
        await watchlistRepository.AddAsync(item, cancellationToken);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        var saved = await watchlistRepository.GetActiveByUserAndOfferingAsync(userId, request.OfferingId, cancellationToken);
        var latestUpdates = await watchlistRepository.GetLatestUpdatesByOfferingIdsAsync(
            [request.OfferingId],
            cancellationToken);
        latestUpdates.TryGetValue(request.OfferingId, out var update);

        var dto = WatchlistMapper.ToItem(saved!, update);
        var result = new Result<WatchlistItemDto>();
        result.AddValue(dto);
        result.OK();
        return result;
    }

    private string ResolveActor() =>
        !string.IsNullOrEmpty(currentUser.UserName)
            ? currentUser.UserName
            : (!string.IsNullOrEmpty(currentUser.IPAddress) ? currentUser.IPAddress : "System");
}
