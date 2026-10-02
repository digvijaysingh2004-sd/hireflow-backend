namespace HireFlow.Hiring.Domain.Entities;

public class Job
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid CompanyId { get; set; }
    public Company Company { get; set; } = null!;
    public string Title { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string? Requirements { get; set; }
    public string? Location { get; set; }
    public string EmploymentType { get; set; } = "FullTime";
    public string Status { get; set; } = "Draft"; // Draft, Published, Closed
    public short? ExperienceMinYears { get; set; }
    public short? ExperienceMaxYears { get; set; }
    public decimal? SalaryMin { get; set; }
    public decimal? SalaryMax { get; set; }
    public string Currency { get; set; } = "USD";
    public string[] Skills { get; set; } = Array.Empty<string>();
    public Guid CreatedByUserId { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAtUtc { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? PublishedAtUtc { get; set; }
    public DateTimeOffset? ClosingAtUtc { get; set; }
    public DateTimeOffset? ClosedAtUtc { get; set; }
    public long RowVersion { get; set; } = 1;

    public ICollection<JobApplication> Applications { get; set; } = new List<JobApplication>();
}
