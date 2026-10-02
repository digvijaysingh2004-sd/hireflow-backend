using HireFlow.Notification.Application.DTOs;
using HireFlow.Notification.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace HireFlow.Notification.Api.Controllers;

[ApiController]
[Route("api/v1/internal/outbox")]
[Produces("application/json")]
public class OutboxController : ControllerBase
{
    private readonly IOutboxService _outboxService;

    public OutboxController(IOutboxService outboxService)
    {
        _outboxService = outboxService;
    }

    /// <summary>
    /// List pending outbox messages for asynchronous event publishing (Internal worker endpoint).
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<OutboxMessageDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetOutboxMessages(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 100,
        [FromQuery] string? status = "Pending",
        CancellationToken cancellationToken = default)
    {
        var result = await _outboxService.GetOutboxMessagesPagedAsync(page, pageSize, status, cancellationToken);
        return Ok(result.Value);
    }

    /// <summary>
    /// Mark an outbox message as published (Internal worker endpoint).
    /// </summary>
    [HttpPost("{messageId:guid}/publish")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> PublishMessage(Guid messageId, CancellationToken cancellationToken)
    {
        var result = await _outboxService.PublishOutboxMessageAsync(messageId, cancellationToken);
        if (!result.IsSuccess)
        {
            return StatusCode(result.StatusCode, new { message = result.Error });
        }

        return NoContent();
    }
}
