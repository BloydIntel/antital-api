using MediatR;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;
using BuildingBlocks.API.Controllers;
using Antital.Application.Features.Authentication.Login;
using Antital.Application.Features.Authentication.SignUp;
using Antital.Application.Features.Authentication.VerifyEmail;
using Antital.Application.DTOs.Authentication;
using Swashbuckle.AspNetCore.Filters;
using Antital.Application.Features.Authentication.RefreshToken;
using Antital.Application.Features.Authentication.Logout;
using Antital.Application.Features.Authentication.ForgotPassword;
using Antital.Application.Features.Authentication.ResetPassword;

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

    [HttpPost("refresh")]
    [SwaggerOperation("Refresh Token", "Get a new access token using a refresh token")]
    [SwaggerResponse(StatusCodes.Status200OK, "Token refreshed", typeof(AuthResponseDto))]
    [SwaggerResponse(StatusCodes.Status400BadRequest, "Invalid request data", typeof(void))]
    [SwaggerResponse(StatusCodes.Status401Unauthorized, "Invalid or expired refresh token", typeof(void))]
    public async Task<IActionResult> Refresh(RefreshTokenCommand request, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(request, cancellationToken);
        return ApiResult(result);
    }

    [HttpPost("logout")]
    [SwaggerOperation("Logout", "Revoke a refresh token")]
    [SwaggerResponse(StatusCodes.Status200OK, "Logged out successfully", typeof(void))]
    [SwaggerResponse(StatusCodes.Status401Unauthorized, "Invalid or expired refresh token", typeof(void))]
    public async Task<IActionResult> Logout(LogoutCommand request, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(request, cancellationToken);
        return ApiResult(result);
    }

    [HttpPost("forgot-password")]
    [SwaggerOperation("Forgot Password", "Send a password reset email")]
    [SwaggerResponse(StatusCodes.Status200OK, "Reset email sent (always 200 even if email not found)", typeof(void))]
    public async Task<IActionResult> ForgotPassword(ForgotPasswordCommand request, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(request, cancellationToken);
        return ApiResult(result);
    }

    [HttpPost("reset-password")]
    [SwaggerOperation("Reset Password", "Reset password using email + token")]
    [SwaggerResponse(StatusCodes.Status200OK, "Password reset successfully", typeof(void))]
    [SwaggerResponse(StatusCodes.Status400BadRequest, "Invalid or expired token", typeof(void))]
    [SwaggerResponse(StatusCodes.Status404NotFound, "User not found", typeof(void))]
    public async Task<IActionResult> ResetPassword(ResetPasswordCommand request, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(request, cancellationToken);
        return ApiResult(result);
    }
}
