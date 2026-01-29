using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace Antital.Application.Common.Security;

public class ResetTokenProtector
{
    private readonly IConfiguration _configuration;
    private readonly JwtSecurityTokenHandler _handler = new();

    public ResetTokenProtector(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public string Protect(string email, string rawToken, DateTime expiresAtUtc)
    {
        var key = _configuration["Jwt:Key"] ?? throw new InvalidOperationException("JWT Key is not configured.");
        var issuer = _configuration["Jwt:Issuer"] ?? "AntitalAPI";
        var audience = _configuration["Jwt:Audience"] ?? "AntitalClient";

        var credentials = new SigningCredentials(
            new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key)),
            SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim("email", email),
            new Claim("rt", rawToken) // reset token
        };

        var token = new JwtSecurityToken(
            issuer: issuer,
            audience: audience,
            claims: claims,
            notBefore: DateTime.UtcNow,
            expires: expiresAtUtc,
            signingCredentials: credentials);

        return _handler.WriteToken(token);
    }

    public (string Email, string RawToken) Unprotect(string protectedToken)
    {
        var key = _configuration["Jwt:Key"] ?? throw new InvalidOperationException("JWT Key is not configured.");
        var issuer = _configuration["Jwt:Issuer"] ?? "AntitalAPI";
        var audience = _configuration["Jwt:Audience"] ?? "AntitalClient";

        var validationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key)),
            ValidateIssuer = true,
            ValidIssuer = issuer,
            ValidateAudience = true,
            ValidAudience = audience,
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromMinutes(1)
        };

        var principal = _handler.ValidateToken(protectedToken, validationParameters, out _);
        // Jwt handler remaps "email" to ClaimTypes.Email by default; check both.
        var email = principal.FindFirstValue(ClaimTypes.Email) ?? principal.FindFirstValue("email")
                    ?? throw new SecurityTokenException("Reset token missing email.");
        var raw = principal.FindFirstValue("rt") ?? throw new SecurityTokenException("Reset token missing payload.");
        return (email, raw);
    }
}
