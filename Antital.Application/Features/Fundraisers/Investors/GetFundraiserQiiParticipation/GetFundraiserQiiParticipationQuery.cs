using Antital.Application.DTOs.Fundraisers;
using Antital.Domain.Interfaces;
using BuildingBlocks.Application.Features;

namespace Antital.Application.Features.Fundraisers.Investors.GetFundraiserQiiParticipation;

public record GetFundraiserQiiParticipationQuery : ICommandQuery<FundraiserQiiParticipationResponse>;

public class GetFundraiserQiiParticipationQueryHandler(
    IFundraiserUserAccess fundraiserUserAccess,
    IFundraiserDashboardRepository dashboardRepository,
    IFundraiserQiiParticipationRepository qiiRepository
) : ICommandQueryHandler<GetFundraiserQiiParticipationQuery, FundraiserQiiParticipationResponse>
{
    public async Task<Result<FundraiserQiiParticipationResponse>> Handle(
        GetFundraiserQiiParticipationQuery request,
        CancellationToken cancellationToken)
    {
        var (userId, _) = await fundraiserUserAccess.RequireFundraiserAsync(cancellationToken);
        var offering = await dashboardRepository.GetPrimaryOfferingAsync(userId, cancellationToken);

        if (offering == null)
        {
            var empty = new Result<FundraiserQiiParticipationResponse>();
            empty.AddValue(new FundraiserQiiParticipationResponse(null, []));
            empty.OK();
            return empty;
        }

        var holdings = await qiiRepository.ListQiiHoldingsAsync(offering.Id, cancellationToken);
        var pendingOrders = await qiiRepository.ListQiiPendingOrdersAsync(offering.Id, cancellationToken);
        var userIds = holdings.Select(h => h.UserId)
            .Concat(pendingOrders.Select(o => o.UserId))
            .Distinct()
            .ToList();
        var profiles = await qiiRepository.GetProfilesByUserIdsAsync(userIds, cancellationToken);
        var items = FundraiserQiiParticipationMappers.BuildItems(holdings, pendingOrders, profiles);

        var result = new Result<FundraiserQiiParticipationResponse>();
        result.AddValue(new FundraiserQiiParticipationResponse(offering.Id, items));
        result.OK();
        return result;
    }
}
