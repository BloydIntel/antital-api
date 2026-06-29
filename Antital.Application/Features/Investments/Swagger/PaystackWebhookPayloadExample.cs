using Antital.Application.DTOs.Investments;
using Swashbuckle.AspNetCore.Filters;

namespace Antital.Application.Features.Investments.Swagger;

public class PaystackWebhookPayloadExample : IExamplesProvider<PaystackWebhookPayloadDto>
{
    public PaystackWebhookPayloadDto GetExamples() =>
        new(
            Event: "charge.success",
            Data: new PaystackChargeDataDto(
                Reference: "ANT-ORD-42-a1b2c3d4e5f6",
                Amount: 102_500,
                Channel: "card",
                Status: "success"));
}
