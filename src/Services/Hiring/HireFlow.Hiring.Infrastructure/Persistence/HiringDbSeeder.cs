using HireFlow.Hiring.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace HireFlow.Hiring.Infrastructure.Persistence;

public static class HiringDbSeeder
{
    public static async Task SeedAsync(IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<HiringDbContext>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<HiringDbContext>>();

        if (!await context.Companies.AnyAsync())
        {
            var defaultCompany = new Company
            {
                Id = Guid.Parse("c0000000-0000-0000-0000-000000000001"),
                Name = "HireFlow Technologies",
                Slug = "hireflow-technologies",
                Website = "https://hireflow.local",
                Description = "Modern recruiting & hiring intelligence platform.",
                CreatedByUserId = Guid.Parse("a0000000-0000-0000-0000-000000000001"),
                CreatedAtUtc = DateTimeOffset.UtcNow,
                UpdatedAtUtc = DateTimeOffset.UtcNow
            };

            var partnerCompany = new Company
            {
                Id = Guid.Parse("c0000000-0000-0000-0000-000000000002"),
                Name = "Acme Global Systems",
                Slug = "acme-global-systems",
                Website = "https://acme.example.com",
                Description = "Enterprise cloud solutions & digital engineering.",
                CreatedByUserId = Guid.Parse("a0000000-0000-0000-0000-000000000001"),
                CreatedAtUtc = DateTimeOffset.UtcNow,
                UpdatedAtUtc = DateTimeOffset.UtcNow
            };

            await context.Companies.AddRangeAsync(defaultCompany, partnerCompany);
            await context.SaveChangesAsync();
            logger.LogInformation("Seeded default companies in Hiring service.");
        }
    }
}
