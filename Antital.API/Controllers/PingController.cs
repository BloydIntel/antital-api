using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Antital.API.Controllers;

/// <summary>
/// Lightweight keep-alive endpoint for schedulers (e.g. Azure App Service warm-up).
/// Prefer GET /ping over Swagger UI or /healthz for frequent pings — minimal CPU and no DB.
/// </summary>
[ApiController]
[AllowAnonymous]
public sealed class PingController : ControllerBase
{
    [HttpGet("/ping")]
    public IActionResult KeepAlive() => Ok("pong");
}
