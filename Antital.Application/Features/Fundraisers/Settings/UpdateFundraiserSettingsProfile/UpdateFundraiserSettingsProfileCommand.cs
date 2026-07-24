using Antital.Application.DTOs.Fundraisers;
using Antital.Application.Features.Fundraisers.Settings;
using Antital.Domain.Interfaces;
using Antital.Domain.Models;
using BuildingBlocks.Application.Features;

namespace Antital.Application.Features.Fundraisers.Settings.UpdateFundraiserSettingsProfile;

public record UpdateFundraiserSettingsProfileCommand(
    string CompanyName,
    string? RegistrationNumber,
    string? Bio,
    string? Website,
    string? PublicEmail,
    string? Headquarters,
    UpdateFundraiserSettingsContactRequest? Contact
) : ICommandQuery<FundraiserSettingsProfileResponse>;

public class UpdateFundraiserSettingsProfileCommandHandler(
    IFundraiserUserAccess fundraiserUserAccess,
    IUserInvestmentProfileRepository profileRepository,
    IAntitalUnitOfWork unitOfWork
) : ICommandQueryHandler<UpdateFundraiserSettingsProfileCommand, FundraiserSettingsProfileResponse>
{
    public async Task<Result<FundraiserSettingsProfileResponse>> Handle(
        UpdateFundraiserSettingsProfileCommand request,
        CancellationToken cancellationToken)
    {
        var (userId, _) = await fundraiserUserAccess.RequireFundraiserAsync(cancellationToken);

        var profile = await profileRepository.GetByUserIdAsync(userId, cancellationToken);
        var isNew = profile is null;
        profile ??= new UserInvestmentProfile { UserId = userId };

        var updateRequest = new UpdateFundraiserSettingsProfileRequest(
            request.CompanyName,
            request.RegistrationNumber,
            request.Bio,
            request.Website,
            request.PublicEmail,
            request.Headquarters,
            request.Contact);

        FundraiserSettingsProfileMapper.ApplyUpdate(profile, updateRequest);

        if (isNew)
        {
            await profileRepository.AddAsync(profile, cancellationToken);
        }
        else
        {
            await profileRepository.UpdateAsync(profile, cancellationToken);
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);

        var refreshed = await profileRepository.GetByUserIdAsync(userId, cancellationToken);
        var response = FundraiserSettingsProfileMapper.ToResponse(refreshed);

        var result = new Result<FundraiserSettingsProfileResponse>();
        result.AddValue(response);
        result.OK();
        return result;
    }
}
