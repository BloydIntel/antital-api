using Antital.Application.DTOs.Investors;
using Antital.Application.Features.Investors.Profile;
using Antital.Domain.Interfaces;
using BuildingBlocks.Application.Features;
using BuildingBlocks.Domain.Interfaces;

namespace Antital.Application.Features.Investors.UpdateInvestorProfile;

public record UpdateInvestorProfileCommand(
    string FirstName,
    string LastName,
    string? PreferredName,
    string PhoneNumber,
    string ResidentialAddress,
    string StateOfResidence,
    string CountryOfResidence
) : ICommandQuery<InvestorProfileResponse>;

public class UpdateInvestorProfileCommandHandler(
    IInvestorUserAccess investorUserAccess,
    IUserRepository userRepository,
    IAntitalUnitOfWork unitOfWork,
    ICurrentUser currentUser
) : ICommandQueryHandler<UpdateInvestorProfileCommand, InvestorProfileResponse>
{
    public async Task<Result<InvestorProfileResponse>> Handle(
        UpdateInvestorProfileCommand request,
        CancellationToken cancellationToken)
    {
        var (userId, user) = await investorUserAccess.RequireAuthenticatedUserAsync(cancellationToken);

        var updateRequest = new UpdateInvestorProfileRequest(
            request.FirstName,
            request.LastName,
            request.PreferredName,
            request.PhoneNumber,
            request.ResidentialAddress,
            request.StateOfResidence,
            request.CountryOfResidence);

        InvestorProfileMapper.ApplyUpdate(user, updateRequest);

        var actor = !string.IsNullOrEmpty(currentUser.UserName)
            ? currentUser.UserName
            : (!string.IsNullOrEmpty(currentUser.IPAddress) ? currentUser.IPAddress : "System");
        user.Updated(actor);

        await userRepository.UpdateAsync(user, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        var refreshed = await userRepository.GetByIdAsync(userId, cancellationToken);
        var response = InvestorProfileMapper.ToResponse(refreshed!);

        var result = new Result<InvestorProfileResponse>();
        result.AddValue(response);
        result.OK();
        return result;
    }
}
