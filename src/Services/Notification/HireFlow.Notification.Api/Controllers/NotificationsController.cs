using HireFlow.Notification.Application.DTOs;
using HireFlow.Notification.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HireFlow.Notification.Api.Controllers;

[ApiController]
[Route("api/v1/notifications")]
[Produces("application/json")]
public class NotificationsController : ControllerBase
{
    private readonly IEmailNotificationService _notificationService;

    public NotificationsController(IEmailNotificationService notificationService)
    {
        _notificationService = notificationService;
    }

    /// <summary>
    /// Query paginated notification delivery records (with masked recipient emails).
    /// </summary>
    [HttpGet]
    [Authorize(Roles = "Admin,Recruiter,HiringManager")]
    [ProducesResponseType(typeof(PagedResult<NotificationDeliveryDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetNotifications(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? status = null,
        [FromQuery] string? templateKey = null,
        CancellationToken cancellationToken = default)
    {
        var result = await _notificationService.GetNotificationsPagedAsync(page, pageSize, status, templateKey, cancellationToken);
        return Ok(result.Value);
    }

    /// <summary>
    /// Get delivery details of a specific notification.
    /// </summary>
    [HttpGet("{notificationId:guid}")]
    [Authorize(Roles = "Admin,Recruiter,HiringManager")]
    [ProducesResponseType(typeof(NotificationDeliveryDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetNotificationById(Guid notificationId, CancellationToken cancellationToken)
    {
        var result = await _notificationService.GetNotificationByIdAsync(notificationId, cancellationToken);
        if (!result.IsSuccess)
        {
            return StatusCode(result.StatusCode, new { message = result.Error });
        }

        return Ok(new { data = result.Value, meta = new { traceId = HttpContext.TraceIdentifier } });
    }

    /// <summary>
    /// Retry delivery for a failed notification (Admin only).
    /// </summary>
    [HttpPost("{notificationId:guid}/retry")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(NotificationQueuedResponse), StatusCodes.Status202Accepted)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RetryNotification(
        Guid notificationId,
        [FromBody] RetryNotificationRequest? request,
        CancellationToken cancellationToken)
    {
        var result = await _notificationService.RetryNotificationAsync(notificationId, request?.Reason, cancellationToken);
        if (!result.IsSuccess)
        {
            return StatusCode(result.StatusCode, new { message = result.Error });
        }

        return Accepted(new { data = result.Value, meta = new { traceId = HttpContext.TraceIdentifier } });
    }
}
