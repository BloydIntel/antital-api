using Antital.Application.DTOs.Investors;
using Antital.Application.Features.Investors.GetInvestorDashboard;
using BuildingBlocks.API.Controllers;
using BuildingBlocks.Application.Features;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace Antital.API.Controllers;

[SwaggerTag("Investors")]
[Route("api/investors")]
[Authorize]
[ApiController]
public class InvestorsController(IMediator mediator) : BaseController
{
    [HttpGet("me/dashboard")]
    [SwaggerOperation("Get Investor Dashboard", "Returns dashboard summary, performance, watchlist preview, and holdings for the authenticated investor.")]
    [SwaggerResponse(StatusCodes.Status200OK, "Success", typeof(Result<InvestorDashboardResponse>))]
    [SwaggerResponse(StatusCodes.Status400BadRequest, "Invalid period", typeof(void))]
    [SwaggerResponse(StatusCodes.Status401Unauthorized, "Not authenticated", typeof(void))]
    public async Task<IActionResult> GetDashboard(
        [FromQuery] string period = "this-month",
        CancellationToken cancellationToken = default)
    {
        var result = await mediator.Send(new GetInvestorDashboardQuery(period), cancellationToken);
        return ApiResult(result);
    }
}
