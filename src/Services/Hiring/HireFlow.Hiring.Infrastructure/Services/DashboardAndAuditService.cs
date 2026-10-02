using HireFlow.Hiring.Application.Common;
using HireFlow.Hiring.Application.DTOs;
using HireFlow.Hiring.Application.Interfaces;
using HireFlow.Hiring.Domain.Entities;
using HireFlow.Hiring.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace HireFlow.Hiring.Infrastructure.Services;

public class DashboardService : IDashboardService
{
    private readonly HiringDbContext _context;

    public DashboardService(HiringDbContext context)
    {
        _context = context;
    }

    public async Task<Result<DashboardSummaryDto>> GetDashboardSummaryAsync(CancellationToken cancellationToken = default)
    {
        var totalActiveJobs = await _context.Jobs
            .AsNoTracking()
            .CountAsync(j => j.Status == "Published" || j.Status == "Active", cancellationToken);

        var totalApplications = await _context.Applications
            .AsNoTracking()
            .CountAsync(cancellationToken);

        var pendingReviews = await _context.Applications
            .AsNoTracking()
            .CountAsync(a => a.Status == "Applied" || a.Status == "Screening", cancellationToken);

        var scheduledInterviews = await _context.Interviews
            .AsNoTracking()
            .CountAsync(i => i.Status == "Scheduled", cancellationToken);

        var summary = new DashboardSummaryDto(
            totalActiveJobs,
            totalApplications,
            pendingReviews,
            scheduledInterviews
        );

        return Result<DashboardSummaryDto>.Success(summary);
    }
}

public class AuditService : IAuditService
{
    private readonly HiringDbContext _context;

    public AuditService(HiringDbContext context)
    {
        _context = context;
    }

    public async Task<Result<PagedResult<AuditLogDto>>> GetAuditLogsPagedAsync(
        int page,
        int pageSize,
        string? entityType,
        string? action,
        CancellationToken cancellationToken = default)
    {
        if (page < 1) page = 1;
        if (pageSize < 1) pageSize = 10;
        if (pageSize > 100) pageSize = 100;

        var query = _context.AuditLogs.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(entityType))
        {
            var normalized = entityType.Trim().ToLower();
            query = query.Where(a => a.EntityType.ToLower() == normalized);
        }

        if (!string.IsNullOrWhiteSpace(action))
        {
            var normalized = action.Trim().ToLower();
            query = query.Where(a => a.Action.ToLower() == normalized);
        }

        query = query.OrderByDescending(a => a.CreatedAtUtc);

        var totalItems = await query.CountAsync(cancellationToken);
        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(a => new AuditLogDto(
                a.Id,
                a.ActorUserId,
                a.Action,
                a.EntityType,
                a.EntityId,
                a.OldValuesJson,
                a.NewValuesJson,
                a.CorrelationId,
                a.IpAddress,
                a.CreatedAtUtc
            ))
            .ToListAsync(cancellationToken);

        return Result<PagedResult<AuditLogDto>>.Success(PagedResult<AuditLogDto>.Create(items, page, pageSize, totalItems));
    }

    public async Task<Result<AuditLogDto>> GetAuditLogByIdAsync(Guid auditId, CancellationToken cancellationToken = default)
    {
        var log = await _context.AuditLogs
            .AsNoTracking()
            .FirstOrDefaultAsync(a => a.Id == auditId, cancellationToken);

        if (log == null)
        {
            return Result<AuditLogDto>.Failure("Audit log not found.", 404);
        }

        return Result<AuditLogDto>.Success(new AuditLogDto(
            log.Id,
            log.ActorUserId,
            log.Action,
            log.EntityType,
            log.EntityId,
            log.OldValuesJson,
            log.NewValuesJson,
            log.CorrelationId,
            log.IpAddress,
            log.CreatedAtUtc
        ));
    }
}
