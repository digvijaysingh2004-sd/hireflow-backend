using FluentValidation;
using HireFlow.Notification.Application.DTOs;
using HireFlow.Notification.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HireFlow.Notification.Api.Controllers;

[ApiController]
[Route("api/v1/templates")]
[Produces("application/json")]
public class TemplatesController : ControllerBase
{
    private readonly ITemplateService _templateService;

    public TemplatesController(ITemplateService templateService)
    {
        _templateService = templateService;
    }

    /// <summary>
    /// Create a new email template (Admin only).
    /// </summary>
    [HttpPost]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(TemplateDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> CreateTemplate(
        [FromBody] CreateTemplateRequest request,
        [FromServices] IValidator<CreateTemplateRequest> validator,
        CancellationToken cancellationToken)
    {
        var validation = await validator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            return BadRequest(new { errors = validation.Errors.Select(e => e.ErrorMessage) });
        }

        var result = await _templateService.CreateTemplateAsync(request, cancellationToken);
        if (!result.IsSuccess)
        {
            return StatusCode(result.StatusCode, new { message = result.Error });
        }

        return CreatedAtAction(nameof(GetTemplateById), new { templateId = result.Value!.Id }, result.Value);
    }

    /// <summary>
    /// List email templates with pagination and search.
    /// </summary>
    [HttpGet]
    [Authorize(Roles = "Admin,Recruiter,HiringManager")]
    [ProducesResponseType(typeof(PagedResult<TemplateDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetTemplates(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? search = null,
        [FromQuery] bool? activeOnly = null,
        CancellationToken cancellationToken = default)
    {
        var result = await _templateService.GetTemplatesPagedAsync(page, pageSize, search, activeOnly, cancellationToken);
        return Ok(result.Value);
    }

    /// <summary>
    /// Get details of an email template by ID.
    /// </summary>
    [HttpGet("{templateId:guid}")]
    [Authorize(Roles = "Admin,Recruiter,HiringManager")]
    [ProducesResponseType(typeof(TemplateDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetTemplateById(Guid templateId, CancellationToken cancellationToken)
    {
        var result = await _templateService.GetTemplateByIdAsync(templateId, cancellationToken);
        if (!result.IsSuccess)
        {
            return StatusCode(result.StatusCode, new { message = result.Error });
        }

        return Ok(result.Value);
    }

    /// <summary>
    /// Update an email template (Admin only).
    /// </summary>
    [HttpPut("{templateId:guid}")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(TemplateDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateTemplate(
        Guid templateId,
        [FromBody] UpdateTemplateRequest request,
        [FromServices] IValidator<UpdateTemplateRequest> validator,
        CancellationToken cancellationToken)
    {
        var validation = await validator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            return BadRequest(new { errors = validation.Errors.Select(e => e.ErrorMessage) });
        }

        var result = await _templateService.UpdateTemplateAsync(templateId, request, cancellationToken);
        if (!result.IsSuccess)
        {
            return StatusCode(result.StatusCode, new { message = result.Error });
        }

        return Ok(result.Value);
    }

    /// <summary>
    /// Deactivate an email template (Admin only).
    /// </summary>
    [HttpDelete("{templateId:guid}")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeactivateTemplate(Guid templateId, CancellationToken cancellationToken)
    {
        var result = await _templateService.DeactivateTemplateAsync(templateId, cancellationToken);
        if (!result.IsSuccess)
        {
            return StatusCode(result.StatusCode, new { message = result.Error });
        }

        return NoContent();
    }
}
