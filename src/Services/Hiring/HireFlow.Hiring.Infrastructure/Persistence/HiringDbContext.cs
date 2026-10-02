using HireFlow.Hiring.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace HireFlow.Hiring.Infrastructure.Persistence;

public class HiringDbContext : DbContext
{
    public HiringDbContext(DbContextOptions<HiringDbContext> options) : base(options)
    {
    }

    public DbSet<Company> Companies => Set<Company>();
    public DbSet<Job> Jobs => Set<Job>();
    public DbSet<JobApplication> Applications => Set<JobApplication>();
    public DbSet<ApplicationStatusHistory> ApplicationStatusHistories => Set<ApplicationStatusHistory>();
    public DbSet<Interview> Interviews => Set<Interview>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(HiringDbContext).Assembly);
    }
}
