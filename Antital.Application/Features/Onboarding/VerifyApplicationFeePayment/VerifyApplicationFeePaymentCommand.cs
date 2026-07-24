using Antital.Application.DTOs.Onboarding;
using BuildingBlocks.Application.Features;

namespace Antital.Application.Features.Onboarding.VerifyApplicationFeePayment;

public record VerifyApplicationFeePaymentCommand(string? Reference = null)
    : ICommandQuery<ApplicationFeeStatusResponse>;
