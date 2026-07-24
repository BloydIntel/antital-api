using Antital.Application.DTOs.Fundraisers;
using Antital.Application.Features.Fundraisers.Settings;
using Antital.Domain.Interfaces;
using Antital.Domain.Models;
using BuildingBlocks.Application.Features;
using BuildingBlocks.Domain.Interfaces;

namespace Antital.Application.Features.Fundraisers.Settings.UpdateFundraiserNotificationPreferences;

public record UpdateFundraiserNotificationPreferencesCommand(
    FundraiserEmailNotificationPrefsDto Email,
    FundraiserInAppNotificationPrefsDto InApp,
    FundraiserMarketingNotificationPrefsDto Marketing
) : ICommandQuery<FundraiserNotificationPreferencesResponse>;

public class UpdateFundraiserNotificationPreferencesCommandHandler(
    IFundraiserUserAccess fundraiserUserAccess,
    IFundraiserNotificationPreferencesRepository preferencesRepository,
    IAntitalUnitOfWork unitOfWork,
    ICurrentUser currentUser
) : ICommandQueryHandler<UpdateFundraiserNotificationPreferencesCommand, FundraiserNotificationPreferencesResponse>
{
    public async Task<Result<FundraiserNotificationPreferencesResponse>> Handle(
        UpdateFundraiserNotificationPreferencesCommand request,
        CancellationToken cancellationToken)
    {
        var (userId, _) = await fundraiserUserAccess.RequireFundraiserAsync(cancellationToken);

        var prefs = await preferencesRepository.GetByUserIdAsync(userId, cancellationToken);
        var isNew = prefs is null;
        prefs ??= new FundraiserNotificationPreferences { UserId = userId };

        var updateRequest = new UpdateFundraiserNotificationPreferencesRequest(
            request.Email,
            request.InApp,
            request.Marketing);

        FundraiserNotificationPreferencesMapper.ApplyUpdate(prefs, updateRequest);

        var actor = !string.IsNullOrEmpty(currentUser.UserName)
            ? currentUser.UserName
            : (!string.IsNullOrEmpty(currentUser.IPAddress) ? currentUser.IPAddress : "System");

        if (isNew)
        {
            prefs.Created(actor);
            await preferencesRepository.AddAsync(prefs, cancellationToken);
        }
        else
        {
            prefs.Updated(actor);
            await preferencesRepository.UpdateAsync(prefs, cancellationToken);
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);

        var refreshed = await preferencesRepository.GetByUserIdAsync(userId, cancellationToken);
        var response = FundraiserNotificationPreferencesMapper.ToResponse(refreshed);

        var result = new Result<FundraiserNotificationPreferencesResponse>();
        result.AddValue(response);
        result.OK();
        return result;
    }
}
