namespace HireFlow.Hiring.Application.DTOs;

public record PagedResult<T>(
    IReadOnlyList<T> Items,
    int Page,
    int PageSize,
    int TotalCount,
    int TotalPages,
    bool HasNextPage
)
{
    public static PagedResult<T> Create(IReadOnlyList<T> items, int page, int pageSize, int totalCount)
    {
        var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);
        return new PagedResult<T>(
            items,
            page,
            pageSize,
            totalCount,
            totalPages,
            page < totalPages
        );
    }
}

// Company DTOs
public record CreateCompanyRequest(
    string Name,
    string? Website,
    string? Description
);

public record UpdateCompanyRequest(
    string Name,
    string? Website,
    string? Description
);

public record CompanyDto(
    Guid Id,
    string Name,
    string Slug,
    string? Website,
    string? Description,
    Guid CreatedByUserId,
    DateTimeOffset CreatedAtUtc,
    int ActiveJobsCount
);

// Job DTOs
public record CreateJobRequest(
    Guid CompanyId,
    string Title,
    string Description,
    string? Location,
    string EmploymentType,
    short? ExperienceMinYears,
    short? ExperienceMaxYears,
    decimal? SalaryMin,
    decimal? SalaryMax,
    DateTimeOffset? ClosingAtUtc
);

public record UpdateJobRequest(
    string Title,
    string Description,
    string? Location,
    string EmploymentType,
    short? ExperienceMinYears,
    short? ExperienceMaxYears,
    decimal? SalaryMin,
    decimal? SalaryMax,
    DateTimeOffset? ClosingAtUtc
);

public record JobDto(
    Guid Id,
    Guid CompanyId,
    string CompanyName,
    string Title,
    string Slug,
    string Description,
    string? Location,
    string EmploymentType,
    short? ExperienceMinYears,
    short? ExperienceMaxYears,
    decimal? SalaryMin,
    decimal? SalaryMax,
    string Status,
    Guid CreatedByUserId,
    DateTimeOffset? PublishedAtUtc,
    DateTimeOffset? ClosingAtUtc,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset UpdatedAtUtc
);

public record JobStatisticsDto(
    Guid JobId,
    string JobTitle,
    int TotalApplications,
    Dictionary<string, int> ApplicationsByStatus
);

// Application DTOs
public record ApplyJobRequest(
    string? ResumeUrl,
    string? CoverNote
);

public record UpdateApplicationStatusRequest(
    string Status,
    string? Comment
);

public record JobApplicationDto(
    Guid Id,
    Guid JobId,
    string JobTitle,
    string CompanyName,
    Guid CandidateUserId,
    string? ResumeUrl,
    string? CoverNote,
    string Status,
    DateTimeOffset AppliedAtUtc,
    DateTimeOffset UpdatedAtUtc
);

public record ApplicationStatusHistoryDto(
    Guid Id,
    Guid ApplicationId,
    string? FromStatus,
    string ToStatus,
    Guid ChangedByUserId,
    string? Comment,
    DateTimeOffset ChangedAtUtc
);

// Interview DTOs
public record ScheduleInterviewRequest(
    DateTimeOffset StartsAtUtc,
    DateTimeOffset EndsAtUtc,
    string? MeetingUrl,
    string? Notes
);

public record RescheduleInterviewRequest(
    DateTimeOffset StartsAtUtc,
    DateTimeOffset EndsAtUtc,
    string? MeetingUrl,
    string? Notes
);

public record UpdateInterviewStatusRequest(
    string Status, // Scheduled, Completed, Cancelled
    string? Notes
);

public record InterviewDto(
    Guid Id,
    Guid ApplicationId,
    Guid CandidateUserId,
    string JobTitle,
    string CompanyName,
    Guid ScheduledByUserId,
    DateTimeOffset StartsAtUtc,
    DateTimeOffset EndsAtUtc,
    string? MeetingUrl,
    string Status,
    string? Notes,
    DateTimeOffset CreatedAtUtc
);

// Dashboard DTOs
public record DashboardSummaryDto(
    int TotalActiveJobs,
    int TotalApplications,
    int PendingReviews,
    int ScheduledInterviews
);

// Audit Log DTOs
public record AuditLogDto(
    Guid Id,
    Guid? ActorUserId,
    string Action,
    string EntityType,
    string? EntityId,
    string? OldValuesJson,
    string? NewValuesJson,
    string? CorrelationId,
    string? IpAddress,
    DateTimeOffset CreatedAtUtc
);
