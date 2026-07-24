using System.Text.Json;
using Antital.Application.Features.Onboarding.ApplicationFeePayment;
using BuildingBlocks.Application.Features;

namespace Antital.Application.Features.Investments.ProcessPaystackWebhook;

public class ProcessPaystackWebhookCommandHandler(
    IInvestmentPaymentConfirmationService paymentConfirmationService,
    IApplicationFeePaymentConfirmationService applicationFeePaymentConfirmationService
) : ICommandQueryHandler<ProcessPaystackWebhookCommand, bool>
{
    public async Task<Result<bool>> Handle(ProcessPaystackWebhookCommand request, CancellationToken cancellationToken)
    {
        using var document = JsonDocument.Parse(request.RawBody);
        var root = document.RootElement;

        if (!root.TryGetProperty("event", out var eventElement))
        {
            return Ignored();
        }

        var eventName = eventElement.GetString();

        if (!root.TryGetProperty("data", out var data))
        {
            return Ignored();
        }

        var reference = data.TryGetProperty("reference", out var referenceElement)
            ? referenceElement.GetString()
            : null;

        if (string.IsNullOrWhiteSpace(reference))
        {
            return Ignored();
        }

        var handled = eventName switch
        {
            "charge.success" => await HandleSuccessAsync(data, reference, request.RawBody, cancellationToken),
            "charge.failed" => await HandleFailedAsync(reference, request.RawBody, cancellationToken),
            _ => false,
        };

        var result = new Result<bool>();
        result.AddValue(handled);
        result.OK();
        return result;
    }

    private static Result<bool> Ignored()
    {
        var ignored = new Result<bool>();
        ignored.AddValue(false);
        ignored.OK();
        return ignored;
    }

    private async Task<bool> HandleSuccessAsync(
        JsonElement data,
        string reference,
        string rawPayload,
        CancellationToken cancellationToken)
    {
        var amountKobo = data.TryGetProperty("amount", out var amountElement) && amountElement.TryGetInt32(out var amount)
            ? amount
            : 0;
        var channel = data.TryGetProperty("channel", out var channelElement)
            ? channelElement.GetString()
            : null;

        if (await paymentConfirmationService.TryConfirmSuccessfulChargeAsync(
                reference,
                amountKobo,
                channel,
                rawPayload,
                cancellationToken))
        {
            return true;
        }

        return await applicationFeePaymentConfirmationService.TryConfirmSuccessfulChargeAsync(
            reference,
            amountKobo,
            channel,
            rawPayload,
            cancellationToken);
    }

    private async Task<bool> HandleFailedAsync(
        string reference,
        string rawPayload,
        CancellationToken cancellationToken)
    {
        if (await paymentConfirmationService.TryMarkFailedChargeAsync(reference, rawPayload, cancellationToken))
        {
            return true;
        }

        return await applicationFeePaymentConfirmationService.TryMarkFailedChargeAsync(
            reference,
            rawPayload,
            cancellationToken);
    }
}
