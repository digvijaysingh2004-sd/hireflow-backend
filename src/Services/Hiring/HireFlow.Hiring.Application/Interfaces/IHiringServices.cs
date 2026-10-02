using HireFlow.Hiring.Application.Common;
using HireFlow.Hiring.Application.DTOs;

namespace HireFlow.Hiring.Application.Interfaces;

public interface ICompanyService
{
    Task<Result<Guid>> CreateCompanyAsync(CreateCompanyRequest request, Guid currentUserId, CancellationToken cancellationToken = default);
    Task<Result<PagedResult<CompanyDto>>> GetCompaniesPagedAsync(int page, int pageSize, string? search, CancellationToken cancellationToken = default);
    Task<Result<CompanyDto>> GetCompanyByIdAsync(Guid companyId, CancellationToken cancellationToken = default);
    Task<Result<CompanyDto>> UpdateCompanyAsync(Guid companyId, UpdateCompanyRequest request, Guid currentUserId, bool isAdmin, CancellationToken cancellationToken = default);
    Task<Result<bool>> DeleteCompanyAsync(Guid companyId, Guid currentUserId, bool isAdmin, CancellationToken cancellationToken = default);
}

public interface IJobService
{
    Task<Result<Guid>> CreateJobAsync(CreateJobRequest request, Guid currentUserId, CancellationToken cancellationToken = default);
    Task<Result<PagedResult<JobDto>>> GetJobsPagedAsync(int page, int pageSize, string? search, string? status, Guid? companyId, CancellationToken cancellationToken = default);
    Task<Result<JobDto>> GetJobByIdAsync(Guid jobId, CancellationToken cancellationToken = default);
    Task<Result<JobDto>> UpdateJobAsync(Guid jobId, UpdateJobRequest request, Guid currentUserId, bool isAdmin, CancellationToken cancellationToken = default);
    Task<Result<bool>> PublishJobAsync(Guid jobId, Guid currentUserId, bool isAdmin, CancellationToken cancellationToken = default);
    Task<Result<bool>> CloseJobAsync(Guid jobId, Guid currentUserId, bool isAdmin, CancellationToken cancellationToken = default);
    Task<Result<bool>> DeleteJobAsync(Guid jobId, Guid currentUserId, bool isAdmin, CancellationToken cancellationToken = default);
    Task<Result<JobStatisticsDto>> GetJobStatisticsAsync(Guid jobId, CancellationToken cancellationToken = default);
}

public interface IApplicationService
{
    Task<Result<Guid>> ApplyJobAsync(Guid jobId, ApplyJobRequest request, Guid candidateUserId, CancellationToken cancellationToken = default);
    Task<Result<PagedResult<JobApplicationDto>>> GetCandidateApplicationsAsync(Guid candidateUserId, int page, int pageSize, CancellationToken cancellationToken = default);
    Task<Result<JobApplicationDto>> GetApplicationByIdAsync(Guid applicationId, Guid currentUserId, bool isElevated, CancellationToken cancellationToken = default);
    Task<Result<PagedResult<JobApplicationDto>>> GetJobApplicationsAsync(Guid jobId, int page, int pageSize, string? status, CancellationToken cancellationToken = default);
    Task<Result<bool>> UpdateApplicationStatusAsync(Guid applicationId, UpdateApplicationStatusRequest request, Guid currentUserId, CancellationToken cancellationToken = default);
    Task<Result<bool>> WithdrawApplicationAsync(Guid applicationId, Guid candidateUserId, CancellationToken cancellationToken = default);
    Task<Result<IReadOnlyList<ApplicationStatusHistoryDto>>> GetApplicationHistoryAsync(Guid applicationId, Guid currentUserId, bool isElevated, CancellationToken cancellationToken = default);
}

public interface IInterviewService
{
    Task<Result<Guid>> ScheduleInterviewAsync(Guid applicationId, ScheduleInterviewRequest request, Guid scheduledByUserId, CancellationToken cancellationToken = default);
    Task<Result<PagedResult<InterviewDto>>> GetCandidateInterviewsAsync(Guid candidateUserId, int page, int pageSize, CancellationToken cancellationToken = default);
    Task<Result<PagedResult<InterviewDto>>> GetInterviewsPagedAsync(int page, int pageSize, string? status, CancellationToken cancellationToken = default);
    Task<Result<InterviewDto>> GetInterviewByIdAsync(Guid interviewId, Guid currentUserId, bool isElevated, CancellationToken cancellationToken = default);
    Task<Result<bool>> RescheduleInterviewAsync(Guid interviewId, RescheduleInterviewRequest request, Guid currentUserId, bool isElevated, CancellationToken cancellationToken = default);
    Task<Result<bool>> UpdateInterviewStatusAsync(Guid interviewId, UpdateInterviewStatusRequest request, Guid currentUserId, bool isElevated, CancellationToken cancellationToken = default);
    Task<Result<bool>> CancelInterviewAsync(Guid interviewId, Guid currentUserId, bool isElevated, CancellationToken cancellationToken = default);
}

public interface IDashboardService
{
    Task<Result<DashboardSummaryDto>> GetDashboardSummaryAsync(CancellationToken cancellationToken = default);
}

public interface IAuditService
{
    Task<Result<PagedResult<AuditLogDto>>> GetAuditLogsPagedAsync(int page, int pageSize, string? entityType, string? action, CancellationToken cancellationToken = default);
    Task<Result<AuditLogDto>> GetAuditLogByIdAsync(Guid auditId, CancellationToken cancellationToken = default);
}
