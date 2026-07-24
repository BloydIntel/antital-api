using Antital.Application.DTOs.Onboarding;
using Antital.Application.Features.Investments.Paystack;
using Antital.Domain.Configuration;
using Antital.Domain.Enums;
using Antital.Domain.Integrations.Paystack;
using Antital.Domain.Interfaces;
using Antital.Domain.Models;
using BuildingBlocks.Application.Exceptions;
using BuildingBlocks.Application.Features;
using BuildingBlocks.Domain.Interfaces;
using Microsoft.Extensions.Options;

namespace Antital.Application.Features.Onboarding.InitializeApplicationFeePayment;

public class InitializeApplicationFeePaymentCommandHandler(
    IOnboardingUserAccess onboardingUserAccess,
    IUserInvestmentProfileRepository profileRepository,
    IPaystackClient paystackClient,
    IAntitalUnitOfWork unitOfWork,
    ICurrentUser currentUser,
    IOptions<PaystackSettings> paystackOptions
) : ICommandQueryHandler<InitializeApplicationFeePaymentCommand, InitializeApplicationFeePaymentResponse>
{
    public async Task<Result<InitializeApplicationFeePaymentResponse>> Handle(
        InitializeApplicationFeePaymentCommand request,
        CancellationToken cancellationToken)
    {
        var (userId, user) = await onboardingUserAccess.RequireVerifiedUserAsync(cancellationToken);
        if (user.UserType != UserTypeEnum.FundRaiser)
        {
            throw new ForbiddenException("Only fundraisers can pay the application fee.");
        }

        var settings = paystackOptions.Value;
        if (string.IsNullOrWhiteSpace(settings.SecretKey)
            || string.IsNullOrWhiteSpace(settings.ApplicationFeeCallbackUrl))
        {
            throw new BadRequestException(
                "Payment is not configured.",
                new Dictionary<string, string[]> { ["payment"] = ["Paystack application fee is not configured."] });
        }

        if (settings.ApplicationFeeAmountNgn <= 0)
        {
            throw new BadRequestException(
                "Application fee amount is not configured.",
                new Dictionary<string, string[]> { ["payment"] = ["Invalid application fee amount."] });
        }

        var profile = await profileRepository.GetByUserIdAsync(userId, cancellationToken);
        if (profile?.FundRaiserApplicationFeePaid == true)
        {
            throw new BadRequestException(
                "Application fee already paid.",
                new Dictionary<string, string[]> { ["payment"] = ["Application fee already paid."] });
        }

        var reference = PaystackReferenceGenerator.CreateForApplicationFee(userId);
        var amountKobo = PaystackAmountConverter.ToKobo(settings.ApplicationFeeAmountNgn);
        var initializeResult = await paystackClient.InitializeTransactionAsync(
            new PaystackInitializeRequest(
                user.Email,
                amountKobo,
                reference,
                settings.ApplicationFeeCallbackUrl,
                PaystackChannelMapper.ToPaystackChannels(request.Channel)),
            cancellationToken);

        if (!initializeResult.Success
            || string.IsNullOrWhiteSpace(initializeResult.AuthorizationUrl)
            || string.IsNullOrWhiteSpace(initializeResult.AccessCode)
            || string.IsNullOrWhiteSpace(initializeResult.Reference))
        {
            throw new BadRequestException(
                initializeResult.Message ?? "Unable to initialize payment.",
                new Dictionary<string, string[]> { ["payment"] = ["Paystack initialization failed."] });
        }

        var isNew = profile == null;
        profile ??= new UserInvestmentProfile { UserId = userId };

        var actor = ResolveActor();
        profile.FundRaiserPaymentMethod = request.Channel.ToString().ToLowerInvariant();
        profile.FundRaiserPaymentReference = initializeResult.Reference;
        profile.FundRaiserPaymentStatus = "Pending";
        profile.FundRaiserApplicationFeePaid = false;

        if (isNew)
        {
            profile.Created(actor);
            await profileRepository.AddAsync(profile, cancellationToken);
        }
        else
        {
            profile.Updated(actor);
            await profileRepository.UpdateAsync(profile, cancellationToken);
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);

        var response = new InitializeApplicationFeePaymentResponse(
            initializeResult.AuthorizationUrl,
            initializeResult.AccessCode,
            initializeResult.Reference,
            settings.PublicKey,
            settings.ApplicationFeeAmountNgn,
            "NGN");

        var result = new Result<InitializeApplicationFeePaymentResponse>();
        result.AddValue(response);
        result.OK();
        return result;
    }

    private string ResolveActor() =>
        !string.IsNullOrEmpty(currentUser.UserName)
            ? currentUser.UserName
            : (!string.IsNullOrEmpty(currentUser.IPAddress) ? currentUser.IPAddress : "System");
}
