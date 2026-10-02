using HireFlow.Notification.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace HireFlow.Notification.Infrastructure.Persistence;

public static class NotificationDbSeeder
{
    public static async Task SeedAsync(NotificationDbContext context, ILogger logger)
    {
        if (await context.Templates.AnyAsync())
        {
            return;
        }

        logger.LogInformation("Seeding default email templates into Notification database...");

        var templates = new List<EmailTemplate>
        {
            new()
            {
                TemplateKey = "ApplicationSubmitted",
                SubjectTemplate = "Your application for {{jobTitle}} at {{companyName}} was received",
                BodyTemplate = "<h2>HireFlow Application Received</h2><p>Hello {{candidateName}},</p><p>Thank you for applying for the <strong>{{jobTitle}}</strong> position at <strong>{{companyName}}</strong>. Our recruiting team has received your application and will review your profile shortly.</p><p>Best regards,<br/>The {{companyName}} Hiring Team</p>",
                Locale = "en-US",
                Version = 1,
                IsActive = true
            },
            new()
            {
                TemplateKey = "InterviewScheduled",
                SubjectTemplate = "Interview Scheduled for {{jobTitle}} at {{companyName}}",
                BodyTemplate = "<h2>HireFlow Interview Invitation</h2><p>Hello {{candidateName}},</p><p>Great news! An interview has been scheduled for your application for <strong>{{jobTitle}}</strong> at <strong>{{companyName}}</strong>.</p><p>Meeting Details: <a href='{{meetingUrl}}'>Join Meeting</a></p><p>We look forward to speaking with you!</p>",
                Locale = "en-US",
                Version = 1,
                IsActive = true
            },
            new()
            {
                TemplateKey = "ApplicationStatusChanged",
                SubjectTemplate = "Update on your application for {{jobTitle}}",
                BodyTemplate = "<h2>HireFlow Application Update</h2><p>Hello {{candidateName}},</p><p>The status of your application for <strong>{{jobTitle}}</strong> has been updated to: <strong>{{status}}</strong>.</p><p>Log in to your HireFlow portal to view the details.</p>",
                Locale = "en-US",
                Version = 1,
                IsActive = true
            },
            new()
            {
                TemplateKey = "OtpVerification",
                SubjectTemplate = "Your HireFlow Verification Code: {{otp}}",
                BodyTemplate = "<h2>HireFlow Verification Code</h2><p>Your one-time verification code is: <strong style='font-size:24px; color:#2563eb;'>{{otp}}</strong></p><p>This code will expire in 15 minutes. Please do not share this code with anyone.</p>",
                Locale = "en-US",
                Version = 1,
                IsActive = true
            }
        };

        context.Templates.AddRange(templates);
        await context.SaveChangesAsync();
        logger.LogInformation("Default email templates seeded successfully.");
    }
}
