using BuildingBlocks.Application.Features;

namespace Antital.Application.Features.Investments.ProcessPaystackWebhook;

public record ProcessPaystackWebhookCommand(string RawBody) : ICommandQuery<bool>;
