using Antital.Application.DTOs.Investments;
using Swashbuckle.AspNetCore.Filters;

namespace Antital.Application.Features.Investments.Swagger;

public class InitializeInvestmentPaymentRequestExample : IExamplesProvider<InitializeInvestmentPaymentRequest>
{
    public InitializeInvestmentPaymentRequest GetExamples() => new(Channel: "card");
}
