using Antital.Application.DTOs.Investments;
using Antital.Application.Features.Investments.GetOfferingContentBlocks;
using Antital.Application.Features.Investments.GetOfferingDocuments;
using Antital.Application.Features.Investments.GetOfferingFinancials;
using Antital.Application.Features.Investments.GetOfferingHighlights;
using Antital.Application.Features.Investments.GetOfferingMedia;
using Antital.Application.Features.Investments.GetOfferingRisks;
using Antital.Application.Features.Investments.GetOfferingShell;
using Antital.Application.Features.Investments.GetOfferingTeam;
using Antital.Application.Features.Investments.GetOfferingTestimonials;
using Antital.Application.Features.Investments.GetOfferingUpdates;
using Antital.Application.Features.Investments.ListInvestments;
using BuildingBlocks.API.Controllers;
using BuildingBlocks.Application.Features;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace Antital.API.Controllers;

[SwaggerTag("Investments")]
[Route("api/investments")]
[AllowAnonymous]
[ApiController]
public class InvestmentsController(IMediator mediator) : BaseController
{
    [HttpGet]
    [SwaggerOperation("List Investments", "Returns paginated published investment offerings for explore/landing cards.")]
    [SwaggerResponse(StatusCodes.Status200OK, "Success", typeof(Result<InvestmentListResponse>))]
    public async Task<IActionResult> List(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 12,
        [FromQuery] string? category = null,
        [FromQuery] string? risk = null,
        [FromQuery] string? search = null,
        CancellationToken cancellationToken = default)
    {
        var result = await mediator.Send(
            new ListInvestmentsQuery(page, pageSize, category, risk, search),
            cancellationToken);
        return ApiResult(result);
    }

    [HttpGet("{idOrSlug}")]
    [SwaggerOperation("Get Investment Shell", "Returns offering identity, funding, deal terms, and corporate profile.")]
    [SwaggerResponse(StatusCodes.Status200OK, "Success", typeof(Result<OfferingShellResponse>))]
    [SwaggerResponse(StatusCodes.Status404NotFound, "Not found", typeof(void))]
    public async Task<IActionResult> GetShell(string idOrSlug, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new GetOfferingShellQuery(idOrSlug), cancellationToken);
        return ApiResult(result);
    }

    [HttpGet("{idOrSlug}/highlights")]
    [SwaggerResponse(StatusCodes.Status200OK, "Success", typeof(Result<IReadOnlyList<HighlightDto>>))]
    [SwaggerResponse(StatusCodes.Status404NotFound, "Not found", typeof(void))]
    public async Task<IActionResult> GetHighlights(string idOrSlug, CancellationToken cancellationToken) =>
        ApiResult(await mediator.Send(new GetOfferingHighlightsQuery(idOrSlug), cancellationToken));

    [HttpGet("{idOrSlug}/content-blocks")]
    [SwaggerResponse(StatusCodes.Status200OK, "Success", typeof(Result<IReadOnlyList<ContentBlockDto>>))]
    [SwaggerResponse(StatusCodes.Status404NotFound, "Not found", typeof(void))]
    public async Task<IActionResult> GetContentBlocks(string idOrSlug, CancellationToken cancellationToken) =>
        ApiResult(await mediator.Send(new GetOfferingContentBlocksQuery(idOrSlug), cancellationToken));

    [HttpGet("{idOrSlug}/team")]
    [SwaggerResponse(StatusCodes.Status200OK, "Success", typeof(Result<IReadOnlyList<TeamMemberDto>>))]
    [SwaggerResponse(StatusCodes.Status404NotFound, "Not found", typeof(void))]
    public async Task<IActionResult> GetTeam(string idOrSlug, CancellationToken cancellationToken) =>
        ApiResult(await mediator.Send(new GetOfferingTeamQuery(idOrSlug), cancellationToken));

    [HttpGet("{idOrSlug}/financials")]
    [SwaggerResponse(StatusCodes.Status200OK, "Success", typeof(Result<OfferingFinancialsResponse>))]
    [SwaggerResponse(StatusCodes.Status404NotFound, "Not found", typeof(void))]
    public async Task<IActionResult> GetFinancials(string idOrSlug, CancellationToken cancellationToken) =>
        ApiResult(await mediator.Send(new GetOfferingFinancialsQuery(idOrSlug), cancellationToken));

    [HttpGet("{idOrSlug}/risks")]
    [SwaggerResponse(StatusCodes.Status200OK, "Success", typeof(Result<IReadOnlyList<OfferingRiskDto>>))]
    [SwaggerResponse(StatusCodes.Status404NotFound, "Not found", typeof(void))]
    public async Task<IActionResult> GetRisks(string idOrSlug, CancellationToken cancellationToken) =>
        ApiResult(await mediator.Send(new GetOfferingRisksQuery(idOrSlug), cancellationToken));

    [HttpGet("{idOrSlug}/documents")]
    [SwaggerResponse(StatusCodes.Status200OK, "Success", typeof(Result<IReadOnlyList<OfferingDocumentDto>>))]
    [SwaggerResponse(StatusCodes.Status404NotFound, "Not found", typeof(void))]
    public async Task<IActionResult> GetDocuments(string idOrSlug, CancellationToken cancellationToken) =>
        ApiResult(await mediator.Send(new GetOfferingDocumentsQuery(idOrSlug), cancellationToken));

    [HttpGet("{idOrSlug}/media")]
    [SwaggerResponse(StatusCodes.Status200OK, "Success", typeof(Result<IReadOnlyList<MediaAssetDto>>))]
    [SwaggerResponse(StatusCodes.Status404NotFound, "Not found", typeof(void))]
    public async Task<IActionResult> GetMedia(string idOrSlug, CancellationToken cancellationToken) =>
        ApiResult(await mediator.Send(new GetOfferingMediaQuery(idOrSlug), cancellationToken));

    [HttpGet("{idOrSlug}/updates")]
    [SwaggerResponse(StatusCodes.Status200OK, "Success", typeof(Result<OfferingUpdatesResponse>))]
    [SwaggerResponse(StatusCodes.Status404NotFound, "Not found", typeof(void))]
    public async Task<IActionResult> GetUpdates(
        string idOrSlug,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default) =>
        ApiResult(await mediator.Send(new GetOfferingUpdatesQuery(idOrSlug, page, pageSize), cancellationToken));

    [HttpGet("{idOrSlug}/testimonials")]
    [SwaggerResponse(StatusCodes.Status200OK, "Success", typeof(Result<IReadOnlyList<TestimonialDto>>))]
    [SwaggerResponse(StatusCodes.Status404NotFound, "Not found", typeof(void))]
    public async Task<IActionResult> GetTestimonials(string idOrSlug, CancellationToken cancellationToken) =>
        ApiResult(await mediator.Send(new GetOfferingTestimonialsQuery(idOrSlug), cancellationToken));
}
