using Antital.Domain.Enums;

namespace Antital.Application.DTOs.Onboarding;

/// <summary>
/// Payload for the Investor Category step (Retail / Sophisticated / HNI).
/// </summary>
public record InvestorCategoryPayload(InvestorCategory InvestorCategory);
