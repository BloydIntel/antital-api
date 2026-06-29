using Antital.Domain.Enums;
using BuildingBlocks.Application.Exceptions;

namespace Antital.Application.Features.Investments.Paystack;

public static class PaystackChannelMapper
{
    public static bool TryParseChannel(string channel, out PaymentChannel parsed)
    {
        parsed = default;
        switch (channel.Trim().ToLowerInvariant())
        {
            case "card":
                parsed = PaymentChannel.Card;
                return true;
            case "transfer":
                parsed = PaymentChannel.Transfer;
                return true;
            case "opay":
                parsed = PaymentChannel.Opay;
                return true;
            default:
                return false;
        }
    }

    public static PaymentChannel ParseChannel(string channel)
    {
        if (TryParseChannel(channel, out var parsed))
        {
            return parsed;
        }

        throw new BadRequestException(
            "Unsupported payment channel.",
            new Dictionary<string, string[]>
            {
                ["channel"] = ["Channel must be card, transfer, or opay."],
            });
    }

    public static IReadOnlyList<string> ToPaystackChannels(PaymentChannel channel) =>
        channel switch
        {
            PaymentChannel.Card => ["card"],
            PaymentChannel.Transfer => ["bank_transfer"],
            PaymentChannel.Opay => ["opay"],
            _ => throw new ArgumentOutOfRangeException(nameof(channel), channel, null)
        };
}
