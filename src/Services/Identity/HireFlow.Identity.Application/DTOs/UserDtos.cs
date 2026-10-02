namespace HireFlow.Identity.Application.DTOs;

public record ResendOtpRequest(
    string Email,
    string? Purpose = "Registration"
);

public record SessionDto(
    Guid Id,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset ExpiresAtUtc,
    string? CreatedByIp,
    bool IsCurrent
);

public record UpdateProfileRequest(
    string FirstName,
    string LastName
);

public record ChangePasswordRequest(
    string CurrentPassword,
    string NewPassword
);

public record UpdateUserStatusRequest(
    bool IsActive
);

public record UpdateUserRolesRequest(
    IReadOnlyList<string> Roles
);

public record RoleDto(
    Guid Id,
    string Name,
    string? Description
);

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

