using Antital.Domain.Interfaces;
using Antital.Domain.Models;
using Antital.Domain.Enums;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace Antital.Infrastructure.Services;

public class JwtTokenService : IJwtTokenService
{
    private readonly IConfiguration _configuration;
    private readonly JwtSecurityTokenHandler _tokenHandler;
    private readonly ILogger<JwtTokenService> _logger;

    public JwtTokenService(IConfiguration configuration, ILogger<JwtTokenService> logger)
    {
        _configuration = configuration;
        _tokenHandler = new JwtSecurityTokenHandler();
        _logger = logger;
    }

    public string GenerateToken(User user)
    {
        var securityKey = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(_configuration["Jwt:Key"] ?? throw new InvalidOperationException("JWT Key is not configured."))
        );
        var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

        var claims = new List<Claim>
        {
            new Claim("UserId", user.Id.ToString()),
            new Claim(ClaimTypes.Email, user.Email),
            new Claim("UserType", user.UserType.ToString()),
            new Claim("IsEmailVerified", user.IsEmailVerified.ToString()),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        var tokenExpiryValue = _configuration["Jwt:TokenExpiryMinutes"];
        if (!int.TryParse(tokenExpiryValue, out var tokenExpiryMinutes))
        {
            tokenExpiryMinutes = 15; // safe default
        }
        var issuer = _configuration["Jwt:Issuer"] ?? throw new InvalidOperationException("JWT Issuer is not configured.");
        var audience = _configuration["Jwt:Audience"] ?? throw new InvalidOperationException("JWT Audience is not configured.");

        var token = new JwtSecurityToken(
            issuer: issuer,
            audience: audience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(tokenExpiryMinutes),
            signingCredentials: credentials
        );

        return _tokenHandler.WriteToken(token);
    }

    public ClaimsPrincipal? ValidateToken(string token)
    {
        try
        {
            var securityKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(_configuration["Jwt:Key"] ?? throw new InvalidOperationException("JWT Key is not configured."))
            );

            var validationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                ValidIssuer = _configuration["Jwt:Issuer"],
                ValidAudience = _configuration["Jwt:Audience"],
                IssuerSigningKey = securityKey
            };

            var principal = _tokenHandler.ValidateToken(token, validationParameters, out SecurityToken validatedToken);
            return principal;
        }
        catch (SecurityTokenExpiredException ex)
        {
            _logger.LogWarning(ex, "JWT validation failed: token expired.");
            return null;
        }
        catch (SecurityTokenException ex)
        {
            _logger.LogWarning(ex, "JWT validation failed: security token error.");
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "JWT validation failed: unexpected error.");
            return null;
        }
    }
}
