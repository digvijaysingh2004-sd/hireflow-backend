using HireFlow.Hiring.Application.DTOs;
using HireFlow.Hiring.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HireFlow.Hiring.Api.Controllers;

[ApiController]
[Route("api/v1/dashboard")]
[Produces("application/json")]
[Authorize(Roles = "Admin,Recruiter,HiringManager")]
public class DashboardController : ControllerBase
{
    private readonly IDashboardService _dashboardService;

    public DashboardController(IDashboardService dashboardService)
    {
        _dashboardService = dashboardService;
    }

    /// <summary>
    /// Get high-level hiring pipeline dashboard metrics (Active jobs, total applications, pending reviews, scheduled interviews).
    /// </summary>
    [HttpGet("summary")]
    [ProducesResponseType(typeof(DashboardSummaryDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetSummary(CancellationToken cancellationToken)
    {
        var result = await _dashboardService.GetDashboardSummaryAsync(cancellationToken);
        return Ok(result.Value);
    }
}
