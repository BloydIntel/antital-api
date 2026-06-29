using Antital.Application.DTOs.Investments;
using BuildingBlocks.Application.Features;

namespace Antital.Application.Features.Investments.GetInvestmentOrder;

public record GetInvestmentOrderQuery(int OrderId) : ICommandQuery<GetInvestmentOrderResponse>;
