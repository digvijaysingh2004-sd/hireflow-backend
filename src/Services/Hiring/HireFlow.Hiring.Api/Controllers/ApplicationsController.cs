using System.Security.Claims;
using FluentValidation;
using HireFlow.Hiring.Application.Common;
using HireFlow.Hiring.Application.DTOs;
using HireFlow.Hiring.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HireFlow.Hiring.Api.Controllers;

[ApiController]
[Produces("application/json")]
public class ApplicationsController : ControllerBase
{
    private readonly IApplicationService _applicationService;

    public ApplicationsController(IApplicationService applicationService)
    {
        _applicationService = applicationService;
    }

    /// <summary>
    /// Apply for a job opening.
    /// </summary>
    [HttpPost("api/v1/jobs/{jobId:guid}/applications")]
    [Authorize(Roles = "Candidate,Admin")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> ApplyToJob(
        Guid jobId,
        [FromBody] ApplyJobRequest request,
        [FromServices] IValidator<ApplyJobRequest> validator,
        CancellationToken cancellationToken)
    {
        var validation = await validator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            return BadRequest(new { errors = validation.Errors.Select(e => e.ErrorMessage) });
        }

        var candidateUserId = GetCurrentUserId();
        var result = await _applicationService.ApplyJobAsync(jobId, request, candidateUserId, cancellationToken);
        if (!result.IsSuccess)
        {
            return StatusCode(result.StatusCode, new { message = result.Error });
        }

        return CreatedAtAction(nameof(GetApplicationById), new { applicationId = result.Value }, new { id = result.Value, message = "Application submitted successfully." });
    }

    /// <summary>
    /// Get currently logged-in candidate's job applications.
    /// </summary>
    [HttpGet("api/v1/me/applications")]
    [Authorize]
    [ProducesResponseType(typeof(PagedResult<JobApplicationDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetMyApplications(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        CancellationToken cancellationToken = default)
    {
        var candidateUserId = GetCurrentUserId();
        var result = await _applicationService.GetCandidateApplicationsAsync(candidateUserId, page, pageSize, cancellationToken);
        return Ok(result.Value);
    }

    /// <summary>
    /// Get application details by ID.
    /// </summary>
    [HttpGet("api/v1/applications/{applicationId:guid}")]
    [Authorize]
    [ProducesResponseType(typeof(JobApplicationDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetApplicationById(Guid applicationId, CancellationToken cancellationToken)
    {
        var currentUserId = GetCurrentUserId();
        var isElevated = IsElevatedUser();
        var result = await _applicationService.GetApplicationByIdAsync(applicationId, currentUserId, isElevated, cancellationToken);
        if (!result.IsSuccess)
        {
            return StatusCode(result.StatusCode, new { message = result.Error });
        }

        return Ok(result.Value);
    }

    /// <summary>
    /// Update status of an application (Screening, Interviewing, Offered, Rejected, Hired).
    /// </summary>
    [HttpPatch("api/v1/applications/{applicationId:guid}/status")]
    [Authorize(Roles = "Admin,Recruiter,HiringManager")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateApplicationStatus(
        Guid applicationId,
        [FromBody] UpdateApplicationStatusRequest request,
        [FromServices] IValidator<UpdateApplicationStatusRequest> validator,
        CancellationToken cancellationToken)
    {
        var validation = await validator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            return BadRequest(new { errors = validation.Errors.Select(e => e.ErrorMessage) });
        }

        var currentUserId = GetCurrentUserId();
        var result = await _applicationService.UpdateApplicationStatusAsync(applicationId, request, currentUserId, cancellationToken);
        if (!result.IsSuccess)
        {
            return StatusCode(result.StatusCode, new { message = result.Error });
        }

        return Ok(new { message = $"Application status updated to '{request.Status}' successfully." });
    }

    /// <summary>
    /// Withdraw a submitted application (Candidate).
    /// </summary>
    [HttpPost("api/v1/applications/{applicationId:guid}/withdraw")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> WithdrawApplication(Guid applicationId, CancellationToken cancellationToken)
    {
        var candidateUserId = GetCurrentUserId();
        var result = await _applicationService.WithdrawApplicationAsync(applicationId, candidateUserId, cancellationToken);
        if (!result.IsSuccess)
        {
            return StatusCode(result.StatusCode, new { message = result.Error });
        }

        return Ok(new { message = "Application withdrawn successfully." });
    }

    /// <summary>
    /// Get status change timeline / history for an application.
    /// </summary>
    [HttpGet("api/v1/applications/{applicationId:guid}/history")]
    [Authorize]
    [ProducesResponseType(typeof(IReadOnlyList<ApplicationStatusHistoryDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetApplicationHistory(Guid applicationId, CancellationToken cancellationToken)
    {
        var currentUserId = GetCurrentUserId();
        var isElevated = IsElevatedUser();
        var result = await _applicationService.GetApplicationHistoryAsync(applicationId, currentUserId, isElevated, cancellationToken);
        if (!result.IsSuccess)
        {
            return StatusCode(result.StatusCode, new { message = result.Error });
        }

        return Ok(result.Value);
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
