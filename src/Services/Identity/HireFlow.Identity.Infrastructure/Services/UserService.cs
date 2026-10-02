using HireFlow.Identity.Application.Common;
using HireFlow.Identity.Application.DTOs;
using HireFlow.Identity.Application.Interfaces;
using HireFlow.Identity.Domain.Entities;
using HireFlow.Identity.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace HireFlow.Identity.Infrastructure.Services;

public class UserService : IUserService
{
    private readonly IdentityDbContext _dbContext;
    private readonly IPasswordHasher _passwordHasher;
    private readonly ILogger<UserService> _logger;

    public UserService(
        IdentityDbContext dbContext,
        IPasswordHasher passwordHasher,
        ILogger<UserService> logger)
    {
        _dbContext = dbContext;
        _passwordHasher = passwordHasher;
        _logger = logger;
    }

    public async Task<Result<IReadOnlyList<SessionDto>>> GetSessionsAsync(
        Guid userId, 
        string? currentTokenHash, 
        CancellationToken cancellationToken = default)
    {
        var tokens = await _dbContext.RefreshTokens
            .AsNoTracking()
            .Where(t => t.UserId == userId && t.RevokedAtUtc == null && t.ExpiresAtUtc > DateTimeOffset.UtcNow)
            .OrderByDescending(t => t.CreatedAtUtc)
            .ToListAsync(cancellationToken);

        var sessions = tokens.Select(t => new SessionDto(
            t.Id,
            t.CreatedAtUtc,
            t.ExpiresAtUtc,
            t.CreatedByIp?.ToString(),
            currentTokenHash != null && t.TokenHash == currentTokenHash
        )).ToList();

        return Result<IReadOnlyList<SessionDto>>.Success(sessions);
    }

    public async Task<Result<bool>> RevokeSessionAsync(
        Guid userId, 
        Guid sessionId, 
        CancellationToken cancellationToken = default)
    {
        var token = await _dbContext.RefreshTokens
            .FirstOrDefaultAsync(t => t.Id == sessionId && t.UserId == userId, cancellationToken);

        if (token == null)
        {
            return Result<bool>.Failure("Session not found.", 404);
        }

        if (token.RevokedAtUtc == null)
        {
            token.RevokedAtUtc = DateTimeOffset.UtcNow;
            token.RevokedReason = "Revoked by user";

            _dbContext.AuditLogs.Add(new AuditLog
            {
                ActorUserId = userId,
                Action = "SessionRevoked",
                EntityType = "RefreshToken",
                EntityId = sessionId.ToString(),
                CreatedAtUtc = DateTimeOffset.UtcNow
            });

            await _dbContext.SaveChangesAsync(cancellationToken);
        }

        return Result<bool>.Success(true);
    }

    public async Task<Result<bool>> RevokeAllOtherSessionsAsync(
        Guid userId, 
        string? currentTokenHash, 
        CancellationToken cancellationToken = default)
    {
        var query = _dbContext.RefreshTokens
            .Where(t => t.UserId == userId && t.RevokedAtUtc == null && t.ExpiresAtUtc > DateTimeOffset.UtcNow);

        if (!string.IsNullOrWhiteSpace(currentTokenHash))
        {
            query = query.Where(t => t.TokenHash != currentTokenHash);
        }

        var tokens = await query.ToListAsync(cancellationToken);

        foreach (var t in tokens)
        {
            t.RevokedAtUtc = DateTimeOffset.UtcNow;
            t.RevokedReason = "Revoked by user (all other sessions)";
        }

        _dbContext.AuditLogs.Add(new AuditLog
        {
            ActorUserId = userId,
            Action = "AllOtherSessionsRevoked",
            EntityType = "User",
            EntityId = userId.ToString(),
            CreatedAtUtc = DateTimeOffset.UtcNow
        });

        await _dbContext.SaveChangesAsync(cancellationToken);
        return Result<bool>.Success(true);
    }

    public async Task<Result<UserDto>> GetProfileAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var user = await _dbContext.Users
            .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);

        if (user == null)
        {
            return Result<UserDto>.Failure("User not found.", 404);
        }

        var roles = user.UserRoles.Select(ur => ur.Role.Name).ToList();
        return Result<UserDto>.Success(new UserDto(user.Id, user.Email, user.FirstName, user.LastName, roles));
    }

    public async Task<Result<UserDto>> UpdateProfileAsync(
        Guid userId, 
        UpdateProfileRequest request, 
        CancellationToken cancellationToken = default)
    {
        var user = await _dbContext.Users
            .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
            .FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);

        if (user == null)
        {
            return Result<UserDto>.Failure("User not found.", 404);
        }

        user.FirstName = request.FirstName.Trim();
        user.LastName = request.LastName.Trim();
        user.UpdatedAtUtc = DateTimeOffset.UtcNow;

        _dbContext.AuditLogs.Add(new AuditLog
        {
            ActorUserId = userId,
            Action = "ProfileUpdated",
            EntityType = "User",
            EntityId = userId.ToString(),
            CreatedAtUtc = DateTimeOffset.UtcNow
        });

        await _dbContext.SaveChangesAsync(cancellationToken);

        var roles = user.UserRoles.Select(ur => ur.Role.Name).ToList();
        return Result<UserDto>.Success(new UserDto(user.Id, user.Email, user.FirstName, user.LastName, roles));
    }

    public async Task<Result<bool>> ChangePasswordAsync(
        Guid userId, 
        ChangePasswordRequest request, 
        CancellationToken cancellationToken = default)
    {
        var user = await _dbContext.Users
            .FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);

        if (user == null)
        {
            return Result<bool>.Failure("User not found.", 404);
        }

        bool isValid = _passwordHasher.VerifyPassword(request.CurrentPassword, user.PasswordHash);
        if (!isValid)
        {
            return Result<bool>.Failure("Incorrect current password.", 400);
        }

        user.PasswordHash = _passwordHasher.HashPassword(request.NewPassword);
        user.UpdatedAtUtc = DateTimeOffset.UtcNow;

        // Revoke active sessions for security
        var activeTokens = await _dbContext.RefreshTokens
            .Where(t => t.UserId == userId && t.RevokedAtUtc == null)
            .ToListAsync(cancellationToken);

        foreach (var t in activeTokens)
        {
            t.RevokedAtUtc = DateTimeOffset.UtcNow;
            t.RevokedReason = "Password changed";
        }

        _dbContext.AuditLogs.Add(new AuditLog
        {
            ActorUserId = userId,
            Action = "PasswordChanged",
            EntityType = "User",
            EntityId = userId.ToString(),
            CreatedAtUtc = DateTimeOffset.UtcNow
        });

        await _dbContext.SaveChangesAsync(cancellationToken);
        return Result<bool>.Success(true);
    }

    public async Task<Result<PagedResult<UserDto>>> GetUsersPagedAsync(
        int page, 
        int pageSize, 
        string? search, 
        string? role, 
        bool? isActive, 
        CancellationToken cancellationToken = default)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var query = _dbContext.Users
            .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
            .AsNoTracking()
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.Trim().ToLower();
            query = query.Where(u => u.Email.ToLower().Contains(s) || 
                                     u.FirstName.ToLower().Contains(s) || 
                                     u.LastName.ToLower().Contains(s));
        }

        if (!string.IsNullOrWhiteSpace(role))
        {
            query = query.Where(u => u.UserRoles.Any(ur => ur.Role.Name.ToLower() == role.Trim().ToLower()));
        }

        if (isActive.HasValue)
        {
            query = query.Where(u => u.IsActive == isActive.Value);
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var users = await query
            .OrderByDescending(u => u.CreatedAtUtc)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        var dtos = users.Select(u => new UserDto(
            u.Id,
            u.Email,
            u.FirstName,
            u.LastName,
            u.UserRoles.Select(ur => ur.Role.Name).ToList()
        )).ToList();

        var paged = PagedResult<UserDto>.Create(dtos, page, pageSize, totalCount);
        return Result<PagedResult<UserDto>>.Success(paged);
    }

    public async Task<Result<UserDto>> GetUserByIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var user = await _dbContext.Users
            .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);

        if (user == null)
        {
            return Result<UserDto>.Failure("User not found.", 404);
        }

        var roles = user.UserRoles.Select(ur => ur.Role.Name).ToList();
        return Result<UserDto>.Success(new UserDto(user.Id, user.Email, user.FirstName, user.LastName, roles));
    }

    public async Task<Result<bool>> UpdateUserStatusAsync(Guid userId, bool isActive, CancellationToken cancellationToken = default)
    {
        var user = await _dbContext.Users
            .FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);

        if (user == null)
        {
            return Result<bool>.Failure("User not found.", 404);
        }

        user.IsActive = isActive;
        user.UpdatedAtUtc = DateTimeOffset.UtcNow;

        if (!isActive)
        {
            var activeTokens = await _dbContext.RefreshTokens
                .Where(t => t.UserId == userId && t.RevokedAtUtc == null)
                .ToListAsync(cancellationToken);

            foreach (var t in activeTokens)
            {
                t.RevokedAtUtc = DateTimeOffset.UtcNow;
                t.RevokedReason = "Account deactivated by administrator";
            }
        }

        _dbContext.AuditLogs.Add(new AuditLog
        {
            ActorUserId = userId,
            Action = isActive ? "UserActivated" : "UserDeactivated",
            EntityType = "User",
            EntityId = userId.ToString(),
            CreatedAtUtc = DateTimeOffset.UtcNow
        });

        await _dbContext.SaveChangesAsync(cancellationToken);
        return Result<bool>.Success(true);
    }

    public async Task<Result<bool>> UpdateUserRolesAsync(Guid userId, IReadOnlyList<string> roleNames, CancellationToken cancellationToken = default)
    {
        var user = await _dbContext.Users
            .Include(u => u.UserRoles)
            .FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);

        if (user == null)
        {
            return Result<bool>.Failure("User not found.", 404);
        }

        var requestedRoles = await _dbContext.Roles
            .Where(r => roleNames.Contains(r.Name))
            .ToListAsync(cancellationToken);

        if (requestedRoles.Count == 0)
        {
            return Result<bool>.Failure("None of the specified roles exist.", 400);
        }

        user.UserRoles.Clear();
        foreach (var r in requestedRoles)
        {
            user.UserRoles.Add(new UserRole
            {
                UserId = user.Id,
                RoleId = r.Id
            });
        }
        user.UpdatedAtUtc = DateTimeOffset.UtcNow;

        _dbContext.AuditLogs.Add(new AuditLog
        {
            ActorUserId = userId,
            Action = "UserRolesUpdated",
            EntityType = "User",
            EntityId = userId.ToString(),
            NewValuesJson = $"{{\"roles\": [\"{string.Join("\", \"", requestedRoles.Select(r => r.Name))}\"]}}",
            CreatedAtUtc = DateTimeOffset.UtcNow
        });

        await _dbContext.SaveChangesAsync(cancellationToken);
        return Result<bool>.Success(true);
    }
}

public class RoleService : IRoleService
{
    private readonly IdentityDbContext _dbContext;

    public RoleService(IdentityDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Result<IReadOnlyList<RoleDto>>> GetRolesAsync(CancellationToken cancellationToken = default)
    {
        var roles = await _dbContext.Roles
            .AsNoTracking()
            .OrderBy(r => r.Name)
            .Select(r => new RoleDto(r.Id, r.Name, r.Description))
            .ToListAsync(cancellationToken);

        return Result<IReadOnlyList<RoleDto>>.Success(roles);
    }
}
