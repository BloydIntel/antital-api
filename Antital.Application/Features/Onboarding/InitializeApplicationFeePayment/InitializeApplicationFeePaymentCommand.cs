using Antital.Application.DTOs.Onboarding;
using Antital.Domain.Enums;
using BuildingBlocks.Application.Features;

namespace Antital.Application.Features.Onboarding.InitializeApplicationFeePayment;

public record InitializeApplicationFeePaymentCommand(PaymentChannel Channel)
    : ICommandQuery<InitializeApplicationFeePaymentResponse>;
