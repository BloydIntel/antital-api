using Antital.Application.DTOs.Fundraisers;
using Antital.Application.Features.Fundraisers.CampaignUpdates.CreateFundraiserCampaignUpdate;
using Antital.Application.Features.Fundraisers.CampaignUpdates.ListFundraiserCampaignUpdates;
using Antital.Application.Features.Fundraisers.CampaignUpdates.UpdateFundraiserCampaignUpdate;
using Antital.Application.Features.Fundraisers.Documents.ListFundraiserDocuments;
using Antital.Application.Features.Fundraisers.Documents.UploadFundraiserDocument;
using Antital.Application.Features.Fundraisers.GetFundraiserAnalytics;
using Antital.Application.Features.Fundraisers.GetFundraiserCampaign;
using Antital.Application.Features.Fundraisers.GetFundraiserDashboard;
using Antital.Application.Features.Fundraisers.Investors.GetFundraiserInvestorAnalytics;
using Antital.Application.Features.Fundraisers.Investors.GetFundraiserQiiParticipation;
using Antital.Application.Features.Fundraisers.Investors.ListFundraiserInvestorMessages;
using Antital.Application.Features.Fundraisers.Investors.ReplyFundraiserInvestorMessage;
using Antital.Application.Features.Fundraisers.Investors.UpdateFundraiserInvestorMessage;
using Antital.Application.Features.Fundraisers.Settings.GetFundraiserNotificationPreferences;
using Antital.Application.Features.Fundraisers.Settings.GetFundraiserSettingsProfile;
using Antital.Application.Features.Fundraisers.Settings.UpdateFundraiserNotificationPreferences;
using Antital.Application.Features.Fundraisers.Settings.UpdateFundraiserSettingsProfile;
using Antital.Domain.Interfaces;
using BuildingBlocks.API.Controllers;
using BuildingBlocks.Application.Exceptions;
using BuildingBlocks.Application.Features;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace Antital.API.Controllers;

[SwaggerTag("Fundraisers")]
[Route("api/fundraisers")]
[Authorize]
[ApiController]
public class FundraisersController(IMediator mediator) : BaseController
{
    [HttpGet("me/dashboard")]
    [SwaggerOperation(
        "Get Fundraiser Dashboard",
        "Returns campaign summary, funding progress, investor breakdown, and milestones for the authenticated fundraiser.")]
    [SwaggerResponse(StatusCodes.Status200OK, "Success", typeof(Result<FundraiserDashboardResponse>))]
    [SwaggerResponse(StatusCodes.Status400BadRequest, "Invalid period", typeof(void))]
    [SwaggerResponse(StatusCodes.Status401Unauthorized, "Not authenticated", typeof(void))]
    [SwaggerResponse(StatusCodes.Status403Forbidden, "Not a fundraiser", typeof(void))]
    public async Task<IActionResult> GetDashboard(
        [FromQuery] string period = "this-month",
        CancellationToken cancellationToken = default)
    {
        var result = await mediator.Send(new GetFundraiserDashboardQuery(period), cancellationToken);
        return ApiResult(result);
    }

    [HttpGet("me/campaign")]
    [SwaggerOperation(
        "Get Fundraiser Campaign",
        "Returns the authenticated fundraiser's primary owned offering context for the campaigns page.")]
    [SwaggerResponse(StatusCodes.Status200OK, "Success", typeof(Result<FundraiserCampaignResponse>))]
    [SwaggerResponse(StatusCodes.Status401Unauthorized, "Not authenticated", typeof(void))]
    [SwaggerResponse(StatusCodes.Status403Forbidden, "Not a fundraiser", typeof(void))]
    public async Task<IActionResult> GetCampaign(CancellationToken cancellationToken = default)
    {
        var result = await mediator.Send(new GetFundraiserCampaignQuery(), cancellationToken);
        return ApiResult(result);
    }

    [HttpGet("me/campaign/updates")]
    [SwaggerOperation(
        "List Campaign Updates",
        "Returns draft and/or published updates for the authenticated fundraiser's primary owned campaign.")]
    [SwaggerResponse(StatusCodes.Status200OK, "Success", typeof(Result<FundraiserCampaignUpdatesResponse>))]
    [SwaggerResponse(StatusCodes.Status400BadRequest, "Invalid status", typeof(void))]
    [SwaggerResponse(StatusCodes.Status401Unauthorized, "Not authenticated", typeof(void))]
    [SwaggerResponse(StatusCodes.Status403Forbidden, "Not a fundraiser", typeof(void))]
    public async Task<IActionResult> ListCampaignUpdates(
        [FromQuery] string status = "all",
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var result = await mediator.Send(
            new ListFundraiserCampaignUpdatesQuery(status, page, pageSize),
            cancellationToken);
        return ApiResult(result);
    }

    [HttpPost("me/campaign/updates")]
    [SwaggerOperation(
        "Create Campaign Update",
        "Creates a draft or published update for the authenticated fundraiser's primary owned campaign.")]
    [SwaggerResponse(StatusCodes.Status200OK, "Success", typeof(Result<FundraiserCampaignUpdateDto>))]
    [SwaggerResponse(StatusCodes.Status400BadRequest, "Validation error", typeof(void))]
    [SwaggerResponse(StatusCodes.Status401Unauthorized, "Not authenticated", typeof(void))]
    [SwaggerResponse(StatusCodes.Status403Forbidden, "Not a fundraiser", typeof(void))]
    [SwaggerResponse(StatusCodes.Status404NotFound, "No owned campaign", typeof(void))]
    public async Task<IActionResult> CreateCampaignUpdate(
        [FromBody] CreateFundraiserCampaignUpdateRequest request,
        CancellationToken cancellationToken = default)
    {
        var result = await mediator.Send(
            new CreateFundraiserCampaignUpdateCommand(request.Title, request.Body, request.Publish),
            cancellationToken);
        return ApiResult(result);
    }

    [HttpPatch("me/campaign/updates/{updateId:int}")]
    [SwaggerOperation(
        "Update Campaign Update",
        "Edits title/body and/or publishes a draft owned by the authenticated fundraiser.")]
    [SwaggerResponse(StatusCodes.Status200OK, "Success", typeof(Result<FundraiserCampaignUpdateDto>))]
    [SwaggerResponse(StatusCodes.Status400BadRequest, "Validation error", typeof(void))]
    [SwaggerResponse(StatusCodes.Status401Unauthorized, "Not authenticated", typeof(void))]
    [SwaggerResponse(StatusCodes.Status403Forbidden, "Not a fundraiser", typeof(void))]
    [SwaggerResponse(StatusCodes.Status404NotFound, "Update or campaign not found", typeof(void))]
    public async Task<IActionResult> UpdateCampaignUpdate(
        int updateId,
        [FromBody] UpdateFundraiserCampaignUpdateRequest request,
        CancellationToken cancellationToken = default)
    {
        var result = await mediator.Send(
            new UpdateFundraiserCampaignUpdateCommand(updateId, request.Title, request.Body, request.Publish),
            cancellationToken);
        return ApiResult(result);
    }

    [HttpGet("me/analytics")]
    [SwaggerOperation(
        "Get Fundraiser Analytics",
        "Returns engagement overview, traffic series, investor diversity, and conversion metrics for the primary owned offering.")]
    [SwaggerResponse(StatusCodes.Status200OK, "Success", typeof(Result<FundraiserAnalyticsResponse>))]
    [SwaggerResponse(StatusCodes.Status400BadRequest, "Invalid period", typeof(void))]
    [SwaggerResponse(StatusCodes.Status401Unauthorized, "Not authenticated", typeof(void))]
    [SwaggerResponse(StatusCodes.Status403Forbidden, "Not a fundraiser", typeof(void))]
    public async Task<IActionResult> GetAnalytics(
        [FromQuery] string period = "last-7-days",
        CancellationToken cancellationToken = default)
    {
        var result = await mediator.Send(new GetFundraiserAnalyticsQuery(period), cancellationToken);
        return ApiResult(result);
    }

    [HttpGet("me/investors/qii")]
    [SwaggerOperation(
        "List QII Participation",
        "Returns Qualified Institutional Investor commitments for the fundraiser's primary owned offering.")]
    [SwaggerResponse(StatusCodes.Status200OK, "Success", typeof(Result<FundraiserQiiParticipationResponse>))]
    [SwaggerResponse(StatusCodes.Status401Unauthorized, "Not authenticated", typeof(void))]
    [SwaggerResponse(StatusCodes.Status403Forbidden, "Not a fundraiser", typeof(void))]
    public async Task<IActionResult> GetQiiParticipation(CancellationToken cancellationToken = default)
    {
        var result = await mediator.Send(new GetFundraiserQiiParticipationQuery(), cancellationToken);
        return ApiResult(result);
    }

    [HttpGet("me/investors/messages")]
    [SwaggerOperation(
        "List Investor Messages",
        "Returns investor questions for the authenticated fundraiser's primary owned offering.")]
    [SwaggerResponse(StatusCodes.Status200OK, "Success", typeof(Result<FundraiserInvestorMessagesResponse>))]
    [SwaggerResponse(StatusCodes.Status400BadRequest, "Invalid status", typeof(void))]
    [SwaggerResponse(StatusCodes.Status401Unauthorized, "Not authenticated", typeof(void))]
    [SwaggerResponse(StatusCodes.Status403Forbidden, "Not a fundraiser", typeof(void))]
    public async Task<IActionResult> ListInvestorMessages(
        [FromQuery] string status = "all",
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var result = await mediator.Send(
            new ListFundraiserInvestorMessagesQuery(status, page, pageSize),
            cancellationToken);
        return ApiResult(result);
    }

    [HttpPost("me/investors/messages/{messageId:int}/reply")]
    [SwaggerOperation(
        "Reply To Investor Message",
        "Posts a reply to an investor question on the fundraiser's owned offering.")]
    [SwaggerResponse(StatusCodes.Status200OK, "Success", typeof(Result<FundraiserInvestorMessageDto>))]
    [SwaggerResponse(StatusCodes.Status400BadRequest, "Validation error", typeof(void))]
    [SwaggerResponse(StatusCodes.Status401Unauthorized, "Not authenticated", typeof(void))]
    [SwaggerResponse(StatusCodes.Status403Forbidden, "Not a fundraiser", typeof(void))]
    [SwaggerResponse(StatusCodes.Status404NotFound, "Message or campaign not found", typeof(void))]
    public async Task<IActionResult> ReplyInvestorMessage(
        int messageId,
        [FromBody] ReplyFundraiserInvestorMessageRequest request,
        CancellationToken cancellationToken = default)
    {
        var result = await mediator.Send(
            new ReplyFundraiserInvestorMessageCommand(messageId, request.Reply),
            cancellationToken);
        return ApiResult(result);
    }

    [HttpPatch("me/investors/messages/{messageId:int}")]
    [SwaggerOperation(
        "Update Investor Message",
        "Updates visibility and/or reply text for an investor message on the owned offering.")]
    [SwaggerResponse(StatusCodes.Status200OK, "Success", typeof(Result<FundraiserInvestorMessageDto>))]
    [SwaggerResponse(StatusCodes.Status400BadRequest, "Validation error", typeof(void))]
    [SwaggerResponse(StatusCodes.Status401Unauthorized, "Not authenticated", typeof(void))]
    [SwaggerResponse(StatusCodes.Status403Forbidden, "Not a fundraiser", typeof(void))]
    [SwaggerResponse(StatusCodes.Status404NotFound, "Message or campaign not found", typeof(void))]
    public async Task<IActionResult> UpdateInvestorMessage(
        int messageId,
        [FromBody] UpdateFundraiserInvestorMessageRequest request,
        CancellationToken cancellationToken = default)
    {
        var result = await mediator.Send(
            new UpdateFundraiserInvestorMessageCommand(messageId, request.Visibility, request.Reply),
            cancellationToken);
        return ApiResult(result);
    }

    [HttpGet("me/investors/analytics")]
    [SwaggerOperation(
        "Get Investor Inbox Analytics",
        "Returns response rate and average response time for the fundraiser's primary owned offering inbox.")]
    [SwaggerResponse(StatusCodes.Status200OK, "Success", typeof(Result<FundraiserInvestorAnalyticsResponse>))]
    [SwaggerResponse(StatusCodes.Status401Unauthorized, "Not authenticated", typeof(void))]
    [SwaggerResponse(StatusCodes.Status403Forbidden, "Not a fundraiser", typeof(void))]
    public async Task<IActionResult> GetInvestorAnalytics(CancellationToken cancellationToken = default)
    {
        var result = await mediator.Send(new GetFundraiserInvestorAnalyticsQuery(), cancellationToken);
        return ApiResult(result);
    }

    [HttpGet("me/settings/profile")]
    [SwaggerOperation(
        "Get Fundraiser Settings Profile",
        "Returns company and primary contact fields for the authenticated fundraiser settings pages.")]
    [SwaggerResponse(StatusCodes.Status200OK, "Success", typeof(Result<FundraiserSettingsProfileResponse>))]
    [SwaggerResponse(StatusCodes.Status401Unauthorized, "Not authenticated", typeof(void))]
    [SwaggerResponse(StatusCodes.Status403Forbidden, "Not a fundraiser", typeof(void))]
    public async Task<IActionResult> GetSettingsProfile(CancellationToken cancellationToken = default)
    {
        var result = await mediator.Send(new GetFundraiserSettingsProfileQuery(), cancellationToken);
        return ApiResult(result);
    }

    [HttpPut("me/settings/profile")]
    [SwaggerOperation(
        "Update Fundraiser Settings Profile",
        "Updates editable company and primary contact fields for the authenticated fundraiser.")]
    [SwaggerResponse(StatusCodes.Status200OK, "Success", typeof(Result<FundraiserSettingsProfileResponse>))]
    [SwaggerResponse(StatusCodes.Status400BadRequest, "Validation error", typeof(void))]
    [SwaggerResponse(StatusCodes.Status401Unauthorized, "Not authenticated", typeof(void))]
    [SwaggerResponse(StatusCodes.Status403Forbidden, "Not a fundraiser", typeof(void))]
    public async Task<IActionResult> UpdateSettingsProfile(
        [FromBody] UpdateFundraiserSettingsProfileRequest request,
        CancellationToken cancellationToken = default)
    {
        var result = await mediator.Send(
            new UpdateFundraiserSettingsProfileCommand(
                request.CompanyName,
                request.RegistrationNumber,
                request.Bio,
                request.Website,
                request.PublicEmail,
                request.Headquarters,
                request.Contact),
            cancellationToken);
        return ApiResult(result);
    }

    [HttpGet("me/settings/notifications")]
    [SwaggerOperation(
        "Get Fundraiser Notification Preferences",
        "Returns email, in-app, and marketing notification preferences for the authenticated fundraiser.")]
    [SwaggerResponse(StatusCodes.Status200OK, "Success", typeof(Result<FundraiserNotificationPreferencesResponse>))]
    [SwaggerResponse(StatusCodes.Status401Unauthorized, "Not authenticated", typeof(void))]
    [SwaggerResponse(StatusCodes.Status403Forbidden, "Not a fundraiser", typeof(void))]
    public async Task<IActionResult> GetNotificationPreferences(CancellationToken cancellationToken = default)
    {
        var result = await mediator.Send(new GetFundraiserNotificationPreferencesQuery(), cancellationToken);
        return ApiResult(result);
    }

    [HttpPut("me/settings/notifications")]
    [SwaggerOperation(
        "Update Fundraiser Notification Preferences",
        "Upserts email, in-app, and marketing notification preferences for the authenticated fundraiser.")]
    [SwaggerResponse(StatusCodes.Status200OK, "Success", typeof(Result<FundraiserNotificationPreferencesResponse>))]
    [SwaggerResponse(StatusCodes.Status400BadRequest, "Validation error", typeof(void))]
    [SwaggerResponse(StatusCodes.Status401Unauthorized, "Not authenticated", typeof(void))]
    [SwaggerResponse(StatusCodes.Status403Forbidden, "Not a fundraiser", typeof(void))]
    public async Task<IActionResult> UpdateNotificationPreferences(
        [FromBody] UpdateFundraiserNotificationPreferencesRequest request,
        CancellationToken cancellationToken = default)
    {
        var result = await mediator.Send(
            new UpdateFundraiserNotificationPreferencesCommand(
                request.Email,
                request.InApp,
                request.Marketing),
            cancellationToken);
        return ApiResult(result);
    }

    [HttpGet("me/documents")]
    [SwaggerOperation(
        "List Fundraiser Documents",
        "Returns offering documents for the authenticated fundraiser's primary owned campaign.")]
    [SwaggerResponse(StatusCodes.Status200OK, "Success", typeof(Result<FundraiserDocumentsResponse>))]
    [SwaggerResponse(StatusCodes.Status401Unauthorized, "Not authenticated", typeof(void))]
    [SwaggerResponse(StatusCodes.Status403Forbidden, "Not a fundraiser", typeof(void))]
    public async Task<IActionResult> ListDocuments(CancellationToken cancellationToken = default)
    {
        var result = await mediator.Send(new ListFundraiserDocumentsQuery(), cancellationToken);
        return ApiResult(result);
    }

    [HttpPost("me/documents")]
    [RequestSizeLimit(FileUploadLimits.MaxFileBytes)]
    [RequestFormLimits(MultipartBodyLengthLimit = FileUploadLimits.MaxFileBytes)]
    [Consumes("multipart/form-data")]
    [SwaggerOperation(
        "Upload Fundraiser Document",
        "Uploads a document via Cloudinary and attaches it to the primary owned offering as Pending Approval.")]
    [SwaggerResponse(StatusCodes.Status200OK, "Success", typeof(Result<FundraiserDocumentDto>))]
    [SwaggerResponse(StatusCodes.Status400BadRequest, "Validation error", typeof(void))]
    [SwaggerResponse(StatusCodes.Status401Unauthorized, "Not authenticated", typeof(void))]
    [SwaggerResponse(StatusCodes.Status403Forbidden, "Not a fundraiser", typeof(void))]
    [SwaggerResponse(StatusCodes.Status404NotFound, "No owned campaign", typeof(void))]
    public async Task<IActionResult> UploadDocument(
        IFormFile? file,
        [FromForm] string? title,
        [FromForm] string category = "Core",
        CancellationToken cancellationToken = default)
    {
        if (file is null || file.Length <= 0)
        {
            throw new BadRequestException(
                "Invalid file.",
                new Dictionary<string, string[]> { ["file"] = ["File is required."] });
        }

        await using var stream = file.OpenReadStream();
        var result = await mediator.Send(
            new UploadFundraiserDocumentCommand(
                stream,
                file.FileName,
                file.ContentType,
                file.Length,
                title ?? string.Empty,
                category),
            cancellationToken);
        return ApiResult(result);
    }
}
