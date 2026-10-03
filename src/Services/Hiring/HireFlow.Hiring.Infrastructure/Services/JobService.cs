using System.Text.RegularExpressions;
using BuildingBlocks.Infrastructure.Caching;
using HireFlow.Hiring.Application.Common;
using HireFlow.Hiring.Application.DTOs;
using HireFlow.Hiring.Application.Interfaces;
using HireFlow.Hiring.Domain.Entities;
using HireFlow.Hiring.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace HireFlow.Hiring.Infrastructure.Services;

public class JobService : IJobService
{
    private readonly HiringDbContext _dbContext;
    private readonly ICacheService _cacheService;

    public JobService(HiringDbContext dbContext, ICacheService cacheService)
    {
        _dbContext = dbContext;
        _cacheService = cacheService;
    }

    public async Task<Result<Guid>> CreateJobAsync(CreateJobRequest request, Guid currentUserId, CancellationToken cancellationToken = default)
    {
        var company = await _dbContext.Companies
            .FirstOrDefaultAsync(c => c.Id == request.CompanyId, cancellationToken);

        if (company == null)
        {
            return Result<Guid>.Failure("Target company not found.", 404);
        }

        var slug = GenerateSlug(request.Title);
        var baseSlug = slug;
        var counter = 1;
        while (await _dbContext.Jobs.AnyAsync(j => j.Slug == slug, cancellationToken))
        {
            slug = $"{baseSlug}-{counter++}";
        }

        var job = new Job
        {
            CompanyId = request.CompanyId,
            Title = request.Title.Trim(),
            Slug = slug,
            Description = request.Description.Trim(),
            Location = request.Location?.Trim(),
            EmploymentType = request.EmploymentType.Trim(),
            ExperienceMinYears = request.ExperienceMinYears,
            ExperienceMaxYears = request.ExperienceMaxYears,
            SalaryMin = request.SalaryMin,
            SalaryMax = request.SalaryMax,
            Status = "Draft",
            CreatedByUserId = currentUserId,
            ClosingAtUtc = request.ClosingAtUtc,
            CreatedAtUtc = DateTimeOffset.UtcNow,
            UpdatedAtUtc = DateTimeOffset.UtcNow
        };

        _dbContext.Jobs.Add(job);
        _dbContext.AuditLogs.Add(new AuditLog
        {
            ActorUserId = currentUserId,
            Action = "JobCreated",
            EntityType = "Job",
            EntityId = job.Id.ToString(),
            CreatedAtUtc = DateTimeOffset.UtcNow
        });

        await _dbContext.SaveChangesAsync(cancellationToken);
        await _cacheService.RemoveByPrefixAsync("jobs:paged:", cancellationToken);
        return Result<Guid>.Success(job.Id, 201);
    }

    public async Task<Result<PagedResult<JobDto>>> GetJobsPagedAsync(
        int page, 
        int pageSize, 
        string? search, 
        string? status, 
        Guid? companyId, 
        CancellationToken cancellationToken = default)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var query = _dbContext.Jobs
            .Include(j => j.Company)
            .AsNoTracking()
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(status))
        {
            query = query.Where(j => j.Status == status.Trim());
        }

        if (companyId.HasValue)
        {
            query = query.Where(j => j.CompanyId == companyId.Value);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.Trim().ToLower();
            query = query.Where(j => j.Title.ToLower().Contains(s) || 
                                     j.Description.ToLower().Contains(s) || 
                                     (j.Location != null && j.Location.ToLower().Contains(s)));
        }

        var totalCount = await query.CountAsync(cancellationToken);
        var entities = await query
            .OrderByDescending(j => j.CreatedAtUtc)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        var items = entities.Select(MapToDto).ToList();

        return Result<PagedResult<JobDto>>.Success(PagedResult<JobDto>.Create(items, page, pageSize, totalCount));
    }

    public async Task<Result<JobDto>> GetJobByIdAsync(Guid jobId, CancellationToken cancellationToken = default)
    {
        var cacheKey = $"job:{jobId}";
        var cachedJob = await _cacheService.GetAsync<JobDto>(cacheKey, cancellationToken);
        if (cachedJob != null)
        {
            return Result<JobDto>.Success(cachedJob);
        }

        var job = await _dbContext.Jobs
            .Include(j => j.Company)
            .AsNoTracking()
            .FirstOrDefaultAsync(j => j.Id == jobId, cancellationToken);

        if (job == null)
        {
            return Result<JobDto>.Failure("Job not found.", 404);
        }

        var dto = MapToDto(job);
        await _cacheService.SetAsync(cacheKey, dto, TimeSpan.FromMinutes(10), cancellationToken);
        return Result<JobDto>.Success(dto);
    }

    public async Task<Result<JobDto>> UpdateJobAsync(
        Guid jobId, 
        UpdateJobRequest request, 
        Guid currentUserId, 
        bool isAdmin, 
        CancellationToken cancellationToken = default)
    {
        var job = await _dbContext.Jobs
            .Include(j => j.Company)
            .FirstOrDefaultAsync(j => j.Id == jobId, cancellationToken);

        if (job == null)
        {
            return Result<JobDto>.Failure("Job not found.", 404);
        }

        if (job.CreatedByUserId != currentUserId && !isAdmin)
        {
            return Result<JobDto>.Failure("You do not have permission to update this job.", 403);
        }

        job.Title = request.Title.Trim();
        job.Description = request.Description.Trim();
        job.Location = request.Location?.Trim();
        job.EmploymentType = request.EmploymentType.Trim();
        job.ExperienceMinYears = request.ExperienceMinYears;
        job.ExperienceMaxYears = request.ExperienceMaxYears;
        job.SalaryMin = request.SalaryMin;
        job.SalaryMax = request.SalaryMax;
        job.ClosingAtUtc = request.ClosingAtUtc;
        job.UpdatedAtUtc = DateTimeOffset.UtcNow;

        _dbContext.AuditLogs.Add(new AuditLog
        {
            ActorUserId = currentUserId,
            Action = "JobUpdated",
            EntityType = "Job",
            EntityId = job.Id.ToString(),
            CreatedAtUtc = DateTimeOffset.UtcNow
        });

        await _dbContext.SaveChangesAsync(cancellationToken);
        await _cacheService.RemoveAsync($"job:{jobId}", cancellationToken);
        await _cacheService.RemoveByPrefixAsync("jobs:paged:", cancellationToken);
        return Result<JobDto>.Success(MapToDto(job));
    }

    public async Task<Result<bool>> PublishJobAsync(Guid jobId, Guid currentUserId, bool isAdmin, CancellationToken cancellationToken = default)
    {
        var job = await _dbContext.Jobs
            .FirstOrDefaultAsync(j => j.Id == jobId, cancellationToken);

        if (job == null)
        {
            return Result<bool>.Failure("Job not found.", 404);
        }

        job.Status = "Published";
        job.PublishedAtUtc = DateTimeOffset.UtcNow;
        job.UpdatedAtUtc = DateTimeOffset.UtcNow;

        _dbContext.AuditLogs.Add(new AuditLog
        {
            ActorUserId = currentUserId,
            Action = "JobPublished",
            EntityType = "Job",
            EntityId = jobId.ToString(),
            CreatedAtUtc = DateTimeOffset.UtcNow
        });

        await _dbContext.SaveChangesAsync(cancellationToken);
        await _cacheService.RemoveAsync($"job:{jobId}", cancellationToken);
        await _cacheService.RemoveByPrefixAsync("jobs:paged:", cancellationToken);
        return Result<bool>.Success(true);
    }

    public async Task<Result<bool>> CloseJobAsync(Guid jobId, Guid currentUserId, bool isAdmin, CancellationToken cancellationToken = default)
    {
        var job = await _dbContext.Jobs
            .FirstOrDefaultAsync(j => j.Id == jobId, cancellationToken);

        if (job == null)
        {
            return Result<bool>.Failure("Job not found.", 404);
        }

        job.Status = "Closed";
        job.ClosingAtUtc = DateTimeOffset.UtcNow;
        job.UpdatedAtUtc = DateTimeOffset.UtcNow;

        _dbContext.AuditLogs.Add(new AuditLog
        {
            ActorUserId = currentUserId,
            Action = "JobClosed",
            EntityType = "Job",
            EntityId = jobId.ToString(),
            CreatedAtUtc = DateTimeOffset.UtcNow
        });

        await _dbContext.SaveChangesAsync(cancellationToken);
        await _cacheService.RemoveAsync($"job:{jobId}", cancellationToken);
        await _cacheService.RemoveByPrefixAsync("jobs:paged:", cancellationToken);
        return Result<bool>.Success(true);
    }

    public async Task<Result<bool>> DeleteJobAsync(Guid jobId, Guid currentUserId, bool isAdmin, CancellationToken cancellationToken = default)
    {
        var job = await _dbContext.Jobs
            .FirstOrDefaultAsync(j => j.Id == jobId, cancellationToken);

        if (job == null)
        {
            return Result<bool>.Failure("Job not found.", 404);
        }

        if (job.CreatedByUserId != currentUserId && !isAdmin)
        {
            return Result<bool>.Failure("You do not have permission to delete this job.", 403);
        }

        _dbContext.Jobs.Remove(job);
        _dbContext.AuditLogs.Add(new AuditLog
        {
            ActorUserId = currentUserId,
            Action = "JobDeleted",
            EntityType = "Job",
            EntityId = jobId.ToString(),
            CreatedAtUtc = DateTimeOffset.UtcNow
        });

        await _dbContext.SaveChangesAsync(cancellationToken);
        await _cacheService.RemoveAsync($"job:{jobId}", cancellationToken);
        await _cacheService.RemoveByPrefixAsync("jobs:paged:", cancellationToken);
        return Result<bool>.Success(true);
    }

    public async Task<Result<JobStatisticsDto>> GetJobStatisticsAsync(Guid jobId, CancellationToken cancellationToken = default)
    {
        var job = await _dbContext.Jobs
            .Include(j => j.Applications)
            .AsNoTracking()
            .FirstOrDefaultAsync(j => j.Id == jobId, cancellationToken);

        if (job == null)
        {
            return Result<JobStatisticsDto>.Failure("Job not found.", 404);
        }

        var counts = job.Applications
            .GroupBy(a => a.Status)
            .ToDictionary(g => g.Key, g => g.Count());

        var dto = new JobStatisticsDto(
            job.Id,
            job.Title,
            job.Applications.Count,
            counts
        );

        return Result<JobStatisticsDto>.Success(dto);
    }

    private static JobDto MapToDto(Job j) => new(
        j.Id,
        j.CompanyId,
        j.Company?.Name ?? string.Empty,
        j.Title,
        j.Slug,
        j.Description,
        j.Requirements,
        j.Location,
        j.EmploymentType,
        j.Status,
        j.ExperienceMinYears,
        j.ExperienceMaxYears,
        j.SalaryMin,
        j.SalaryMax,
        j.Currency,
        j.Skills,
        j.CreatedByUserId,
        j.CreatedAtUtc,
        j.UpdatedAtUtc,
        j.PublishedAtUtc,
        j.ClosingAtUtc,
        j.ClosedAtUtc
    );

    private static string GenerateSlug(string text)
    {
        var clean = Regex.Replace(text.ToLowerInvariant(), @"[^a-z0-9\s-]", "");
        return Regex.Replace(clean, @"\s+", "-").Trim('-');
    }
}
