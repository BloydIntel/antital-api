using Antital.Application.DTOs.Fundraisers;
using Antital.Application.Features.Fundraisers.GetFundraiserDashboard;
using BuildingBlocks.API.Controllers;
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
}
