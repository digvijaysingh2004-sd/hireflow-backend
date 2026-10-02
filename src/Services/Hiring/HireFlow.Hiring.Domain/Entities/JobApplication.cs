namespace HireFlow.Hiring.Domain.Entities;

public class JobApplication
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid JobId { get; set; }
    public Job Job { get; set; } = null!;
    public Guid CandidateUserId { get; set; }
    public string? ResumeUrl { get; set; }
    public string? CoverNote { get; set; }
    public string Status { get; set; } = "Submitted"; // Submitted, UnderReview, Shortlisted, InterviewScheduled, Offered, Hired, Rejected, Withdrawn
    public DateTimeOffset AppliedAtUtc { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAtUtc { get; set; } = DateTimeOffset.UtcNow;
    public long RowVersion { get; set; } = 1;

    public ICollection<ApplicationStatusHistory> StatusHistory { get; set; } = new List<ApplicationStatusHistory>();
    public ICollection<Interview> Interviews { get; set; } = new List<Interview>();
}

