using System.Text.Json;
using Antital.Application.DTOs.Onboarding;
using Antital.Application.Features.Investments.Paystack;
using Antital.Application.Features.Onboarding.ApplicationFeePayment;
using Antital.Domain.Configuration;
using Antital.Domain.Enums;
using Antital.Domain.Interfaces;
using BuildingBlocks.Application.Exceptions;
using BuildingBlocks.Application.Features;
using Microsoft.Extensions.Options;

namespace Antital.Application.Features.Onboarding.VerifyApplicationFeePayment;

/// <summary>
/// Confirms application-fee payment via Paystack verify API (local-dev friendly when webhooks cannot reach localhost).
/// </summary>
public class VerifyApplicationFeePaymentCommandHandler(
    IOnboardingUserAccess onboardingUserAccess,
    IUserInvestmentProfileRepository profileRepository,
    IPaystackClient paystackClient,
    IApplicationFeePaymentConfirmationService paymentConfirmationService,
    IOptions<PaystackSettings> paystackOptions
) : ICommandQueryHandler<VerifyApplicationFeePaymentCommand, ApplicationFeeStatusResponse>
{
    public async Task<Result<ApplicationFeeStatusResponse>> Handle(
        VerifyApplicationFeePaymentCommand request,
        CancellationToken cancellationToken)
    {
        var (userId, user) = await onboardingUserAccess.RequireVerifiedUserAsync(cancellationToken);
        if (user.UserType != UserTypeEnum.FundRaiser)
        {
            throw new ForbiddenException("Only fundraisers can verify application fee payment.");
        }

        var settings = paystackOptions.Value;
        var profile = await profileRepository.GetByUserIdAsync(userId, cancellationToken);
        if (profile?.FundRaiserApplicationFeePaid == true)
        {
            return Success(settings, profile);
        }

        var reference = !string.IsNullOrWhiteSpace(request.Reference)
            ? request.Reference.Trim()
            : profile?.FundRaiserPaymentReference;

        if (string.IsNullOrWhiteSpace(reference))
        {
            throw new BadRequestException(
                "Payment has not been initialized.",
                new Dictionary<string, string[]> { ["payment"] = ["Missing Paystack reference."] });
        }

        if (!PaystackReferenceGenerator.TryParseApplicationFeeUserId(reference, out var referenceUserId)
            || referenceUserId != userId)
        {
            throw new BadRequestException(
                "Invalid payment reference.",
                new Dictionary<string, string[]> { ["payment"] = ["Reference does not belong to this user."] });
        }

        var verifyResult = await paystackClient.VerifyTransactionAsync(reference, cancellationToken);
        if (!verifyResult.Success)
        {
            throw new BadRequestException(
                verifyResult.Message ?? "Payment is not complete yet.",
                new Dictionary<string, string[]> { ["payment"] = ["Paystack verification failed."] });
        }

        var rawPayload = JsonSerializer.Serialize(new
        {
            @event = "charge.success",
            data = new
            {
                reference,
                amount = verifyResult.AmountKobo,
                channel = verifyResult.Channel,
                status = verifyResult.Status,
            },
        });

        await paymentConfirmationService.TryConfirmSuccessfulChargeAsync(
            reference,
            verifyResult.AmountKobo,
            verifyResult.Channel,
            rawPayload,
            cancellationToken);

        var updated = await profileRepository.GetByUserIdAsync(userId, cancellationToken)
            ?? throw new NotFoundException("Investment profile not found.");

        if (updated.FundRaiserApplicationFeePaid != true)
        {
            throw new BadRequestException(
                "Payment could not be confirmed.",
                new Dictionary<string, string[]> { ["payment"] = ["Application fee is still unpaid."] });
        }

        return Success(settings, updated);
    }

    private static Result<ApplicationFeeStatusResponse> Success(
        PaystackSettings settings,
        Domain.Models.UserInvestmentProfile profile)
    {
        var response = new ApplicationFeeStatusResponse(
            settings.ApplicationFeeAmountNgn,
            "NGN",
            profile.FundRaiserApplicationFeePaid == true,
            profile.FundRaiserPaymentMethod,
            profile.FundRaiserPaymentReference,
            profile.FundRaiserPaymentStatus);

        var result = new Result<ApplicationFeeStatusResponse>();
        result.AddValue(response);
        result.OK();
        return result;
    }
}
