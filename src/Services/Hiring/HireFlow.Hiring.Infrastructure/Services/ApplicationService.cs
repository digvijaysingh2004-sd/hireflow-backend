using HireFlow.Hiring.Application.Common;
using HireFlow.Hiring.Application.DTOs;
using HireFlow.Hiring.Application.Interfaces;
using HireFlow.Hiring.Domain.Entities;
using HireFlow.Hiring.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace HireFlow.Hiring.Infrastructure.Services;

public class ApplicationService : IApplicationService
{
    private readonly HiringDbContext _dbContext;

    public ApplicationService(HiringDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Result<Guid>> ApplyJobAsync(Guid jobId, ApplyJobRequest request, Guid candidateUserId, CancellationToken cancellationToken = default)
    {
        var job = await _dbContext.Jobs
            .FirstOrDefaultAsync(j => j.Id == jobId, cancellationToken);

        if (job == null)
        {
            return Result<Guid>.Failure("Job posting not found.", 404);
        }

        if (job.Status != "Published")
        {
            return Result<Guid>.Failure("Job is not currently accepting applications.", 400);
        }

        if (job.ClosingAtUtc.HasValue && job.ClosingAtUtc.Value <= DateTimeOffset.UtcNow)
        {
            return Result<Guid>.Failure("This job posting has closed.", 400);
        }

        var alreadyApplied = await _dbContext.Applications
            .AnyAsync(a => a.JobId == jobId && a.CandidateUserId == candidateUserId, cancellationToken);

        if (alreadyApplied)
        {
            return Result<Guid>.Failure("You have already applied for this position.", 409);
        }

        var application = new JobApplication
        {
            JobId = jobId,
            CandidateUserId = candidateUserId,
            ResumeUrl = request.ResumeUrl?.Trim(),
            CoverNote = request.CoverNote?.Trim(),
            Status = "Submitted",
            AppliedAtUtc = DateTimeOffset.UtcNow,
            UpdatedAtUtc = DateTimeOffset.UtcNow
        };

        var history = new ApplicationStatusHistory
        {
            ApplicationId = application.Id,
            FromStatus = null,
            ToStatus = "Submitted",
            ChangedByUserId = candidateUserId,
            Comment = "Initial application submission",
            ChangedAtUtc = DateTimeOffset.UtcNow
        };

        _dbContext.Applications.Add(application);
        _dbContext.ApplicationStatusHistories.Add(history);

        _dbContext.AuditLogs.Add(new AuditLog
        {
            ActorUserId = candidateUserId,
            Action = "ApplicationSubmitted",
            EntityType = "Application",
            EntityId = application.Id.ToString(),
            CreatedAtUtc = DateTimeOffset.UtcNow
        });

        await _dbContext.SaveChangesAsync(cancellationToken);
        return Result<Guid>.Success(application.Id, 201);
    }

    public async Task<Result<PagedResult<JobApplicationDto>>> GetCandidateApplicationsAsync(
        Guid candidateUserId, 
        int page, 
        int pageSize, 
        CancellationToken cancellationToken = default)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var query = _dbContext.Applications
            .Include(a => a.Job)
                .ThenInclude(j => j.Company)
            .AsNoTracking()
            .Where(a => a.CandidateUserId == candidateUserId);

        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderByDescending(a => a.AppliedAtUtc)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(a => MapToDto(a))
            .ToListAsync(cancellationToken);

        return Result<PagedResult<JobApplicationDto>>.Success(PagedResult<JobApplicationDto>.Create(items, page, pageSize, totalCount));
    }

    public async Task<Result<JobApplicationDto>> GetApplicationByIdAsync(
        Guid applicationId, 
        Guid currentUserId, 
        bool isElevated, 
        CancellationToken cancellationToken = default)
    {
        var application = await _dbContext.Applications
            .Include(a => a.Job)
                .ThenInclude(j => j.Company)
            .AsNoTracking()
            .FirstOrDefaultAsync(a => a.Id == applicationId, cancellationToken);

        if (application == null)
        {
            return Result<JobApplicationDto>.Failure("Application not found.", 404);
        }

        if (application.CandidateUserId != currentUserId && !isElevated)
        {
            return Result<JobApplicationDto>.Failure("Access forbidden.", 403);
        }

        return Result<JobApplicationDto>.Success(MapToDto(application));
    }

    public async Task<Result<PagedResult<JobApplicationDto>>> GetJobApplicationsAsync(
        Guid jobId, 
        int page, 
        int pageSize, 
        string? status, 
        CancellationToken cancellationToken = default)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var query = _dbContext.Applications
            .Include(a => a.Job)
                .ThenInclude(j => j.Company)
            .AsNoTracking()
            .Where(a => a.JobId == jobId);

        if (!string.IsNullOrWhiteSpace(status))
        {
            query = query.Where(a => a.Status == status.Trim());
        }

        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderByDescending(a => a.AppliedAtUtc)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(a => MapToDto(a))
            .ToListAsync(cancellationToken);

        return Result<PagedResult<JobApplicationDto>>.Success(PagedResult<JobApplicationDto>.Create(items, page, pageSize, totalCount));
    }

    public async Task<Result<bool>> UpdateApplicationStatusAsync(
        Guid applicationId, 
        UpdateApplicationStatusRequest request, 
        Guid currentUserId, 
        CancellationToken cancellationToken = default)
    {
        var application = await _dbContext.Applications
            .FirstOrDefaultAsync(a => a.Id == applicationId, cancellationToken);

        if (application == null)
        {
            return Result<bool>.Failure("Application not found.", 404);
        }

        var oldStatus = application.Status;
        var newStatus = request.Status.Trim();

        if (oldStatus == newStatus)
        {
            return Result<bool>.Success(true);
        }

        application.Status = newStatus;
        application.UpdatedAtUtc = DateTimeOffset.UtcNow;

        var history = new ApplicationStatusHistory
        {
            ApplicationId = application.Id,
            FromStatus = oldStatus,
            ToStatus = newStatus,
            ChangedByUserId = currentUserId,
            Comment = request.Comment?.Trim(),
            ChangedAtUtc = DateTimeOffset.UtcNow
        };

        _dbContext.ApplicationStatusHistories.Add(history);

        _dbContext.AuditLogs.Add(new AuditLog
        {
            ActorUserId = currentUserId,
            Action = "ApplicationStatusUpdated",
            EntityType = "Application",
            EntityId = application.Id.ToString(),
            OldValuesJson = $"{{\"status\": \"{oldStatus}\"}}",
            NewValuesJson = $"{{\"status\": \"{newStatus}\"}}",
            CreatedAtUtc = DateTimeOffset.UtcNow
        });

        await _dbContext.SaveChangesAsync(cancellationToken);
        return Result<bool>.Success(true);
    }

    public async Task<Result<bool>> WithdrawApplicationAsync(
        Guid applicationId, 
        Guid candidateUserId, 
        CancellationToken cancellationToken = default)
    {
        var application = await _dbContext.Applications
            .FirstOrDefaultAsync(a => a.Id == applicationId, cancellationToken);

        if (application == null)
        {
            return Result<bool>.Failure("Application not found.", 404);
        }

        if (application.CandidateUserId != candidateUserId)
        {
            return Result<bool>.Failure("You can only withdraw your own applications.", 403);
        }

        if (application.Status == "Withdrawn" || application.Status == "Hired" || application.Status == "Rejected")
        {
            return Result<bool>.Failure($"Cannot withdraw an application that is already '{application.Status}'.", 400);
        }

        var oldStatus = application.Status;
        application.Status = "Withdrawn";
        application.UpdatedAtUtc = DateTimeOffset.UtcNow;

        var history = new ApplicationStatusHistory
        {
            ApplicationId = application.Id,
            FromStatus = oldStatus,
            ToStatus = "Withdrawn",
            ChangedByUserId = candidateUserId,
            Comment = "Candidate withdrew application",
            ChangedAtUtc = DateTimeOffset.UtcNow
        };

        _dbContext.ApplicationStatusHistories.Add(history);

        _dbContext.AuditLogs.Add(new AuditLog
        {
            ActorUserId = candidateUserId,
            Action = "ApplicationWithdrawn",
            EntityType = "Application",
            EntityId = application.Id.ToString(),
            CreatedAtUtc = DateTimeOffset.UtcNow
        });

        await _dbContext.SaveChangesAsync(cancellationToken);
        return Result<bool>.Success(true);
    }

    public async Task<Result<IReadOnlyList<ApplicationStatusHistoryDto>>> GetApplicationHistoryAsync(
        Guid applicationId, 
        Guid currentUserId, 
        bool isElevated, 
        CancellationToken cancellationToken = default)
    {
        var application = await _dbContext.Applications
            .AsNoTracking()
            .FirstOrDefaultAsync(a => a.Id == applicationId, cancellationToken);

        if (application == null)
        {
            return Result<IReadOnlyList<ApplicationStatusHistoryDto>>.Failure("Application not found.", 404);
        }

        if (application.CandidateUserId != currentUserId && !isElevated)
        {
            return Result<IReadOnlyList<ApplicationStatusHistoryDto>>.Failure("Access forbidden.", 403);
        }

        var history = await _dbContext.ApplicationStatusHistories
            .AsNoTracking()
            .Where(h => h.ApplicationId == applicationId)
            .OrderBy(h => h.ChangedAtUtc)
            .Select(h => new ApplicationStatusHistoryDto(
                h.Id,
                h.ApplicationId,
                h.FromStatus,
                h.ToStatus,
                h.ChangedByUserId,
                h.Comment,
                h.ChangedAtUtc
            ))
            .ToListAsync(cancellationToken);

        return Result<IReadOnlyList<ApplicationStatusHistoryDto>>.Success(history);
    }

    private static JobApplicationDto MapToDto(JobApplication a) => new(
        a.Id,
        a.JobId,
        a.Job?.Title ?? string.Empty,
        a.Job?.Company?.Name ?? string.Empty,
        a.CandidateUserId,
        a.ResumeUrl,
        a.CoverNote,
        a.Status,
        a.AppliedAtUtc,
        a.UpdatedAtUtc
    );
}
