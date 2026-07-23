using Antital.Application.DTOs.Fundraisers;
using Antital.Domain.Interfaces;
using BuildingBlocks.Application.Features;

namespace Antital.Application.Features.Fundraisers.GetFundraiserCampaign;

public record GetFundraiserCampaignQuery : ICommandQuery<FundraiserCampaignResponse>;

public class GetFundraiserCampaignQueryHandler(
    IFundraiserUserAccess fundraiserUserAccess,
    IFundraiserDashboardRepository dashboardRepository
) : ICommandQueryHandler<GetFundraiserCampaignQuery, FundraiserCampaignResponse>
{
    public async Task<Result<FundraiserCampaignResponse>> Handle(
        GetFundraiserCampaignQuery request,
        CancellationToken cancellationToken)
    {
        var (userId, _) = await fundraiserUserAccess.RequireFundraiserAsync(cancellationToken);
        var offering = await dashboardRepository.GetPrimaryOfferingAsync(userId, cancellationToken);

        FundraiserCampaignResponse response;
        if (offering == null)
        {
            response = new FundraiserCampaignResponse(null, null, null, null, null);
        }
        else
        {
            response = new FundraiserCampaignResponse(
                offering.Id,
                offering.Slug,
                offering.Name,
                offering.Status.ToString().ToLowerInvariant(),
                $"/explore/{offering.Slug}");
        }

        var result = new Result<FundraiserCampaignResponse>();
        result.AddValue(response);
        result.OK();
        return result;
    }
}
