using Antital.Domain.Integrations.Paystack;
using Antital.Domain.Interfaces;

namespace Antital.Test.Fakes;

public class FakePaystackClient : IPaystackClient
{
    public Func<PaystackInitializeRequest, PaystackInitializeResult>? InitializeHandler { get; set; }

    public Task<PaystackInitializeResult> InitializeTransactionAsync(
        PaystackInitializeRequest request,
        CancellationToken cancellationToken = default)
    {
        if (InitializeHandler != null)
        {
            return Task.FromResult(InitializeHandler(request));
        }

        return Task.FromResult(new PaystackInitializeResult(
            true,
            "https://checkout.paystack.com/test-session",
            "access-code-test",
            request.Reference,
            "Initialized"));
    }

    public Task<PaystackVerifyResult> VerifyTransactionAsync(
        string reference,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(new PaystackVerifyResult(true, "success", "card", 0, "Verified"));
}
