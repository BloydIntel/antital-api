using Antital.Application.DTOs.Onboarding;
using Antital.Domain.Configuration;
using Antital.Domain.Enums;
using Antital.Domain.Interfaces;
using BuildingBlocks.Application.Exceptions;
using BuildingBlocks.Application.Features;
using Microsoft.Extensions.Options;

namespace Antital.Application.Features.Onboarding.GetApplicationFee;

public class GetApplicationFeeQueryHandler(
    IOnboardingUserAccess onboardingUserAccess,
    IUserInvestmentProfileRepository profileRepository,
    IOptions<PaystackSettings> paystackOptions
) : ICommandQueryHandler<GetApplicationFeeQuery, ApplicationFeeStatusResponse>
{
    public async Task<Result<ApplicationFeeStatusResponse>> Handle(
        GetApplicationFeeQuery request,
        CancellationToken cancellationToken)
    {
        var (userId, user) = await onboardingUserAccess.RequireVerifiedUserAsync(cancellationToken);
        EnsureFundRaiser(user.UserType);

        var settings = paystackOptions.Value;
        var profile = await profileRepository.GetByUserIdAsync(userId, cancellationToken);

        var response = new ApplicationFeeStatusResponse(
            settings.ApplicationFeeAmountNgn,
            "NGN",
            profile?.FundRaiserApplicationFeePaid == true,
            profile?.FundRaiserPaymentMethod,
            profile?.FundRaiserPaymentReference,
            profile?.FundRaiserPaymentStatus);

        var result = new Result<ApplicationFeeStatusResponse>();
        result.AddValue(response);
        result.OK();
        return result;
    }

    private static void EnsureFundRaiser(UserTypeEnum userType)
    {
        if (userType != UserTypeEnum.FundRaiser)
        {
            throw new ForbiddenException("Only fundraisers can access application fee payment.");
        }
    }
}
