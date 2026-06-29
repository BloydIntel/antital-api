using Antital.Application.DTOs.Investments;
using BuildingBlocks.Application.Features;

namespace Antital.Application.Features.Investments.CreateInvestmentOrder;

public record CreateInvestmentOrderCommand(int OfferingId, int Units)
    : ICommandQuery<CreateInvestmentOrderResponse>;
