using Antital.Application.DTOs.Investors;
using Antital.Application.Features.Investors.Profile;
using BuildingBlocks.Application.Features;

namespace Antital.Application.Features.Investors.GetInvestorProfile;

public record GetInvestorProfileQuery : ICommandQuery<InvestorProfileResponse>;

public class GetInvestorProfileQueryHandler(
    IInvestorUserAccess investorUserAccess
) : ICommandQueryHandler<GetInvestorProfileQuery, InvestorProfileResponse>
{
    public async Task<Result<InvestorProfileResponse>> Handle(
        GetInvestorProfileQuery request,
        CancellationToken cancellationToken)
    {
        var (_, user) = await investorUserAccess.RequireAuthenticatedUserAsync(cancellationToken);

        var response = InvestorProfileMapper.ToResponse(user);
        var result = new Result<InvestorProfileResponse>();
        result.AddValue(response);
        result.OK();
        return result;
    }
}
