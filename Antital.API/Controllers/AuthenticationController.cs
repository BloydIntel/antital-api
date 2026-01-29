using MediatR;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;
using BuildingBlocks.API.Controllers;
using Antital.Application.Features.Authentication.Login;
using Antital.Application.Features.Authentication.SignUp;
using Antital.Application.Features.Authentication.VerifyEmail;
using Antital.Application.DTOs.Authentication;
using Swashbuckle.AspNetCore.Filters;

namespace Antital.API.Controllers;

[SwaggerTag("Authentication Service")]
[Route("api/auth")]
public class AuthenticationController(IMediator mediator) : BaseController
{
    [HttpPost("signup")]
    [SwaggerOperation("Sign Up", "Register a new user account")]
    [SwaggerRequestExample(typeof(SignUpCommand), typeof(SignUpCommandExample))]
    [SwaggerResponse(StatusCodes.Status200OK, "User registered successfully", typeof(AuthResponseDto))]
    [SwaggerResponseExample(StatusCodes.Status200OK, typeof(AuthResponseExample))]
    [SwaggerResponse(StatusCodes.Status400BadRequest, "Invalid request data", typeof(void))]
    [SwaggerResponse(StatusCodes.Status409Conflict, "Email already exists", typeof(void))]
    public async Task<IActionResult> SignUp(SignUpCommand request, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(request, cancellationToken);
        return ApiResult(result);
    }

    [HttpPost("login")]
    [SwaggerOperation("Login", "Authenticate user and get access token")]
    [SwaggerRequestExample(typeof(LoginCommand), typeof(LoginCommandExample))]
    [SwaggerResponse(StatusCodes.Status200OK, "Login successful", typeof(AuthResponseDto))]
    [SwaggerResponseExample(StatusCodes.Status200OK, typeof(AuthResponseExample))]
    [SwaggerResponse(StatusCodes.Status400BadRequest, "Invalid request data", typeof(void))]
    [SwaggerResponse(StatusCodes.Status401Unauthorized, "Invalid credentials or email not verified", typeof(void))]
    [SwaggerResponse(StatusCodes.Status404NotFound, "User not found", typeof(void))]
    public async Task<IActionResult> Login(LoginCommand request, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(request, cancellationToken);
        return ApiResult(result);
    }

    [HttpPost("verify-email")]
    [SwaggerOperation("Verify Email", "Verify user email address with verification token")]
    [SwaggerRequestExample(typeof(VerifyEmailCommand), typeof(VerifyEmailCommandExample))]
    [SwaggerResponse(StatusCodes.Status200OK, "Email verified successfully", typeof(void))]
    [SwaggerResponse(StatusCodes.Status400BadRequest, "Invalid or expired token", typeof(void))]
    [SwaggerResponse(StatusCodes.Status404NotFound, "User not found", typeof(void))]
    public async Task<IActionResult> VerifyEmail(VerifyEmailCommand request, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(request, cancellationToken);
        return ApiResult(result);
    }
}