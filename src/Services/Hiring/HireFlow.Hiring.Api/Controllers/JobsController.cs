using System.Security.Claims;
using BuildingBlocks.Infrastructure.RateLimiting;
using FluentValidation;
using HireFlow.Hiring.Application.Common;
using HireFlow.Hiring.Application.DTOs;
using HireFlow.Hiring.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HireFlow.Hiring.Api.Controllers;

[ApiController]
[Route("api/v1/jobs")]
[Produces("application/json")]
public class JobsController : ControllerBase
{
    private readonly IJobService _jobService;
    private readonly IApplicationService _applicationService;

    public JobsController(IJobService jobService, IApplicationService applicationService)
    {
        _jobService = jobService;
        _applicationService = applicationService;
    }

    /// <summary>
    /// List jobs with optional filters (search, status, companyId) and pagination.
    /// </summary>
    [HttpGet]
    [AllowAnonymous]
    [RateLimit(Policy = "hiring-job-search", MaxRequests = 60, WindowSeconds = 60)]
    [ProducesResponseType(typeof(PagedResult<JobDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    public async Task<IActionResult> GetJobs(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string? search = null,
        [FromQuery] string? status = null,
        [FromQuery] Guid? companyId = null,
        CancellationToken cancellationToken = default)
    {
        var result = await _jobService.GetJobsPagedAsync(page, pageSize, search, status, companyId, cancellationToken);
        return Ok(result.Value);
    }

    /// <summary>
    /// Get job details by ID.
    /// </summary>
    [HttpGet("{jobId:guid}")]
    [AllowAnonymous]
    [RateLimit(Policy = "hiring-job-details", MaxRequests = 120, WindowSeconds = 60)]
    [ProducesResponseType(typeof(JobDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    public async Task<IActionResult> GetJobById(Guid jobId, CancellationToken cancellationToken)
    {
        var result = await _jobService.GetJobByIdAsync(jobId, cancellationToken);
        if (!result.IsSuccess)
        {
            return StatusCode(result.StatusCode, new { message = result.Error });
        }
        return Ok(result.Value);
    }

    /// <summary>
    /// Create a new job posting.
    /// </summary>
    [HttpPost]
    [Authorize(Roles = "Admin,Recruiter,HiringManager")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateJob(
        [FromBody] CreateJobRequest request,
        [FromServices] IValidator<CreateJobRequest> validator,
        CancellationToken cancellationToken)
    {
        var validation = await validator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            return BadRequest(new { errors = validation.Errors.Select(e => e.ErrorMessage) });
        }

        var userId = GetCurrentUserId();
        var result = await _jobService.CreateJobAsync(request, userId, cancellationToken);
        if (!result.IsSuccess)
        {
            return StatusCode(result.StatusCode, new { message = result.Error });
        }

        return CreatedAtAction(nameof(GetJobById), new { jobId = result.Value }, new { id = result.Value, message = "Job created successfully." });
    }

    /// <summary>
    /// Update an existing job.
    /// </summary>
    [HttpPut("{jobId:guid}")]
    [Authorize(Roles = "Admin,Recruiter,HiringManager")]
    [ProducesResponseType(typeof(JobDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateJob(
        Guid jobId,
        [FromBody] UpdateJobRequest request,
        [FromServices] IValidator<UpdateJobRequest> validator,
        CancellationToken cancellationToken)
    {
        var validation = await validator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            return BadRequest(new { errors = validation.Errors.Select(e => e.ErrorMessage) });
        }

        var userId = GetCurrentUserId();
        var isAdmin = User.IsInRole("Admin");
        var result = await _jobService.UpdateJobAsync(jobId, request, userId, isAdmin, cancellationToken);
        if (!result.IsSuccess)
        {
            return StatusCode(result.StatusCode, new { message = result.Error });
        }

        return Ok(result.Value);
    }

    /// <summary>
    /// Publish a draft job to make it active and visible.
    /// </summary>
    [HttpPatch("{jobId:guid}/publish")]
    [Authorize(Roles = "Admin,Recruiter,HiringManager")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> PublishJob(Guid jobId, CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        var isAdmin = User.IsInRole("Admin");
        var result = await _jobService.PublishJobAsync(jobId, userId, isAdmin, cancellationToken);
        if (!result.IsSuccess)
        {
            return StatusCode(result.StatusCode, new { message = result.Error });
        }

        return Ok(new { message = "Job published successfully." });
    }

    /// <summary>
    /// Close a job posting.
    /// </summary>
    [HttpPatch("{jobId:guid}/close")]
    [Authorize(Roles = "Admin,Recruiter,HiringManager")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CloseJob(Guid jobId, CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        var isAdmin = User.IsInRole("Admin");
        var result = await _jobService.CloseJobAsync(jobId, userId, isAdmin, cancellationToken);
        if (!result.IsSuccess)
        {
            return StatusCode(result.StatusCode, new { message = result.Error });
        }

        return Ok(new { message = "Job closed successfully." });
    }

    /// <summary>
    /// Delete a job posting.
    /// </summary>
    [HttpDelete("{jobId:guid}")]
    [Authorize(Roles = "Admin,Recruiter,HiringManager")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteJob(Guid jobId, CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        var isAdmin = User.IsInRole("Admin");
        var result = await _jobService.DeleteJobAsync(jobId, userId, isAdmin, cancellationToken);
        if (!result.IsSuccess)
        {
            return StatusCode(result.StatusCode, new { message = result.Error });
        }

        return Ok(new { message = "Job deleted successfully." });
    }

    /// <summary>
    /// Get application breakdown and statistics for a job.
    /// </summary>
    [HttpGet("{jobId:guid}/statistics")]
    [Authorize(Roles = "Admin,Recruiter,HiringManager")]
    [ProducesResponseType(typeof(JobStatisticsDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetJobStatistics(Guid jobId, CancellationToken cancellationToken)
    {
        var result = await _jobService.GetJobStatisticsAsync(jobId, cancellationToken);
        if (!result.IsSuccess)
        {
            return StatusCode(result.StatusCode, new { message = result.Error });
        }

        return Ok(result.Value);
    }

    /// <summary>
    /// Get all applications submitted for a job.
    /// </summary>
    [HttpGet("{jobId:guid}/applications")]
    [Authorize(Roles = "Admin,Recruiter,HiringManager")]
    [ProducesResponseType(typeof(PagedResult<JobApplicationDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetJobApplications(
        Guid jobId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string? status = null,
        CancellationToken cancellationToken = default)
    {
        var result = await _applicationService.GetJobApplicationsAsync(jobId, page, pageSize, status, cancellationToken);
        return Ok(result.Value);
    }

    private Guid GetCurrentUserId()
    {
        var sub = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
        return Guid.TryParse(sub, out var id) ? id : Guid.Empty;
    }
}
