using HireFlow.Identity.Domain.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace HireFlow.Identity.Infrastructure.Persistence;

public static class IdentityDbSeeder
{
    public static async Task SeedAsync(IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<IdentityDbContext>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<IdentityDbContext>>();

        if (context.Database.IsRelational())
        {
            await context.Database.MigrateAsync();
        }
        else
        {
            await context.Database.EnsureCreatedAsync();
        }

        // 1. Seed Roles
        var defaultRoles = new[]
        {
            new Role { Id = Guid.Parse("11111111-1111-1111-1111-111111111111"), Name = Role.Admin, Description = "Platform Administrator" },
            new Role { Id = Guid.Parse("22222222-2222-2222-2222-222222222222"), Name = Role.Recruiter, Description = "Recruiter managing postings and applicants" },
            new Role { Id = Guid.Parse("33333333-3333-3333-3333-333333333333"), Name = Role.HiringManager, Description = "Hiring Manager reviewing applications and scheduling interviews" },
            new Role { Id = Guid.Parse("44444444-4444-4444-4444-444444444444"), Name = Role.Candidate, Description = "Candidate applying for jobs" }
        };

        foreach (var role in defaultRoles)
        {
            if (!await context.Roles.AnyAsync(r => r.Name == role.Name))
            {
                await context.Roles.AddAsync(role);
            }
        }
        await context.SaveChangesAsync();

        // 2. Seed Default User Logins for testing/demo
        var passwordHasher = scope.ServiceProvider.GetRequiredService<HireFlow.Identity.Application.Interfaces.IPasswordHasher>();

        var adminRole = await context.Roles.FirstAsync(r => r.Name == Role.Admin);
        var recruiterRole = await context.Roles.FirstAsync(r => r.Name == Role.Recruiter);
        var hiringManagerRole = await context.Roles.FirstAsync(r => r.Name == Role.HiringManager);
        var candidateRole = await context.Roles.FirstAsync(r => r.Name == Role.Candidate);

        var seedUsers = new (User User, string Password, Role Role)[]
        {
            (
                new User
                {
                    Id = Guid.Parse("a0000000-0000-0000-0000-000000000001"),
                    Email = "admin@hireflow.local",
                    FirstName = "System",
                    LastName = "Admin",
                    IsEmailVerified = true,
                    IsActive = true
                },
                "Admin123!",
                adminRole
            ),
            (
                new User
                {
                    Id = Guid.Parse("a0000000-0000-0000-0000-000000000002"),
                    Email = "recruiter@hireflow.local",
                    FirstName = "Jane",
                    LastName = "Recruiter",
                    IsEmailVerified = true,
                    IsActive = true
                },
                "Recruiter123!",
                recruiterRole
            ),
            (
                new User
                {
                    Id = Guid.Parse("a0000000-0000-0000-0000-000000000003"),
                    Email = "manager@hireflow.local",
                    FirstName = "Alex",
                    LastName = "Manager",
                    IsEmailVerified = true,
                    IsActive = true
                },
                "Manager123!",
                hiringManagerRole
            ),
            (
                new User
                {
                    Id = Guid.Parse("a0000000-0000-0000-0000-000000000004"),
                    Email = "candidate@hireflow.local",
                    FirstName = "John",
                    LastName = "Candidate",
                    IsEmailVerified = true,
                    IsActive = true
                },
                "Candidate123!",
                candidateRole
            )
        };

        foreach (var (user, password, role) in seedUsers)
        {
            var existing = await context.Users.FirstOrDefaultAsync(u => u.Email == user.Email);
            if (existing == null)
            {
                user.PasswordHash = passwordHasher.HashPassword(password);
                await context.Users.AddAsync(user);
                await context.UserRoles.AddAsync(new UserRole
                {
                    UserId = user.Id,
                    RoleId = role.Id
                });
                logger.LogInformation("Seeded demo user: {Email} with role {Role}", user.Email, role.Name);
            }
            else
            {
                existing.PasswordHash = passwordHasher.HashPassword(password);
                existing.FailedLoginAttempts = 0;
                existing.LockedUntilUtc = null;
                existing.IsActive = true;
                existing.IsEmailVerified = true;
            }
        }

        await context.SaveChangesAsync();
    }
}
