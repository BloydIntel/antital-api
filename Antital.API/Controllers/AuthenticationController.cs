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
using Antital.Application.Features.Authentication.ResendVerificationEmail;
using Antital.Application.Features.Users.GetUsers;
using Antital.Application.Features.Users.GetUserById;
using Antital.Application.Features.Users.CreateUser;
using Antital.Application.Features.Users.UpdateUser;
using Antital.Application.Features.Users.DeleteUser;
using Antital.Application.DTOs;
using Microsoft.AspNetCore.Authorization;
using BuildingBlocks.Application.Features;

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

    [HttpPost("resend-verification")]
    [SwaggerOperation("Resend Verification Email", "Send a new verification email if the user is not verified")]
    [SwaggerResponse(StatusCodes.Status200OK, "Verification email resent", typeof(void))]
    [SwaggerResponse(StatusCodes.Status400BadRequest, "Email already verified or invalid request", typeof(void))]
    [SwaggerResponse(StatusCodes.Status404NotFound, "User not found", typeof(void))]
    public async Task<IActionResult> ResendVerificationEmail(ResendVerificationEmailCommand request, CancellationToken cancellationToken)
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

[SwaggerTag("Users Service")]
[Route("api/users")]
public class UsersController(IMediator mediator) : BaseController
{
    [HttpGet]
    [SwaggerOperation("List Users", "Get all users")]
    [SwaggerResponse(StatusCodes.Status200OK, "Retrieved", typeof(Result<List<UserDto>>))]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new GetUsersQuery(), cancellationToken);
        return ApiResult(result);
    }

    [HttpGet("{id:int}")]
    [SwaggerOperation("Get User", "Get user by id")]
    [SwaggerResponse(StatusCodes.Status200OK, "Retrieved", typeof(Result<UserDto>))]
    [SwaggerResponse(StatusCodes.Status404NotFound, "User not found", typeof(void))]
    public async Task<IActionResult> GetById(int id, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new GetUserByIdQuery(id), cancellationToken);
        return ApiResult(result);
    }

    [HttpPost]
    [SwaggerOperation("Create User", "Create a new user")]
    [SwaggerResponse(StatusCodes.Status200OK, "Created", typeof(Result<UserDto>))]
    [SwaggerResponse(StatusCodes.Status409Conflict, "Email already exists", typeof(void))]
    public async Task<IActionResult> Create(CreateUserCommand request, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(request, cancellationToken);
        return ApiResult(result);
    }

    [HttpPut("{id:int}")]
    [SwaggerOperation("Update User", "Update an existing user")]
    [SwaggerResponse(StatusCodes.Status200OK, "Updated", typeof(Result))]
    [SwaggerResponse(StatusCodes.Status404NotFound, "User not found", typeof(void))]
    public async Task<IActionResult> Update(int id, UpdateUserCommand request, CancellationToken cancellationToken)
    {
        var command = request with { Id = id };
        var result = await mediator.Send(command, cancellationToken);
        return ApiResult(result);
    }

    [Authorize(Policy = "CanDeletePolicy")]
    [HttpDelete("{id:int}")]
    [SwaggerOperation("Delete User", "Delete a user (admin only)")]
    [SwaggerResponse(StatusCodes.Status200OK, "Deleted", typeof(Result))]
    [SwaggerResponse(StatusCodes.Status404NotFound, "User not found", typeof(void))]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new DeleteUserCommand(id), cancellationToken);
        return ApiResult(result);
    }
}
