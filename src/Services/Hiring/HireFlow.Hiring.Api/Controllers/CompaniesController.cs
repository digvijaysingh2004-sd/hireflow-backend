using System.Security.Claims;
using FluentValidation;
using HireFlow.Hiring.Application.Common;
using HireFlow.Hiring.Application.DTOs;
using HireFlow.Hiring.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HireFlow.Hiring.Api.Controllers;

[ApiController]
[Route("api/v1/companies")]
[Produces("application/json")]
public class CompaniesController : ControllerBase
{
    private readonly ICompanyService _companyService;

    public CompaniesController(ICompanyService companyService)
    {
        _companyService = companyService;
    }

    /// <summary>
    /// List companies with optional search and pagination.
    /// </summary>
    [HttpGet]
    [AllowAnonymous]
    [ProducesResponseType(typeof(PagedResult<CompanyDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetCompanies(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string? search = null,
        CancellationToken cancellationToken = default)
    {
        var result = await _companyService.GetCompaniesPagedAsync(page, pageSize, search, cancellationToken);
        return Ok(result.Value);
    }

    /// <summary>
    /// Get company details by ID.
    /// </summary>
    [HttpGet("{companyId:guid}")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(CompanyDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetCompanyById(Guid companyId, CancellationToken cancellationToken)
    {
        var result = await _companyService.GetCompanyByIdAsync(companyId, cancellationToken);
        if (!result.IsSuccess)
        {
            return StatusCode(result.StatusCode, new { message = result.Error });
        }
        return Ok(result.Value);
    }

    /// <summary>
    /// Create a new company (Recruiter, HiringManager, Admin).
    /// </summary>
    [HttpPost]
    [Authorize(Roles = "Admin,Recruiter,HiringManager")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateCompany(
        [FromBody] CreateCompanyRequest request,
        [FromServices] IValidator<CreateCompanyRequest> validator,
        CancellationToken cancellationToken)
    {
        var validation = await validator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            return BadRequest(new { errors = validation.Errors.Select(e => e.ErrorMessage) });
        }

        var userId = GetCurrentUserId();
        var result = await _companyService.CreateCompanyAsync(request, userId, cancellationToken);
        if (!result.IsSuccess)
        {
            return StatusCode(result.StatusCode, new { message = result.Error });
        }

        return CreatedAtAction(nameof(GetCompanyById), new { companyId = result.Value }, new { id = result.Value, message = "Company created successfully." });
    }

    /// <summary>
    /// Update company details.
    /// </summary>
    [HttpPut("{companyId:guid}")]
    [Authorize(Roles = "Admin,Recruiter,HiringManager")]
    [ProducesResponseType(typeof(CompanyDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateCompany(
        Guid companyId,
        [FromBody] UpdateCompanyRequest request,
        [FromServices] IValidator<UpdateCompanyRequest> validator,
        CancellationToken cancellationToken)
    {
        var validation = await validator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            return BadRequest(new { errors = validation.Errors.Select(e => e.ErrorMessage) });
        }

        var userId = GetCurrentUserId();
        var isAdmin = User.IsInRole("Admin");
        var result = await _companyService.UpdateCompanyAsync(companyId, request, userId, isAdmin, cancellationToken);
        if (!result.IsSuccess)
        {
            return StatusCode(result.StatusCode, new { message = result.Error });
        }

        return Ok(result.Value);
    }

    /// <summary>
    /// Delete a company (Admin only).
    /// </summary>
    [HttpDelete("{companyId:guid}")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteCompany(Guid companyId, CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        var isAdmin = User.IsInRole("Admin");
        var result = await _companyService.DeleteCompanyAsync(companyId, userId, isAdmin, cancellationToken);
        if (!result.IsSuccess)
        {
            return StatusCode(result.StatusCode, new { message = result.Error });
        }

        return Ok(new { message = "Company deleted successfully." });
    }

    private Guid GetCurrentUserId()
    {
        var sub = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
        return Guid.TryParse(sub, out var id) ? id : Guid.Empty;
    }
}
