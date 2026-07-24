using Antital.Application.DTOs.Fundraisers;
using Antital.Application.Features.Fundraisers.Settings;
using Antital.Domain.Interfaces;
using BuildingBlocks.Application.Features;

namespace Antital.Application.Features.Fundraisers.Settings.GetFundraiserSettingsProfile;

public record GetFundraiserSettingsProfileQuery : ICommandQuery<FundraiserSettingsProfileResponse>;

public class GetFundraiserSettingsProfileQueryHandler(
    IFundraiserUserAccess fundraiserUserAccess,
    IUserInvestmentProfileRepository profileRepository
) : ICommandQueryHandler<GetFundraiserSettingsProfileQuery, FundraiserSettingsProfileResponse>
{
    public async Task<Result<FundraiserSettingsProfileResponse>> Handle(
        GetFundraiserSettingsProfileQuery request,
        CancellationToken cancellationToken)
    {
        var (userId, _) = await fundraiserUserAccess.RequireFundraiserAsync(cancellationToken);
        var profile = await profileRepository.GetByUserIdAsync(userId, cancellationToken);

        var response = FundraiserSettingsProfileMapper.ToResponse(profile);
        var result = new Result<FundraiserSettingsProfileResponse>();
        result.AddValue(response);
        result.OK();
        return result;
    }
}
