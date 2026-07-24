using Antital.Application.Features.Investments.Paystack;
using Antital.Domain.Configuration;
using Antital.Domain.Interfaces;
using BuildingBlocks.Domain.Interfaces;
using Microsoft.Extensions.Options;

namespace Antital.Application.Features.Onboarding.ApplicationFeePayment;

public interface IApplicationFeePaymentConfirmationService
{
    Task<bool> TryConfirmSuccessfulChargeAsync(
        string reference,
        int amountKobo,
        string? channel,
        string rawPayload,
        CancellationToken cancellationToken = default);

    Task<bool> TryMarkFailedChargeAsync(
        string reference,
        string rawPayload,
        CancellationToken cancellationToken = default);
}

public class ApplicationFeePaymentConfirmationService(
    IUserInvestmentProfileRepository profileRepository,
    IAntitalUnitOfWork unitOfWork,
    ICurrentUser currentUser,
    IOptions<PaystackSettings> paystackOptions
) : IApplicationFeePaymentConfirmationService
{
    public async Task<bool> TryConfirmSuccessfulChargeAsync(
        string reference,
        int amountKobo,
        string? channel,
        string rawPayload,
        CancellationToken cancellationToken = default)
    {
        if (!PaystackReferenceGenerator.TryParseApplicationFeeUserId(reference, out var userId))
        {
            return false;
        }

        var profile = await profileRepository.GetByUserIdAsync(userId, cancellationToken);
        if (profile == null)
        {
            return false;
        }

        if (profile.FundRaiserApplicationFeePaid == true
            && string.Equals(profile.FundRaiserPaymentReference, reference, StringComparison.Ordinal))
        {
            return true;
        }

        // Accept confirm when reference matches the pending init, or when profile has no ref yet (race).
        if (!string.IsNullOrWhiteSpace(profile.FundRaiserPaymentReference)
            && !string.Equals(profile.FundRaiserPaymentReference, reference, StringComparison.Ordinal))
        {
            // Newer init may have replaced the reference; still allow matching ANT-FEE for this user.
            if (!reference.StartsWith($"ANT-FEE-{userId}-", StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }
        }

        var expectedKobo = PaystackAmountConverter.ToKobo(paystackOptions.Value.ApplicationFeeAmountNgn);
        if (amountKobo != expectedKobo)
        {
            return false;
        }

        var actor = ResolveActor();
        profile.FundRaiserPaymentReference = reference;
        profile.FundRaiserPaymentStatus = "Paid";
        profile.FundRaiserApplicationFeePaid = true;
        if (!string.IsNullOrWhiteSpace(channel) && string.IsNullOrWhiteSpace(profile.FundRaiserPaymentMethod))
        {
            profile.FundRaiserPaymentMethod = channel;
        }

        profile.Updated(actor);
        await profileRepository.UpdateAsync(profile, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<bool> TryMarkFailedChargeAsync(
        string reference,
        string rawPayload,
        CancellationToken cancellationToken = default)
    {
        if (!PaystackReferenceGenerator.TryParseApplicationFeeUserId(reference, out var userId))
        {
            return false;
        }

        var profile = await profileRepository.GetByUserIdAsync(userId, cancellationToken);
        if (profile == null)
        {
            return false;
        }

        if (profile.FundRaiserApplicationFeePaid == true)
        {
            return true;
        }

        if (!string.IsNullOrWhiteSpace(profile.FundRaiserPaymentReference)
            && !string.Equals(profile.FundRaiserPaymentReference, reference, StringComparison.Ordinal))
        {
            return false;
        }

        var actor = ResolveActor();
        profile.FundRaiserPaymentReference = reference;
        profile.FundRaiserPaymentStatus = "Failed";
        profile.FundRaiserApplicationFeePaid = false;
        profile.Updated(actor);
        await profileRepository.UpdateAsync(profile, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return true;
    }

    private string ResolveActor() =>
        !string.IsNullOrEmpty(currentUser.UserName)
            ? currentUser.UserName
            : (!string.IsNullOrEmpty(currentUser.IPAddress) ? currentUser.IPAddress : "PaystackWebhook");
}
