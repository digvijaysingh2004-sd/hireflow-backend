using System.Text.RegularExpressions;
using HireFlow.Notification.Application.Common;
using HireFlow.Notification.Application.DTOs;
using HireFlow.Notification.Application.Interfaces;
using HireFlow.Notification.Domain.Entities;
using HireFlow.Notification.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace HireFlow.Notification.Infrastructure.Services;

public class EmailNotificationService : IEmailNotificationService
{
    private readonly NotificationDbContext _context;
    private readonly IEmailSender _emailSender;
    private readonly ILogger<EmailNotificationService> _logger;

    public EmailNotificationService(
        NotificationDbContext context,
        IEmailSender emailSender,
        ILogger<EmailNotificationService> logger)
    {
        _context = context;
        _emailSender = emailSender;
        _logger = logger;
    }

    public async Task<Result<NotificationQueuedResponse>> SendEmailAsync(
        SendEmailNotificationRequest request,
        CancellationToken cancellationToken = default)
    {
        // 1. Check idempotency if key provided
        if (!string.IsNullOrWhiteSpace(request.IdempotencyKey))
        {
            var existing = await _context.Notifications
                .AsNoTracking()
                .FirstOrDefaultAsync(n => n.IdempotencyKey == request.IdempotencyKey, cancellationToken);

            if (existing != null)
            {
                return Result<NotificationQueuedResponse>.Success(
                    new NotificationQueuedResponse(existing.Id, existing.Status), 200);
            }
        }

        // 2. Fetch template
        var template = await _context.Templates
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.TemplateKey == request.TemplateKey && t.IsActive, cancellationToken);

        string subject;
        string body;

        if (template != null)
        {
            subject = RenderTemplate(template.SubjectTemplate, request.Payload);
            body = RenderTemplate(template.BodyTemplate, request.Payload);
        }
        else
        {
            subject = $"HireFlow Notification: {request.TemplateKey}";
            body = $"Notification for {request.TemplateKey}. Payload: " +
                   string.Join(", ", request.Payload?.Select(kv => $"{kv.Key}: {kv.Value}") ?? Array.Empty<string>());
        }

        var maskedEmail = MaskEmail(request.RecipientEmail);

        var notification = new EmailNotification
        {
            EventId = request.EventId,
            TemplateKey = request.TemplateKey,
            RecipientEmail = request.RecipientEmail.Trim(),
            RecipientEmailMasked = maskedEmail,
            Subject = subject,
            Body = body,
            Status = "Queued",
            AttemptCount = 1,
            IdempotencyKey = request.IdempotencyKey?.Trim(),
            CreatedAtUtc = DateTimeOffset.UtcNow
        };

        // 3. Attempt direct delivery
        var (sent, error) = await _emailSender.SendEmailAsync(notification.RecipientEmail, notification.Subject, notification.Body, cancellationToken);
        if (sent)
        {
            notification.Status = "Sent";
            notification.SentAtUtc = DateTimeOffset.UtcNow;
            notification.ErrorMessage = null;
        }
        else
        {
            notification.Status = "Failed";
            notification.ErrorMessage = error;
        }

        _context.Notifications.Add(notification);
        await _context.SaveChangesAsync(cancellationToken);

        return Result<NotificationQueuedResponse>.Success(
            new NotificationQueuedResponse(notification.Id, notification.Status), 202);
    }

    public async Task<Result<NotificationQueuedResponse>> SendOtpEmailAsync(
        SendOtpNotificationRequest request,
        CancellationToken cancellationToken = default)
    {
        var subject = $"Your HireFlow Verification Code: {request.Otp}";
        var body = $@"<html>
<body>
  <h2>HireFlow Verification</h2>
  <p>Purpose: <strong>{request.Purpose}</strong></p>
  <p>Your one-time code is: <strong style='font-size: 24px; color: #2563eb;'>{request.Otp}</strong></p>
  <p>This code will expire at: {request.ExpiresAtUtc:yyyy-MM-dd HH:mm:ss} UTC.</p>
  <p>If you did not request this code, please ignore this email.</p>
</body>
</html>";

        var maskedEmail = MaskEmail(request.RecipientEmail);

        var notification = new EmailNotification
        {
            EventId = request.EventId,
            TemplateKey = "OtpVerification",
            RecipientEmail = request.RecipientEmail.Trim(),
            RecipientEmailMasked = maskedEmail,
            Subject = $"HireFlow Verification Code ({request.Purpose})",
            Body = $"One-time verification code sent for {request.Purpose}.", // Do not store plaintext OTP in database body
            Status = "Queued",
            AttemptCount = 1,
            CreatedAtUtc = DateTimeOffset.UtcNow
        };

        var (sent, error) = await _emailSender.SendEmailAsync(request.RecipientEmail, subject, body, cancellationToken);
        if (sent)
        {
            notification.Status = "Sent";
            notification.SentAtUtc = DateTimeOffset.UtcNow;
            notification.ErrorMessage = null;
        }
        else
        {
            notification.Status = "Failed";
            notification.ErrorMessage = error;
        }

        _context.Notifications.Add(notification);
        await _context.SaveChangesAsync(cancellationToken);

        return Result<NotificationQueuedResponse>.Success(
            new NotificationQueuedResponse(notification.Id, notification.Status), 202);
    }

    public async Task<Result<NotificationDeliveryDto>> GetNotificationByIdAsync(
        Guid notificationId,
        CancellationToken cancellationToken = default)
    {
        var item = await _context.Notifications
            .AsNoTracking()
            .FirstOrDefaultAsync(n => n.Id == notificationId, cancellationToken);

        if (item == null)
        {
            return Result<NotificationDeliveryDto>.Failure("Notification not found.", 404);
        }

        return Result<NotificationDeliveryDto>.Success(new NotificationDeliveryDto(
            item.Id,
            item.TemplateKey,
            item.RecipientEmailMasked,
            item.Status,
            item.AttemptCount,
            item.ErrorMessage,
            item.CreatedAtUtc,
            item.SentAtUtc
        ));
    }

    public async Task<Result<PagedResult<NotificationDeliveryDto>>> GetNotificationsPagedAsync(
        int page,
        int pageSize,
        string? status,
        string? templateKey,
        CancellationToken cancellationToken = default)
    {
        if (page < 1) page = 1;
        if (pageSize < 1) pageSize = 10;
        if (pageSize > 100) pageSize = 100;

        var query = _context.Notifications.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(status))
        {
            var normalizedStatus = status.Trim().ToLower();
            query = query.Where(n => n.Status.ToLower() == normalizedStatus);
        }

        if (!string.IsNullOrWhiteSpace(templateKey))
        {
            var normalizedKey = templateKey.Trim().ToLower();
            query = query.Where(n => n.TemplateKey.ToLower() == normalizedKey);
        }

        query = query.OrderByDescending(n => n.CreatedAtUtc);

        var totalItems = await query.CountAsync(cancellationToken);
        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(n => new NotificationDeliveryDto(
                n.Id,
                n.TemplateKey,
                n.RecipientEmailMasked,
                n.Status,
                n.AttemptCount,
                n.ErrorMessage,
                n.CreatedAtUtc,
                n.SentAtUtc
            ))
            .ToListAsync(cancellationToken);

        return Result<PagedResult<NotificationDeliveryDto>>.Success(
            PagedResult<NotificationDeliveryDto>.Create(items, page, pageSize, totalItems));
    }

    public async Task<Result<NotificationQueuedResponse>> RetryNotificationAsync(
        Guid notificationId,
        string? reason,
        CancellationToken cancellationToken = default)
    {
        var notification = await _context.Notifications
            .FirstOrDefaultAsync(n => n.Id == notificationId, cancellationToken);

        if (notification == null)
        {
            return Result<NotificationQueuedResponse>.Failure("Notification not found.", 404);
        }

        notification.AttemptCount += 1;
        var (sent, error) = await _emailSender.SendEmailAsync(notification.RecipientEmail, notification.Subject, notification.Body, cancellationToken);
        if (sent)
        {
            notification.Status = "Sent";
            notification.SentAtUtc = DateTimeOffset.UtcNow;
            notification.ErrorMessage = null;
        }
        else
        {
            notification.Status = "Failed";
            notification.ErrorMessage = error;
        }

        await _context.SaveChangesAsync(cancellationToken);

        return Result<NotificationQueuedResponse>.Success(
            new NotificationQueuedResponse(notification.Id, notification.Status), 202);
    }

    private static string RenderTemplate(string template, Dictionary<string, string>? payload)
    {
        if (string.IsNullOrWhiteSpace(template) || payload == null)
        {
            return template;
        }

        var result = template;
        foreach (var (key, val) in payload)
        {
            result = result.Replace($"{{{{{key}}}}}", val ?? string.Empty);
        }
        return result;
    }

    private static string MaskEmail(string email)
    {
        if (string.IsNullOrWhiteSpace(email) || !email.Contains('@'))
        {
            return "***@***";
        }

        var parts = email.Split('@');
        var name = parts[0];
        var domain = parts[1];

        var maskedName = name.Length switch
        {
            1 => name + "***",
            2 => name[0] + "***",
            _ => name[0] + "***" + name[^1]
        };

        return $"{maskedName}@{domain}";
    }
}
