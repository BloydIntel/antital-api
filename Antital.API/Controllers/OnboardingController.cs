using Antital.Application.DTOs.Onboarding;
using Antital.Application.Features.Onboarding.GetOnboarding;
using Antital.Application.Features.Onboarding.SaveOnboarding;
using Antital.Application.Features.Onboarding.SubmitOnboarding;
using BuildingBlocks.API.Controllers;
using BuildingBlocks.Application.Features;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;
using Swashbuckle.AspNetCore.Filters;

namespace Antital.API.Controllers;

/// <summary>
/// Individual investor onboarding flow. Requires authenticated, email-verified user.
/// Single PUT to save progress (step + payload); GET for resume/Review; POST to submit.
/// </summary>
[SwaggerTag("Onboarding")]
[Route("api/onboarding")]
[Authorize]
[ApiController]
public class OnboardingController(IMediator mediator) : BaseController
{
    /// <summary>
    /// Get current onboarding progress and all saved data (for resume and Review screen).
    /// Personal and location data come from the user profile.
    /// </summary>
    [HttpGet]
    [SwaggerOperation("Get Onboarding", "Returns current step, status, and aggregated onboarding data for the authenticated user.")]
    [SwaggerResponse(StatusCodes.Status200OK, "Success", typeof(Result<OnboardingResponse>))]
    [SwaggerResponse(StatusCodes.Status401Unauthorized, "Not authenticated", typeof(void))]
    [SwaggerResponse(StatusCodes.Status403Forbidden, "Email not verified", typeof(void))]
    public async Task<IActionResult> Get(CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new GetOnboardingQuery(), cancellationToken);
        return ApiResult(result);
    }

    /// <summary>
    /// Save progress for one step. Send the step and the payload for that step only.
    /// Advances current step on success. User can drop at any point and continue later.
    /// </summary>
    /// <remarks>
    /// Frontend: use GET /api/onboarding to read currentStep, then send step = currentStep with only the matching payload (others null).
    /// Step → payload: InvestorCategory (0) → investorCategoryPayload; InvestmentProfile (1) → investmentProfilePayload; Kyc (2) → kycPayload.
    /// </remarks>
    [HttpPut]
    [SwaggerOperation("Save Progress", "Save onboarding data for the given step. Send only the payload for that step; other payloads must be null.")]
    [SwaggerRequestExample(typeof(SaveOnboardingRequest), typeof(SaveOnboardingRequestMultipleExamples))]
    [SwaggerResponse(StatusCodes.Status200OK, "Saved", typeof(Result))]
    [SwaggerResponse(StatusCodes.Status400BadRequest, "Invalid step or payload", typeof(void))]
    [SwaggerResponse(StatusCodes.Status401Unauthorized, "Not authenticated", typeof(void))]
    [SwaggerResponse(StatusCodes.Status403Forbidden, "Email not verified", typeof(void))]
    public async Task<IActionResult> Save(
        [FromBody] SaveOnboardingRequest request,
        CancellationToken cancellationToken)
    {
        var command = new SaveOnboardingCommand(
            request.Step,
            request.InvestorCategoryPayload,
            request.InvestmentProfilePayload,
            request.KycPayload
        );
        var result = await mediator.Send(command, cancellationToken);
        return ApiResult(result);
    }

    /// <summary>
    /// Submit the onboarding application. Requires at least investor category and investment profile.
    /// Sets status to Submitted and records submission time.
    /// </summary>
    [HttpPost("submit")]
    [SwaggerOperation("Submit", "Final submit of the onboarding application.")]
    [SwaggerResponse(StatusCodes.Status200OK, "Submitted", typeof(Result))]
    [SwaggerResponse(StatusCodes.Status400BadRequest, "Already submitted or incomplete (e.g. missing profile)", typeof(void))]
    [SwaggerResponse(StatusCodes.Status401Unauthorized, "Not authenticated", typeof(void))]
    [SwaggerResponse(StatusCodes.Status403Forbidden, "Email not verified", typeof(void))]
    public async Task<IActionResult> Submit(CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new SubmitOnboardingCommand(), cancellationToken);
        return ApiResult(result);
    }
}
