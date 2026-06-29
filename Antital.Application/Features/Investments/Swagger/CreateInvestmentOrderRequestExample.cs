using Antital.Application.DTOs.Investments;
using Swashbuckle.AspNetCore.Filters;

namespace Antital.Application.Features.Investments.Swagger;

public class CreateInvestmentOrderRequestExample : IExamplesProvider<CreateInvestmentOrderRequest>
{
    public CreateInvestmentOrderRequest GetExamples() => new(Units: 10);
}
