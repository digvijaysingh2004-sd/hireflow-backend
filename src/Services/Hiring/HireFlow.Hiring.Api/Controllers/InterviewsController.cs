using System.Security.Claims;
using BuildingBlocks.Infrastructure.Idempotency;
using FluentValidation;
using HireFlow.Hiring.Application.Common;
using HireFlow.Hiring.Application.DTOs;
using HireFlow.Hiring.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HireFlow.Hiring.Api.Controllers;

[ApiController]
[Produces("application/json")]
public class InterviewsController : ControllerBase
{
    private readonly IInterviewService _interviewService;

    public InterviewsController(IInterviewService interviewService)
    {
        _interviewService = interviewService;
    }

    /// <summary>
    /// Schedule an interview for an application.
    /// </summary>
    [HttpPost("api/v1/applications/{applicationId:guid}/interviews")]
    [Authorize(Roles = "Admin,Recruiter,HiringManager")]
    [Idempotent]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ScheduleInterview(
        Guid applicationId,
        [FromBody] ScheduleInterviewRequest request,
        [FromServices] IValidator<ScheduleInterviewRequest> validator,
        CancellationToken cancellationToken)
    {
        var validation = await validator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            return BadRequest(new { errors = validation.Errors.Select(e => e.ErrorMessage) });
        }

        var scheduledByUserId = GetCurrentUserId();
        var result = await _interviewService.ScheduleInterviewAsync(applicationId, request, scheduledByUserId, cancellationToken);
        if (!result.IsSuccess)
        {
            return StatusCode(result.StatusCode, new { message = result.Error });
        }

        return CreatedAtAction(nameof(GetInterviewById), new { interviewId = result.Value }, new { id = result.Value, message = "Interview scheduled successfully." });
    }

    /// <summary>
    /// List interviews (Recruiter, HiringManager, Admin).
    /// </summary>
    [HttpGet("api/v1/interviews")]
    [Authorize(Roles = "Admin,Recruiter,HiringManager")]
    [ProducesResponseType(typeof(PagedResult<InterviewDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetInterviews(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string? status = null,
        CancellationToken cancellationToken = default)
    {
        var result = await _interviewService.GetInterviewsPagedAsync(page, pageSize, status, cancellationToken);
        return Ok(result.Value);
    }

    /// <summary>
    /// Get currently logged-in candidate's upcoming and historical interviews.
    /// </summary>
    [HttpGet("api/v1/me/interviews")]
    [Authorize]
    [ProducesResponseType(typeof(PagedResult<InterviewDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetMyInterviews(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        CancellationToken cancellationToken = default)
    {
        var candidateUserId = GetCurrentUserId();
        var result = await _interviewService.GetCandidateInterviewsAsync(candidateUserId, page, pageSize, cancellationToken);
        return Ok(result.Value);
    }

    /// <summary>
    /// Get details of an interview by ID.
    /// </summary>
    [HttpGet("api/v1/interviews/{interviewId:guid}")]
    [Authorize]
    [ProducesResponseType(typeof(InterviewDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetInterviewById(Guid interviewId, CancellationToken cancellationToken)
    {
        var currentUserId = GetCurrentUserId();
        var isElevated = IsElevatedUser();
        var result = await _interviewService.GetInterviewByIdAsync(interviewId, currentUserId, isElevated, cancellationToken);
        if (!result.IsSuccess)
        {
            return StatusCode(result.StatusCode, new { message = result.Error });
        }

        return Ok(result.Value);
    }

    /// <summary>
    /// Reschedule an interview.
    /// </summary>
    [HttpPatch("api/v1/interviews/{interviewId:guid}")]
    [Authorize(Roles = "Admin,Recruiter,HiringManager")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RescheduleInterview(
        Guid interviewId,
        [FromBody] RescheduleInterviewRequest request,
        [FromServices] IValidator<RescheduleInterviewRequest> validator,
        CancellationToken cancellationToken)
    {
        var validation = await validator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            return BadRequest(new { errors = validation.Errors.Select(e => e.ErrorMessage) });
        }

        var currentUserId = GetCurrentUserId();
        var isElevated = IsElevatedUser();
        var result = await _interviewService.RescheduleInterviewAsync(interviewId, request, currentUserId, isElevated, cancellationToken);
        if (!result.IsSuccess)
        {
            return StatusCode(result.StatusCode, new { message = result.Error });
        }

        return Ok(new { message = "Interview rescheduled successfully." });
    }

    /// <summary>
    /// Update interview outcome/status (Scheduled, Completed, Cancelled).
    /// </summary>
    [HttpPatch("api/v1/interviews/{interviewId:guid}/status")]
    [Authorize(Roles = "Admin,Recruiter,HiringManager")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateInterviewStatus(
        Guid interviewId,
        [FromBody] UpdateInterviewStatusRequest request,
        [FromServices] IValidator<UpdateInterviewStatusRequest> validator,
        CancellationToken cancellationToken)
    {
        var validation = await validator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            return BadRequest(new { errors = validation.Errors.Select(e => e.ErrorMessage) });
        }

        var currentUserId = GetCurrentUserId();
        var isElevated = IsElevatedUser();
        var result = await _interviewService.UpdateInterviewStatusAsync(interviewId, request, currentUserId, isElevated, cancellationToken);
        if (!result.IsSuccess)
        {
            return StatusCode(result.StatusCode, new { message = result.Error });
        }

        return Ok(new { message = $"Interview status updated to '{request.Status}' successfully." });
    }

    /// <summary>
    /// Cancel an interview.
    /// </summary>
    [HttpDelete("api/v1/interviews/{interviewId:guid}")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CancelInterview(Guid interviewId, CancellationToken cancellationToken)
    {
        var currentUserId = GetCurrentUserId();
        var isElevated = IsElevatedUser();
        var result = await _interviewService.CancelInterviewAsync(interviewId, currentUserId, isElevated, cancellationToken);
        if (!result.IsSuccess)
        {
            return StatusCode(result.StatusCode, new { message = result.Error });
        }

        return Ok(new { message = "Interview cancelled successfully." });
    }

    private Guid GetCurrentUserId()
    {
        var sub = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
        return Guid.TryParse(sub, out var id) ? id : Guid.Empty;
    }

    private bool IsElevatedUser()
    {
        return User.IsInRole("Admin") || User.IsInRole("Recruiter") || User.IsInRole("HiringManager");
    }
}
