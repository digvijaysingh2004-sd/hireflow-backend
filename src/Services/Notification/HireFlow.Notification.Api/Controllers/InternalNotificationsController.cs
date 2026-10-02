using FluentValidation;
using HireFlow.Notification.Application.DTOs;
using HireFlow.Notification.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace HireFlow.Notification.Api.Controllers;

[ApiController]
[Route("api/v1/internal/notifications")]
[Produces("application/json")]
public class InternalNotificationsController : ControllerBase
{
    private readonly IEmailNotificationService _notificationService;

    public InternalNotificationsController(IEmailNotificationService notificationService)
    {
        _notificationService = notificationService;
    }

    /// <summary>
    /// Send an email notification triggered by domain events (Internal service endpoint).
    /// </summary>
    [HttpPost("email")]
    [ProducesResponseType(typeof(NotificationQueuedResponse), StatusCodes.Status202Accepted)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> SendEmail(
        [FromBody] SendEmailNotificationRequest request,
        [FromServices] IValidator<SendEmailNotificationRequest> validator,
        CancellationToken cancellationToken)
    {
        var validation = await validator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            return BadRequest(new { errors = validation.Errors.Select(e => e.ErrorMessage) });
        }

        var result = await _notificationService.SendEmailAsync(request, cancellationToken);
        if (!result.IsSuccess)
        {
            return StatusCode(result.StatusCode, new { message = result.Error });
        }

        return Accepted(new { data = result.Value, meta = new { traceId = HttpContext.TraceIdentifier } });
    }

    /// <summary>
    /// Send a one-time OTP verification code email (Internal identity service endpoint).
    /// </summary>
    [HttpPost("otp")]
    [ProducesResponseType(typeof(NotificationQueuedResponse), StatusCodes.Status202Accepted)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> SendOtp(
        [FromBody] SendOtpNotificationRequest request,
        [FromServices] IValidator<SendOtpNotificationRequest> validator,
        CancellationToken cancellationToken)
    {
        var validation = await validator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            return BadRequest(new { errors = validation.Errors.Select(e => e.ErrorMessage) });
        }

        var result = await _notificationService.SendOtpEmailAsync(request, cancellationToken);
        if (!result.IsSuccess)
        {
            return StatusCode(result.StatusCode, new { message = result.Error });
        }

        return Accepted(new { data = result.Value, meta = new { traceId = HttpContext.TraceIdentifier } });
    }
}
