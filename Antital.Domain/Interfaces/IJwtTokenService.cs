using Antital.Domain.Models;
using System.Security.Claims;

namespace Antital.Domain.Interfaces;

public interface IJwtTokenService
{
    string GenerateToken(User user);
    ClaimsPrincipal? ValidateToken(string token);
}
