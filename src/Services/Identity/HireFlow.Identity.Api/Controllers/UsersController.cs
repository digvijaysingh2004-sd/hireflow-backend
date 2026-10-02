using System.Security.Claims;
using FluentValidation;
using HireFlow.Identity.Application.DTOs;
using HireFlow.Identity.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HireFlow.Identity.Api.Controllers;

[ApiController]
[Route("api/v1/users")]
[Authorize]
[Produces("application/json")]
public class UsersController : ControllerBase
{
    private readonly IUserService _userService;
    private readonly ITokenService _tokenService;

    public UsersController(IUserService userService, ITokenService tokenService)
    {
        _userService = userService;
        _tokenService = tokenService;
    }

    /// <summary>
    /// Get all active sessions for the current user.
    /// </summary>
    [HttpGet("me/sessions")]
    [ProducesResponseType(typeof(IReadOnlyList<SessionDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetMySessions(CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        var result = await _userService.GetSessionsAsync(userId, null, cancellationToken);
        return Ok(result.Value);
    }

    /// <summary>
    /// Revoke a specific active session.
    /// </summary>
    [HttpDelete("me/sessions/{sessionId:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RevokeSession(Guid sessionId, CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        var result = await _userService.RevokeSessionAsync(userId, sessionId, cancellationToken);
        if (!result.IsSuccess)
        {
            return StatusCode(result.StatusCode, new { message = result.Error });
        }
        return Ok(new { message = "Session revoked successfully." });
    }

    /// <summary>
    /// Revoke all sessions for the current user.
    /// </summary>
    [HttpDelete("me/sessions")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> RevokeAllSessions(CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        await _userService.RevokeAllOtherSessionsAsync(userId, null, cancellationToken);
        return Ok(new { message = "All sessions revoked successfully." });
    }

    /// <summary>
    /// Get the current user profile.
    /// </summary>
    [HttpGet("me/profile")]
    [ProducesResponseType(typeof(UserDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetMyProfile(CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        var result = await _userService.GetProfileAsync(userId, cancellationToken);
        if (!result.IsSuccess)
        {
            return StatusCode(result.StatusCode, new { message = result.Error });
        }
        return Ok(result.Value);
    }

    /// <summary>
    /// Update the current user profile name.
    /// </summary>
    [HttpPut("me/profile")]
    [ProducesResponseType(typeof(UserDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> UpdateMyProfile(
        [FromBody] UpdateProfileRequest request,
        [FromServices] IValidator<UpdateProfileRequest> validator,
        CancellationToken cancellationToken)
    {
        var validation = await validator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            return BadRequest(new { errors = validation.Errors.Select(e => e.ErrorMessage) });
        }

        var userId = GetCurrentUserId();
        var result = await _userService.UpdateProfileAsync(userId, request, cancellationToken);
        if (!result.IsSuccess)
        {
            return StatusCode(result.StatusCode, new { message = result.Error });
        }
        return Ok(result.Value);
    }

    /// <summary>
    /// Change password for the current user.
    /// </summary>
    [HttpPut("me/password")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ChangePassword(
        [FromBody] ChangePasswordRequest request,
        [FromServices] IValidator<ChangePasswordRequest> validator,
        CancellationToken cancellationToken)
    {
        var validation = await validator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            return BadRequest(new { errors = validation.Errors.Select(e => e.ErrorMessage) });
        }

        var userId = GetCurrentUserId();
        var result = await _userService.ChangePasswordAsync(userId, request, cancellationToken);
        if (!result.IsSuccess)
        {
            return StatusCode(result.StatusCode, new { message = result.Error });
        }
        return Ok(new { message = "Password changed successfully. Please log in again on other devices." });
    }

    /// <summary>
    /// List all registered users (Admin or Recruiter).
    /// </summary>
    [HttpGet]
    [Authorize(Roles = "Admin,Recruiter")]
    [ProducesResponseType(typeof(PagedResult<UserDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetUsers(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? search = null,
        [FromQuery] string? role = null,
        [FromQuery] bool? isActive = null,
        CancellationToken cancellationToken = default)
    {
        var result = await _userService.GetUsersPagedAsync(page, pageSize, search, role, isActive, cancellationToken);
        return Ok(result.Value);
    }

    /// <summary>
    /// Get single user details by ID (Admin or Recruiter).
    /// </summary>
    [HttpGet("{userId:guid}")]
    [Authorize(Roles = "Admin,Recruiter")]
    [ProducesResponseType(typeof(UserDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetUserById(Guid userId, CancellationToken cancellationToken)
    {
        var result = await _userService.GetUserByIdAsync(userId, cancellationToken);
        if (!result.IsSuccess)
        {
            return StatusCode(result.StatusCode, new { message = result.Error });
        }
        return Ok(result.Value);
    }

    /// <summary>
    /// Activate or deactivate user account (Admin only).
    /// </summary>
    [HttpPatch("{userId:guid}/status")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateUserStatus(
        Guid userId,
        [FromBody] UpdateUserStatusRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _userService.UpdateUserStatusAsync(userId, request.IsActive, cancellationToken);
        if (!result.IsSuccess)
        {
            return StatusCode(result.StatusCode, new { message = result.Error });
        }
        return Ok(new { message = $"User status successfully updated to {(request.IsActive ? "Active" : "Inactive")}." });
    }

    /// <summary>
    /// Assign or change user roles (Admin only).
    /// </summary>
    [HttpPut("{userId:guid}/roles")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateUserRoles(
        Guid userId,
        [FromBody] UpdateUserRolesRequest request,
        [FromServices] IValidator<UpdateUserRolesRequest> validator,
        CancellationToken cancellationToken)
    {
        var validation = await validator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            return BadRequest(new { errors = validation.Errors.Select(e => e.ErrorMessage) });
        }

        var result = await _userService.UpdateUserRolesAsync(userId, request.Roles, cancellationToken);
        if (!result.IsSuccess)
        {
            return StatusCode(result.StatusCode, new { message = result.Error });
        }
        return Ok(new { message = "User roles updated successfully." });
    }

    private Guid GetCurrentUserId()
    {
        var sub = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
        return Guid.TryParse(sub, out var id) ? id : Guid.Empty;
    }
}

