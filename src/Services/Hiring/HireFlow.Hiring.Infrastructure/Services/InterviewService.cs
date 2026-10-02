using HireFlow.Hiring.Application.Common;
using HireFlow.Hiring.Application.DTOs;
using HireFlow.Hiring.Application.Interfaces;
using HireFlow.Hiring.Domain.Entities;
using HireFlow.Hiring.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace HireFlow.Hiring.Infrastructure.Services;

public class InterviewService : IInterviewService
{
    private readonly HiringDbContext _context;

    public InterviewService(HiringDbContext context)
    {
        _context = context;
    }

    public async Task<Result<Guid>> ScheduleInterviewAsync(
        Guid applicationId,
        ScheduleInterviewRequest request,
        Guid scheduledByUserId,
        CancellationToken cancellationToken = default)
    {
        if (request.EndsAtUtc <= request.StartsAtUtc)
        {
            return Result<Guid>.Failure("Interview end time must be after the start time.");
        }

        var application = await _context.Applications
            .Include(a => a.Job)
            .FirstOrDefaultAsync(a => a.Id == applicationId, cancellationToken);

        if (application == null)
        {
            return Result<Guid>.Failure("Application not found.", 404);
        }

        var interview = new Interview
        {
            ApplicationId = applicationId,
            ScheduledByUserId = scheduledByUserId,
            StartsAtUtc = request.StartsAtUtc,
            EndsAtUtc = request.EndsAtUtc,
            MeetingUrl = request.MeetingUrl?.Trim(),
            Status = "Scheduled",
            Notes = request.Notes?.Trim(),
            CreatedAtUtc = DateTimeOffset.UtcNow,
            UpdatedAtUtc = DateTimeOffset.UtcNow
        };

        _context.Interviews.Add(interview);

        // Optionally update application status to 'Interviewing' if currently applied or screening
        if (application.Status == "Applied" || application.Status == "Screening")
        {
            var oldStatus = application.Status;
            application.Status = "Interviewing";
            application.UpdatedAtUtc = DateTimeOffset.UtcNow;

            _context.ApplicationStatusHistories.Add(new ApplicationStatusHistory
            {
                ApplicationId = applicationId,
                FromStatus = oldStatus,
                ToStatus = "Interviewing",
                ChangedByUserId = scheduledByUserId,
                Comment = "Interview scheduled",
                ChangedAtUtc = DateTimeOffset.UtcNow
            });
        }

        _context.AuditLogs.Add(new AuditLog
        {
            ActorUserId = scheduledByUserId,
            Action = "SCHEDULE_INTERVIEW",
            EntityType = "Interview",
            EntityId = interview.Id.ToString(),
            NewValuesJson = JsonSerializer.Serialize(new
            {
                interview.ApplicationId,
                interview.StartsAtUtc,
                interview.EndsAtUtc,
                interview.MeetingUrl,
                interview.Status
            }),
            CreatedAtUtc = DateTimeOffset.UtcNow
        });

        await _context.SaveChangesAsync(cancellationToken);
        return Result<Guid>.Success(interview.Id);
    }

    public async Task<Result<PagedResult<InterviewDto>>> GetCandidateInterviewsAsync(
        Guid candidateUserId,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        if (page < 1) page = 1;
        if (pageSize < 1) pageSize = 10;
        if (pageSize > 100) pageSize = 100;

        var query = _context.Interviews
            .AsNoTracking()
            .Include(i => i.Application)
                .ThenInclude(a => a.Job)
                    .ThenInclude(j => j.Company)
            .Where(i => i.Application.CandidateUserId == candidateUserId)
            .OrderByDescending(i => i.StartsAtUtc);

        var totalItems = await query.CountAsync(cancellationToken);
        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(i => MapToDto(i))
            .ToListAsync(cancellationToken);

        return Result<PagedResult<InterviewDto>>.Success(PagedResult<InterviewDto>.Create(items, page, pageSize, totalItems));
    }

    public async Task<Result<PagedResult<InterviewDto>>> GetInterviewsPagedAsync(
        int page,
        int pageSize,
        string? status,
        CancellationToken cancellationToken = default)
    {
        if (page < 1) page = 1;
        if (pageSize < 1) pageSize = 10;
        if (pageSize > 100) pageSize = 100;

        var query = _context.Interviews
            .AsNoTracking()
            .Include(i => i.Application)
                .ThenInclude(a => a.Job)
                    .ThenInclude(j => j.Company)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(status))
        {
            var normalizedStatus = status.Trim().ToLower();
            query = query.Where(i => i.Status.ToLower() == normalizedStatus);
        }

        query = query.OrderByDescending(i => i.StartsAtUtc);

        var totalItems = await query.CountAsync(cancellationToken);
        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(i => MapToDto(i))
            .ToListAsync(cancellationToken);

        return Result<PagedResult<InterviewDto>>.Success(PagedResult<InterviewDto>.Create(items, page, pageSize, totalItems));
    }

    public async Task<Result<InterviewDto>> GetInterviewByIdAsync(
        Guid interviewId,
        Guid currentUserId,
        bool isElevated,
        CancellationToken cancellationToken = default)
    {
        var interview = await _context.Interviews
            .AsNoTracking()
            .Include(i => i.Application)
                .ThenInclude(a => a.Job)
                    .ThenInclude(j => j.Company)
            .FirstOrDefaultAsync(i => i.Id == interviewId, cancellationToken);

        if (interview == null)
        {
            return Result<InterviewDto>.Failure("Interview not found.", 404);
        }

        if (!isElevated && interview.Application.CandidateUserId != currentUserId)
        {
            return Result<InterviewDto>.Failure("You are not authorized to view this interview.", 403);
        }

        return Result<InterviewDto>.Success(MapToDto(interview));
    }

    public async Task<Result<bool>> RescheduleInterviewAsync(
        Guid interviewId,
        RescheduleInterviewRequest request,
        Guid currentUserId,
        bool isElevated,
        CancellationToken cancellationToken = default)
    {
        if (request.EndsAtUtc <= request.StartsAtUtc)
        {
            return Result<bool>.Failure("Interview end time must be after the start time.");
        }

        var interview = await _context.Interviews
            .Include(i => i.Application)
            .FirstOrDefaultAsync(i => i.Id == interviewId, cancellationToken);

        if (interview == null)
        {
            return Result<bool>.Failure("Interview not found.", 404);
        }

        if (!isElevated && interview.ScheduledByUserId != currentUserId)
        {
            return Result<bool>.Failure("You are not authorized to reschedule this interview.", 403);
        }

        var oldValues = JsonSerializer.Serialize(new
        {
            interview.StartsAtUtc,
            interview.EndsAtUtc,
            interview.MeetingUrl,
            interview.Notes,
            interview.Status
        });

        interview.StartsAtUtc = request.StartsAtUtc;
        interview.EndsAtUtc = request.EndsAtUtc;
        if (!string.IsNullOrWhiteSpace(request.MeetingUrl)) interview.MeetingUrl = request.MeetingUrl.Trim();
        if (request.Notes != null) interview.Notes = request.Notes.Trim();
        interview.Status = "Rescheduled";
        interview.UpdatedAtUtc = DateTimeOffset.UtcNow;

        _context.AuditLogs.Add(new AuditLog
        {
            ActorUserId = currentUserId,
            Action = "RESCHEDULE_INTERVIEW",
            EntityType = "Interview",
            EntityId = interview.Id.ToString(),
            OldValuesJson = oldValues,
            NewValuesJson = JsonSerializer.Serialize(new
            {
                interview.StartsAtUtc,
                interview.EndsAtUtc,
                interview.MeetingUrl,
                interview.Notes,
                interview.Status
            }),
            CreatedAtUtc = DateTimeOffset.UtcNow
        });

        await _context.SaveChangesAsync(cancellationToken);
        return Result<bool>.Success(true);
    }

    public async Task<Result<bool>> UpdateInterviewStatusAsync(
        Guid interviewId,
        UpdateInterviewStatusRequest request,
        Guid currentUserId,
        bool isElevated,
        CancellationToken cancellationToken = default)
    {
        var validStatuses = new[] { "Scheduled", "Completed", "Cancelled", "Rescheduled" };
        if (!validStatuses.Any(s => s.Equals(request.Status, StringComparison.OrdinalIgnoreCase)))
        {
            return Result<bool>.Failure($"Invalid status '{request.Status}'. Valid statuses are: {string.Join(", ", validStatuses)}");
        }

        var interview = await _context.Interviews
            .Include(i => i.Application)
            .FirstOrDefaultAsync(i => i.Id == interviewId, cancellationToken);

        if (interview == null)
        {
            return Result<bool>.Failure("Interview not found.", 404);
        }

        if (!isElevated && interview.ScheduledByUserId != currentUserId)
        {
            return Result<bool>.Failure("You are not authorized to update this interview status.", 403);
        }

        var oldValues = JsonSerializer.Serialize(new { interview.Status, interview.Notes });

        interview.Status = request.Status;
        if (request.Notes != null) interview.Notes = request.Notes.Trim();
        interview.UpdatedAtUtc = DateTimeOffset.UtcNow;

        _context.AuditLogs.Add(new AuditLog
        {
            ActorUserId = currentUserId,
            Action = "UPDATE_INTERVIEW_STATUS",
            EntityType = "Interview",
            EntityId = interview.Id.ToString(),
            OldValuesJson = oldValues,
            NewValuesJson = JsonSerializer.Serialize(new { interview.Status, interview.Notes }),
            CreatedAtUtc = DateTimeOffset.UtcNow
        });

        await _context.SaveChangesAsync(cancellationToken);
        return Result<bool>.Success(true);
    }

    public async Task<Result<bool>> CancelInterviewAsync(
        Guid interviewId,
        Guid currentUserId,
        bool isElevated,
        CancellationToken cancellationToken = default)
    {
        var interview = await _context.Interviews
            .Include(i => i.Application)
            .FirstOrDefaultAsync(i => i.Id == interviewId, cancellationToken);

        if (interview == null)
        {
            return Result<bool>.Failure("Interview not found.", 404);
        }

        if (!isElevated && interview.ScheduledByUserId != currentUserId && interview.Application.CandidateUserId != currentUserId)
        {
            return Result<bool>.Failure("You are not authorized to cancel this interview.", 403);
        }

        var oldStatus = interview.Status;
        interview.Status = "Cancelled";
        interview.UpdatedAtUtc = DateTimeOffset.UtcNow;

        _context.AuditLogs.Add(new AuditLog
        {
            ActorUserId = currentUserId,
            Action = "CANCEL_INTERVIEW",
            EntityType = "Interview",
            EntityId = interview.Id.ToString(),
            OldValuesJson = JsonSerializer.Serialize(new { Status = oldStatus }),
            NewValuesJson = JsonSerializer.Serialize(new { Status = "Cancelled" }),
            CreatedAtUtc = DateTimeOffset.UtcNow
        });

        await _context.SaveChangesAsync(cancellationToken);
        return Result<bool>.Success(true);
    }

    private static InterviewDto MapToDto(Interview i)
    {
        return new InterviewDto(
            i.Id,
            i.ApplicationId,
            i.Application?.CandidateUserId ?? Guid.Empty,
            i.Application?.Job?.Title ?? "Unknown Job",
            i.Application?.Job?.Company?.Name ?? "Unknown Company",
            i.ScheduledByUserId,
            i.StartsAtUtc,
            i.EndsAtUtc,
            i.MeetingUrl,
            i.Status,
            i.Notes,
            i.CreatedAtUtc
        );
    }
}
