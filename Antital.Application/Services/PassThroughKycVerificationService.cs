using Antital.Domain.Interfaces;

namespace Antital.Application.Services;

/// <summary>
/// Default implementation: stores client-provided document paths/keys as-is.
/// Replace with a third-party implementation (e.g. Veriff, Onfido) by registering it in DI.
/// </summary>
public class PassThroughKycVerificationService : IKycVerificationService
{
    public Task<KycVerificationResult> ProcessAsync(KycVerificationInput input, CancellationToken cancellationToken = default)
    {
        var result = new KycVerificationResult(
            input.GovernmentIdDocumentPathOrKey,
            input.ProofOfAddressDocumentPathOrKey,
            input.SelfieVerificationPathOrKey,
            input.IncomeVerificationPathOrKey,
            GovernmentIdVerifiedAt: null,
            ProofOfAddressVerifiedAt: null,
            SelfieVerifiedAt: null,
            IncomeVerifiedAt: null
        );
        return Task.FromResult(result);
    }
}
