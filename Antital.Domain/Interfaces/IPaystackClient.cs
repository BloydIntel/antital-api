using Antital.Domain.Integrations.Paystack;

namespace Antital.Domain.Interfaces;

public interface IPaystackClient
{
    Task<PaystackInitializeResult> InitializeTransactionAsync(
        PaystackInitializeRequest request,
        CancellationToken cancellationToken = default);

    Task<PaystackVerifyResult> VerifyTransactionAsync(
        string reference,
        CancellationToken cancellationToken = default);
}
