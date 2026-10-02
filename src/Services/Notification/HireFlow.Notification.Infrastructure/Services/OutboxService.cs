using HireFlow.Notification.Application.Common;
using HireFlow.Notification.Application.DTOs;
using HireFlow.Notification.Application.Interfaces;
using HireFlow.Notification.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace HireFlow.Notification.Infrastructure.Services;

public class OutboxService : IOutboxService
{
    private readonly NotificationDbContext _context;

    public OutboxService(NotificationDbContext context)
    {
        _context = context;
    }

    public async Task<Result<PagedResult<OutboxMessageDto>>> GetOutboxMessagesPagedAsync(
        int page,
        int pageSize,
        string? status,
        CancellationToken cancellationToken = default)
    {
        if (page < 1) page = 1;
        if (pageSize < 1) pageSize = 10;
        if (pageSize > 100) pageSize = 100;

        var query = _context.OutboxMessages.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(status))
        {
            var normalizedStatus = status.Trim().ToLower();
            query = query.Where(o => o.Status.ToLower() == normalizedStatus);
        }

        query = query.OrderByDescending(o => o.CreatedAtUtc);

        var totalItems = await query.CountAsync(cancellationToken);
        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(o => new OutboxMessageDto(
                o.Id,
                o.EventType,
                o.Status,
                o.CreatedAtUtc,
                o.ProcessedAtUtc
            ))
            .ToListAsync(cancellationToken);

        return Result<PagedResult<OutboxMessageDto>>.Success(PagedResult<OutboxMessageDto>.Create(items, page, pageSize, totalItems));
    }

    public async Task<Result<bool>> PublishOutboxMessageAsync(Guid messageId, CancellationToken cancellationToken = default)
    {
        var message = await _context.OutboxMessages
            .FirstOrDefaultAsync(o => o.Id == messageId, cancellationToken);

        if (message == null)
        {
            return Result<bool>.Failure("Outbox message not found.", 404);
        }

        message.Status = "Published";
        message.ProcessedAtUtc = DateTimeOffset.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);
        return Result<bool>.Success(true);
    }
}
