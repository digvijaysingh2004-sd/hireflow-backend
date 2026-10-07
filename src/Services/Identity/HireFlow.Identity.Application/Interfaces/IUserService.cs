using HireFlow.Identity.Application.Common;
using HireFlow.Identity.Application.DTOs;

namespace HireFlow.Identity.Application.Interfaces;

public interface IUserService
{
    Task<Result<IReadOnlyList<SessionDto>>> GetSessionsAsync(Guid userId, string? currentTokenHash, CancellationToken cancellationToken = default);
    Task<Result<bool>> RevokeSessionAsync(Guid userId, Guid sessionId, CancellationToken cancellationToken = default);
    Task<Result<bool>> RevokeAllOtherSessionsAsync(Guid userId, string? currentTokenHash, CancellationToken cancellationToken = default);
    Task<Result<UserDto>> GetProfileAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<Result<UserDto>> UpdateProfileAsync(Guid userId, UpdateProfileRequest request, CancellationToken cancellationToken = default);
    Task<Result<bool>> ChangePasswordAsync(Guid userId, ChangePasswordRequest request, CancellationToken cancellationToken = default);
    Task<Result<PagedResult<UserDto>>> GetUsersPagedAsync(int page, int pageSize, string? search, string? role, bool? isActive, CancellationToken cancellationToken = default);
    Task<Result<UserDto>> GetUserByIdAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<Result<bool>> UpdateUserStatusAsync(Guid userId, bool isActive, CancellationToken cancellationToken = default);
    Task<Result<bool>> UpdateUserRolesAsync(Guid userId, IReadOnlyList<string> roleNames, CancellationToken cancellationToken = default);
    Task<Result<bool>> DeleteUserAsync(Guid userId, CancellationToken cancellationToken = default);
}

public interface IRoleService
{
    Task<Result<IReadOnlyList<RoleDto>>> GetRolesAsync(CancellationToken cancellationToken = default);
}

