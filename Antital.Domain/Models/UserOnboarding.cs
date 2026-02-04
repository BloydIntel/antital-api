using BuildingBlocks.Domain.Models;
using Antital.Domain.Enums;

namespace Antital.Domain.Models;

/// <summary>
/// Tracks onboarding progress and status per user (one per user per flow type).
/// </summary>
public class UserOnboarding : TrackableEntity
{
    public int UserId { get; set; }
    public OnboardingFlowType FlowType { get; set; }
    public OnboardingStep CurrentStep { get; set; }
    public OnboardingStatus Status { get; set; }
    public DateTime? SubmittedAt { get; set; }

    public virtual User User { get; set; } = null!;
}
