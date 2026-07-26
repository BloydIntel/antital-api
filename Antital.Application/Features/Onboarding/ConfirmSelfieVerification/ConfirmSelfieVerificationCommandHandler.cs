using Antital.Application.Features.Onboarding;
using Antital.Domain.Configuration;
using Antital.Domain.Interfaces;
using Antital.Domain.Models;
using BuildingBlocks.Application.Exceptions;
using BuildingBlocks.Application.Features;
using Microsoft.Extensions.Options;

namespace Antital.Application.Features.Onboarding.ConfirmSelfieVerification;

public record ConfirmSelfieVerificationCommand(string ReferenceId)
    : ICommandQuery<ConfirmSelfieVerificationResponse>;

public record ConfirmSelfieVerificationResponse(
    string ReferenceId,
    bool SelfieCompleted,
    string? SelfieUrl
);

public class ConfirmSelfieVerificationCommandHandler(
    IOnboardingUserAccess userAccess,
    IUserKycRepository userKycRepository,
    IDojahClient dojahClient,
    IOptions<DojahSettings> dojahOptions,
    IAntitalUnitOfWork unitOfWork
) : ICommandQueryHandler<ConfirmSelfieVerificationCommand, ConfirmSelfieVerificationResponse>
{
    public async Task<Result<ConfirmSelfieVerificationResponse>> Handle(
        ConfirmSelfieVerificationCommand request,
        CancellationToken cancellationToken)
    {
        if (!dojahOptions.Value.Enabled)
        {
            throw new BadRequestException(
                "Dojah verification is not enabled.",
                new Dictionary<string, string[]>());
        }

        if (string.IsNullOrWhiteSpace(request.ReferenceId) || request.ReferenceId.Trim().Length <= 10)
        {
            throw new BadRequestException(
                "Invalid selfie verification reference.",
                new Dictionary<string, string[]>
                {
                    ["referenceId"] = ["Reference id must be longer than 10 characters."],
                });
        }

        var (userId, _) = await userAccess.RequireVerifiedUserAsync(cancellationToken);
        var referenceId = request.ReferenceId.Trim();

        var verification = await dojahClient.GetWidgetVerificationAsync(referenceId, cancellationToken);
        if (!verification.IsSuccess || !verification.SelfiePassed)
        {
            throw new BadRequestException(
                "Selfie verification failed.",
                new Dictionary<string, string[]>
                {
                    ["selfie"] =
                    [
                        verification.ErrorMessage
                        ?? "Dojah did not confirm a successful selfie/liveness check.",
                    ],
                });
        }

        var storedReference = verification.ReferenceId ?? referenceId;
        var kyc = await userKycRepository.GetByUserIdAsync(userId, cancellationToken);
        var isNew = kyc == null;
        kyc ??= new UserKyc { UserId = userId, IdType = Domain.Enums.KycIdType.NationalIdCard };
        kyc.SelfieVerificationPathOrKey = storedReference;
        kyc.SelfieVerifiedAt = DateTime.UtcNow;

        if (isNew)
        {
            await userKycRepository.AddAsync(kyc, cancellationToken);
        }
        else
        {
            await userKycRepository.UpdateAsync(kyc, cancellationToken);
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);

        var result = new Result<ConfirmSelfieVerificationResponse>();
        result.AddValue(new ConfirmSelfieVerificationResponse(storedReference, true, verification.SelfieUrl));
        result.OK();
        return result;
    }
}
