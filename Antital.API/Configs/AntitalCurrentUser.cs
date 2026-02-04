using Antital.Domain.Interfaces;
using Microsoft.AspNetCore.Http;
using System.Security.Claims;

namespace Antital.API.Configs;

/// <summary>
/// Provides current user from JWT (UserId claim) for onboarding and user-scoped operations.
/// </summary>
public class AntitalCurrentUser(IHttpContextAccessor httpContextAccessor) : IAntitalCurrentUser
{
    public string IPAddress =>
        httpContextAccessor.HttpContext?.Connection.RemoteIpAddress?.ToString() ?? string.Empty;

    public string UserName =>
        httpContextAccessor.HttpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier)
        ?? httpContextAccessor.HttpContext?.User.FindFirstValue(ClaimTypes.Email)
        ?? string.Empty;

    public int? UserId
    {
        get
        {
            var value = httpContextAccessor.HttpContext?.User.FindFirstValue("UserId");
            return int.TryParse(value, out var id) ? id : null;
        }
    }
}
