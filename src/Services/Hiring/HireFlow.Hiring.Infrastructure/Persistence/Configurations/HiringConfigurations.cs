using HireFlow.Hiring.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HireFlow.Hiring.Infrastructure.Persistence.Configurations;

public class CompanyConfiguration : IEntityTypeConfiguration<Company>
{
    public void Configure(EntityTypeBuilder<Company> builder)
    {
        builder.ToTable("companies");
        builder.HasKey(c => c.Id);

        builder.Property(c => c.Id).HasColumnName("id");
        builder.Property(c => c.Name).HasColumnName("name").HasMaxLength(150).IsRequired();
        builder.Property(c => c.Slug).HasColumnName("slug").HasMaxLength(160).IsRequired();
        builder.Property(c => c.Website).HasColumnName("website").HasMaxLength(255);
        builder.Property(c => c.Description).HasColumnName("description").HasMaxLength(2000);
        builder.Property(c => c.CreatedByUserId).HasColumnName("created_by_user_id").IsRequired();
        builder.Property(c => c.CreatedAtUtc).HasColumnName("created_at_utc").IsRequired();
        builder.Property(c => c.UpdatedAtUtc).HasColumnName("updated_at_utc").IsRequired();

        builder.HasIndex(c => c.Slug).IsUnique();
        builder.HasIndex(c => c.CreatedByUserId);
    }
}

public class JobConfiguration : IEntityTypeConfiguration<Job>
{
    public void Configure(EntityTypeBuilder<Job> builder)
    {
        builder.ToTable("jobs");
        builder.HasKey(j => j.Id);

        builder.Property(j => j.Id).HasColumnName("id");
        builder.Property(j => j.CompanyId).HasColumnName("company_id").IsRequired();
        builder.Property(j => j.Title).HasColumnName("title").HasMaxLength(160).IsRequired();
        builder.Property(j => j.Slug).HasColumnName("slug").HasMaxLength(180).IsRequired();
        builder.Property(j => j.Description).HasColumnName("description").IsRequired();
        builder.Property(j => j.Location).HasColumnName("location").HasMaxLength(160);
        builder.Property(j => j.EmploymentType).HasColumnName("employment_type").HasMaxLength(30).IsRequired();
        builder.Property(j => j.ExperienceMinYears).HasColumnName("experience_min_years");
        builder.Property(j => j.ExperienceMaxYears).HasColumnName("experience_max_years");
        builder.Property(j => j.SalaryMin).HasColumnName("salary_min").HasPrecision(12, 2);
        builder.Property(j => j.SalaryMax).HasColumnName("salary_max").HasPrecision(12, 2);
        builder.Property(j => j.Status).HasColumnName("status").HasMaxLength(30).IsRequired();
        builder.Property(j => j.CreatedByUserId).HasColumnName("created_by_user_id").IsRequired();
        builder.Property(j => j.PublishedAtUtc).HasColumnName("published_at_utc");
        builder.Property(j => j.ClosingAtUtc).HasColumnName("closing_at_utc");
        builder.Property(j => j.CreatedAtUtc).HasColumnName("created_at_utc").IsRequired();
        builder.Property(j => j.UpdatedAtUtc).HasColumnName("updated_at_utc").IsRequired();
        builder.Property(j => j.RowVersion).HasColumnName("row_version").IsConcurrencyToken().IsRequired();

        builder.Ignore(j => j.Requirements);
        builder.Ignore(j => j.Currency);
        builder.Ignore(j => j.Skills);
        builder.Ignore(j => j.ClosedAtUtc);

        builder.HasOne(j => j.Company)
            .WithMany(c => c.Jobs)
            .HasForeignKey(j => j.CompanyId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(j => j.CompanyId);
        builder.HasIndex(j => j.Slug);
        builder.HasIndex(j => j.Status);
        builder.HasIndex(j => j.CreatedByUserId);
    }
}

public class JobApplicationConfiguration : IEntityTypeConfiguration<JobApplication>
{
    public void Configure(EntityTypeBuilder<JobApplication> builder)
    {
        builder.ToTable("applications");
        builder.HasKey(a => a.Id);

        builder.Property(a => a.Id).HasColumnName("id");
        builder.Property(a => a.JobId).HasColumnName("job_id").IsRequired();
        builder.Property(a => a.CandidateUserId).HasColumnName("candidate_user_id").IsRequired();
        builder.Property(a => a.ResumeUrl).HasColumnName("resume_url").HasMaxLength(500);
        builder.Property(a => a.CoverNote).HasColumnName("cover_note").HasMaxLength(2000);
        builder.Property(a => a.Status).HasColumnName("status").HasMaxLength(40).IsRequired();
        builder.Property(a => a.AppliedAtUtc).HasColumnName("applied_at_utc").IsRequired();
        builder.Property(a => a.UpdatedAtUtc).HasColumnName("updated_at_utc").IsRequired();
        builder.Property(a => a.RowVersion).HasColumnName("row_version").IsConcurrencyToken().IsRequired();

        builder.HasOne(a => a.Job)
            .WithMany(j => j.Applications)
            .HasForeignKey(a => a.JobId)
            .OnDelete(DeleteBehavior.Restrict);

        // One application per candidate per job
        builder.HasIndex(a => new { a.JobId, a.CandidateUserId }).IsUnique();
        builder.HasIndex(a => a.CandidateUserId);
        builder.HasIndex(a => a.Status);
    }
}

public class ApplicationStatusHistoryConfiguration : IEntityTypeConfiguration<ApplicationStatusHistory>
{
    public void Configure(EntityTypeBuilder<ApplicationStatusHistory> builder)
    {
        builder.ToTable("application_status_history");
        builder.HasKey(h => h.Id);

        builder.Property(h => h.Id).HasColumnName("id");
        builder.Property(h => h.ApplicationId).HasColumnName("application_id").IsRequired();
        builder.Property(h => h.FromStatus).HasColumnName("from_status").HasMaxLength(40);
        builder.Property(h => h.ToStatus).HasColumnName("to_status").HasMaxLength(40).IsRequired();
        builder.Property(h => h.ChangedByUserId).HasColumnName("changed_by_user_id").IsRequired();
        builder.Property(h => h.Comment).HasColumnName("comment").HasMaxLength(500);
        builder.Property(h => h.ChangedAtUtc).HasColumnName("changed_at_utc").IsRequired();

        builder.HasOne(h => h.Application)
            .WithMany(a => a.StatusHistory)
            .HasForeignKey(h => h.ApplicationId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(h => h.ApplicationId);
    }
}

public class InterviewConfiguration : IEntityTypeConfiguration<Interview>
{
    public void Configure(EntityTypeBuilder<Interview> builder)
    {
        builder.ToTable("interviews");
        builder.HasKey(i => i.Id);

        builder.Property(i => i.Id).HasColumnName("id");
        builder.Property(i => i.ApplicationId).HasColumnName("application_id").IsRequired();
        builder.Property(i => i.ScheduledByUserId).HasColumnName("scheduled_by_user_id").IsRequired();
        builder.Property(i => i.StartsAtUtc).HasColumnName("starts_at_utc").IsRequired();
        builder.Property(i => i.EndsAtUtc).HasColumnName("ends_at_utc").IsRequired();
        builder.Property(i => i.MeetingUrl).HasColumnName("meeting_url").HasMaxLength(500);
        builder.Property(i => i.Status).HasColumnName("status").HasMaxLength(30).IsRequired();
        builder.Property(i => i.Notes).HasColumnName("notes").HasMaxLength(2000);
        builder.Property(i => i.CreatedAtUtc).HasColumnName("created_at_utc").IsRequired();
        builder.Property(i => i.UpdatedAtUtc).HasColumnName("updated_at_utc").IsRequired();

        builder.HasOne(i => i.Application)
            .WithMany(a => a.Interviews)
            .HasForeignKey(i => i.ApplicationId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(i => i.ApplicationId);
        builder.HasIndex(i => i.ScheduledByUserId);
        builder.HasIndex(i => i.Status);
    }
}

public class AuditLogConfiguration : IEntityTypeConfiguration<AuditLog>
{
    public void Configure(EntityTypeBuilder<AuditLog> builder)
    {
        builder.ToTable("audit_logs");
        builder.HasKey(l => l.Id);

        builder.Property(l => l.Id).HasColumnName("id");
        builder.Property(l => l.ActorUserId).HasColumnName("actor_user_id");
        builder.Property(l => l.Action).HasColumnName("action").HasMaxLength(100).IsRequired();
        builder.Property(l => l.EntityType).HasColumnName("entity_type").HasMaxLength(100).IsRequired();
        builder.Property(l => l.EntityId).HasColumnName("entity_id").HasMaxLength(100);
        builder.Property(l => l.OldValuesJson).HasColumnName("old_values_json").HasColumnType("jsonb");
        builder.Property(l => l.NewValuesJson).HasColumnName("new_values_json").HasColumnType("jsonb");
        builder.Property(l => l.CorrelationId).HasColumnName("correlation_id").HasMaxLength(100);
        builder.Property(l => l.IpAddress).HasColumnName("ip_address").HasMaxLength(50);
        builder.Property(l => l.CreatedAtUtc).HasColumnName("created_at_utc").IsRequired();

        builder.HasIndex(l => l.ActorUserId);
        builder.HasIndex(l => l.CreatedAtUtc);
    }
}
