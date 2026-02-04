using Antital.Application.DTOs.Onboarding;
using BuildingBlocks.Application.Features;

namespace Antital.Application.Features.Onboarding.GetOnboarding;

public record GetOnboardingQuery : ICommandQuery<OnboardingResponse>;
