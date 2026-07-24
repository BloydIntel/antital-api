using Antital.Application.DTOs.Onboarding;
using BuildingBlocks.Application.Features;

namespace Antital.Application.Features.Onboarding.GetApplicationFee;

public record GetApplicationFeeQuery : ICommandQuery<ApplicationFeeStatusResponse>;
