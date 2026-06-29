using Antital.Application.DTOs.Investments;
using Swashbuckle.AspNetCore.Filters;

namespace Antital.Application.Features.Investments.Swagger;

public class InitializeInvestmentPaymentResponseExample : IExamplesProvider<InitializeInvestmentPaymentResponse>
{
    public InitializeInvestmentPaymentResponse GetExamples() =>
        new(
            AuthorizationUrl: "https://checkout.paystack.com/abc123",
            AccessCode: "access_code_xyz",
            Reference: "ANT-ORD-42-a1b2c3d4e5f6",
            PublicKey: "pk_test_xxxxxxxx");
}
