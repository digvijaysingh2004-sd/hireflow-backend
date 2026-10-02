namespace HireFlow.Notification.Application.DTOs;

public record PagedResult<T>(
    IReadOnlyList<T> Items,
    int Page,
    int PageSize,
    int TotalCount,
    int TotalPages,
    bool HasNextPage
)
{
    public static PagedResult<T> Create(IReadOnlyList<T> items, int page, int pageSize, int totalCount)
    {
        var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);
        return new PagedResult<T>(
            items,
            page,
            pageSize,
            totalCount,
            totalPages,
            page < totalPages
        );
    }
}

// Internal notification requests
public record SendEmailNotificationRequest(
    Guid? EventId,
    string TemplateKey,
    string RecipientEmail,
    Dictionary<string, string>? Payload,
    string? IdempotencyKey
);

public record SendOtpNotificationRequest(
    Guid? EventId,
    string RecipientEmail,
    string Purpose,
    string Otp,
    DateTimeOffset ExpiresAtUtc
);

public record NotificationQueuedResponse(
    Guid NotificationId,
    string Status
);

// Template DTOs
public record CreateTemplateRequest(
    string TemplateKey,
    string SubjectTemplate,
    string BodyTemplate,
    string? Locale
);

public record UpdateTemplateRequest(
    string SubjectTemplate,
    string BodyTemplate,
    bool IsActive
);

public record TemplateDto(
    Guid Id,
    string TemplateKey,
    string SubjectTemplate,
    string BodyTemplate,
    string Locale,
    int Version,
    bool IsActive,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset UpdatedAtUtc
);

// Delivery / Log DTOs
public record NotificationDeliveryDto(
    Guid Id,
    string TemplateKey,
    string RecipientEmailMasked,
    string Status,
    int AttemptCount,
    string? ErrorMessage,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset? SentAtUtc
);

public record RetryNotificationRequest(
    string? Reason
);

// Outbox DTOs
public record OutboxMessageDto(
    Guid Id,
    string EventType,
    string Status,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset? ProcessedAtUtc
);
