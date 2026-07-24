using Antital.Application.DTOs.Fundraisers;
using Antital.Application.Features.Fundraisers.Settings;
using Antital.Domain.Interfaces;
using BuildingBlocks.Application.Features;

namespace Antital.Application.Features.Fundraisers.Settings.GetFundraiserNotificationPreferences;

public record GetFundraiserNotificationPreferencesQuery
    : ICommandQuery<FundraiserNotificationPreferencesResponse>;

public class GetFundraiserNotificationPreferencesQueryHandler(
    IFundraiserUserAccess fundraiserUserAccess,
    IFundraiserNotificationPreferencesRepository preferencesRepository
) : ICommandQueryHandler<GetFundraiserNotificationPreferencesQuery, FundraiserNotificationPreferencesResponse>
{
    public async Task<Result<FundraiserNotificationPreferencesResponse>> Handle(
        GetFundraiserNotificationPreferencesQuery request,
        CancellationToken cancellationToken)
    {
        var (userId, _) = await fundraiserUserAccess.RequireFundraiserAsync(cancellationToken);
        var prefs = await preferencesRepository.GetByUserIdAsync(userId, cancellationToken);

        var response = FundraiserNotificationPreferencesMapper.ToResponse(prefs);
        var result = new Result<FundraiserNotificationPreferencesResponse>();
        result.AddValue(response);
        result.OK();
        return result;
    }
}
