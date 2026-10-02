using HireFlow.Notification.Application.Common;
using HireFlow.Notification.Application.DTOs;

namespace HireFlow.Notification.Application.Interfaces;

public interface IEmailNotificationService
{
    Task<Result<NotificationQueuedResponse>> SendEmailAsync(SendEmailNotificationRequest request, CancellationToken cancellationToken = default);
    Task<Result<NotificationQueuedResponse>> SendOtpEmailAsync(SendOtpNotificationRequest request, CancellationToken cancellationToken = default);
    Task<Result<NotificationDeliveryDto>> GetNotificationByIdAsync(Guid notificationId, CancellationToken cancellationToken = default);
    Task<Result<PagedResult<NotificationDeliveryDto>>> GetNotificationsPagedAsync(int page, int pageSize, string? status, string? templateKey, CancellationToken cancellationToken = default);
    Task<Result<NotificationQueuedResponse>> RetryNotificationAsync(Guid notificationId, string? reason, CancellationToken cancellationToken = default);
}

public interface ITemplateService
{
    Task<Result<TemplateDto>> CreateTemplateAsync(CreateTemplateRequest request, CancellationToken cancellationToken = default);
    Task<Result<PagedResult<TemplateDto>>> GetTemplatesPagedAsync(int page, int pageSize, string? search, bool? activeOnly, CancellationToken cancellationToken = default);
    Task<Result<TemplateDto>> GetTemplateByIdAsync(Guid templateId, CancellationToken cancellationToken = default);
    Task<Result<TemplateDto>> UpdateTemplateAsync(Guid templateId, UpdateTemplateRequest request, CancellationToken cancellationToken = default);
    Task<Result<bool>> DeactivateTemplateAsync(Guid templateId, CancellationToken cancellationToken = default);
}

public interface IOutboxService
{
    Task<Result<PagedResult<OutboxMessageDto>>> GetOutboxMessagesPagedAsync(int page, int pageSize, string? status, CancellationToken cancellationToken = default);
    Task<Result<bool>> PublishOutboxMessageAsync(Guid messageId, CancellationToken cancellationToken = default);
}
