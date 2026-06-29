using Antital.Application.DTOs.Investments;
using BuildingBlocks.Application.Features;

namespace Antital.Application.Features.Investments.VerifyInvestmentPayment;

public record VerifyInvestmentPaymentCommand(int OrderId) : ICommandQuery<GetInvestmentOrderResponse>;
