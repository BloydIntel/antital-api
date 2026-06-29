using Antital.Application.DTOs.Investments;
using Antital.Domain.Enums;
using BuildingBlocks.Application.Features;

namespace Antital.Application.Features.Investments.InitializeInvestmentPayment;

public record InitializeInvestmentPaymentCommand(int OrderId, PaymentChannel Channel)
    : ICommandQuery<InitializeInvestmentPaymentResponse>;
