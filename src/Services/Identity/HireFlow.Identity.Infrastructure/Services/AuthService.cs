using System.Net;
using HireFlow.Identity.Application.Common;
using HireFlow.Identity.Application.DTOs;
using HireFlow.Identity.Application.Interfaces;
using HireFlow.Identity.Domain.Entities;
using HireFlow.Identity.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace HireFlow.Identity.Infrastructure.Services;

public class AuthService : IAuthService
{
    private readonly IdentityDbContext _dbContext;
    private readonly IPasswordHasher _passwordHasher;
    private readonly ITokenService _tokenService;
    private readonly INotificationClient _notificationClient;
    private readonly ILogger<AuthService> _logger;

    public AuthService(
        IdentityDbContext dbContext,
        IPasswordHasher passwordHasher,
        ITokenService tokenService,
        INotificationClient notificationClient,
        ILogger<AuthService> logger)
    {
        _dbContext = dbContext;
        _passwordHasher = passwordHasher;
        _tokenService = tokenService;
        _notificationClient = notificationClient;
        _logger = logger;
    }

    public async Task<Result<Guid>> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken = default)
    {
        var normalizedEmail = request.Email.Trim().ToLowerInvariant();

        var existingUser = await _dbContext.Users
            .AsNoTracking()
            .AnyAsync(u => u.Email == normalizedEmail, cancellationToken);

        if (existingUser)
        {
            return Result<Guid>.Failure("A user with this email address already exists.", 409);
        }

        // Public registration assigns Candidate; privileged roles like Admin/HiringManager are not accepted from public registration
        var candidateRole = await _dbContext.Roles
            .FirstOrDefaultAsync(r => r.Name == "Candidate", cancellationToken);

        if (candidateRole == null)
        {
            _logger.LogError("Candidate role not found in database. Initial roles must be seeded.");
            return Result<Guid>.Failure("Server configuration error: role not found.", 500);
        }

        var user = new User
        {
            Email = normalizedEmail,
            PasswordHash = _passwordHasher.HashPassword(request.Password),
            FirstName = request.FirstName.Trim(),
            LastName = request.LastName.Trim(),
            IsEmailVerified = false,
            IsActive = true,
            CreatedAtUtc = DateTimeOffset.UtcNow,
            UpdatedAtUtc = DateTimeOffset.UtcNow
        };

        user.UserRoles.Add(new UserRole
        {
            UserId = user.Id,
            RoleId = candidateRole.Id
        });

        // Generate 6-digit OTP for email verification
        var otpCode = _tokenService.GenerateOtpCode();
        var otpHash = _tokenService.HashOtp(otpCode);

        var otpChallenge = new OtpChallenge
        {
            UserId = user.Id,
            Purpose = "Registration",
            CodeHash = otpHash,
            ExpiresAtUtc = DateTimeOffset.UtcNow.AddMinutes(10),
            CreatedAtUtc = DateTimeOffset.UtcNow
        };

        _dbContext.Users.Add(user);
        _dbContext.OtpChallenges.Add(otpChallenge);

        _dbContext.AuditLogs.Add(new AuditLog
        {
            ActorUserId = user.Id,
            Action = "UserRegistered",
            EntityType = "User",
            EntityId = user.Id.ToString(),
            CreatedAtUtc = DateTimeOffset.UtcNow
        });

        await _dbContext.SaveChangesAsync(cancellationToken);

        // Log OTP in development for convenience and easy testing
        _logger.LogInformation(">>> [REGISTRATION OTP] Email: {Email}, OTP Code: {OtpCode} (Expires in 10 minutes) <<<",
            normalizedEmail, otpCode);

        try
        {
            await _notificationClient.SendOtpEmailAsync(normalizedEmail, "Registration", otpCode, otpChallenge.ExpiresAtUtc, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send registration OTP email to {Email}", normalizedEmail);
        }

        return Result<Guid>.Success(user.Id, 201);
    }

    public async Task<Result<bool>> VerifyEmailAsync(VerifyEmailRequest request, CancellationToken cancellationToken = default)
    {
        var normalizedEmail = request.Email.Trim().ToLowerInvariant();

        var user = await _dbContext.Users
            .FirstOrDefaultAsync(u => u.Email == normalizedEmail, cancellationToken);

        if (user == null)
        {
            return Result<bool>.Failure("Invalid email or verification code.", 400);
        }

        if (user.IsEmailVerified)
        {
            return Result<bool>.Success(true);
        }

        var challenge = await _dbContext.OtpChallenges
            .Where(c => c.UserId == user.Id && c.Purpose == "Registration" && c.VerifiedAtUtc == null)
            .OrderByDescending(c => c.CreatedAtUtc)
            .FirstOrDefaultAsync(cancellationToken);

        if (challenge == null || challenge.ExpiresAtUtc <= DateTimeOffset.UtcNow)
        {
            return Result<bool>.Failure("Verification code has expired or is invalid. Please request a new one.", 400);
        }

        if (challenge.AttemptCount >= 5)
        {
            return Result<bool>.Failure("Too many failed attempts. This code is invalidated.", 400);
        }

        var submittedOtpHash = _tokenService.HashOtp(request.Otp.Trim());
        if (challenge.CodeHash != submittedOtpHash)
        {
            challenge.AttemptCount++;
            await _dbContext.SaveChangesAsync(cancellationToken);
            return Result<bool>.Failure("Invalid email or verification code.", 400);
        }

        challenge.VerifiedAtUtc = DateTimeOffset.UtcNow;
        user.IsEmailVerified = true;
        user.UpdatedAtUtc = DateTimeOffset.UtcNow;

        _dbContext.AuditLogs.Add(new AuditLog
        {
            ActorUserId = user.Id,
            Action = "EmailVerified",
            EntityType = "User",
            EntityId = user.Id.ToString(),
            CreatedAtUtc = DateTimeOffset.UtcNow
        });

        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("User {Email} email successfully verified.", normalizedEmail);
        return Result<bool>.Success(true);
    }

    public async Task<Result<bool>> ResendOtpAsync(ResendOtpRequest request, CancellationToken cancellationToken = default)
    {
        var normalizedEmail = request.Email.Trim().ToLowerInvariant();
        var purpose = string.IsNullOrWhiteSpace(request.Purpose) ? "Registration" : request.Purpose.Trim();

        var user = await _dbContext.Users
            .FirstOrDefaultAsync(u => u.Email == normalizedEmail, cancellationToken);

        // Always return success to prevent email enumeration
        if (user == null || !user.IsActive)
        {
            return Result<bool>.Success(true);
        }

        if (purpose == "Registration" && user.IsEmailVerified)
        {
            return Result<bool>.Success(true);
        }

        // Invalidate older pending challenges for this purpose
        var olderChallenges = await _dbContext.OtpChallenges
            .Where(c => c.UserId == user.Id && c.Purpose == purpose && c.VerifiedAtUtc == null)
            .ToListAsync(cancellationToken);

        foreach (var c in olderChallenges)
        {
            c.VerifiedAtUtc = DateTimeOffset.UtcNow;
        }

        var otpCode = _tokenService.GenerateOtpCode();
        var otpHash = _tokenService.HashOtp(otpCode);

        var challenge = new OtpChallenge
        {
            UserId = user.Id,
            Purpose = purpose,
            CodeHash = otpHash,
            ExpiresAtUtc = DateTimeOffset.UtcNow.AddMinutes(10),
            CreatedAtUtc = DateTimeOffset.UtcNow
        };

        _dbContext.OtpChallenges.Add(challenge);

        _dbContext.AuditLogs.Add(new AuditLog
        {
            ActorUserId = user.Id,
            Action = "OtpResent",
            EntityType = "User",
            EntityId = user.Id.ToString(),
            CreatedAtUtc = DateTimeOffset.UtcNow
        });

        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(">>> [RESENT OTP] Email: {Email}, Purpose: {Purpose}, OTP Code: {OtpCode} (Expires in 10 minutes) <<<",
            normalizedEmail, purpose, otpCode);

        try
        {
            await _notificationClient.SendOtpEmailAsync(normalizedEmail, purpose, otpCode, challenge.ExpiresAtUtc, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send resend OTP email to {Email}", normalizedEmail);
        }

        return Result<bool>.Success(true);
    }

    public async Task<Result<AuthResponse>> LoginAsync(LoginRequest request, string? ipAddress, CancellationToken cancellationToken = default)
    {
        var normalizedEmail = request.Email.Trim().ToLowerInvariant();

        var user = await _dbContext.Users
            .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
            .FirstOrDefaultAsync(u => u.Email == normalizedEmail, cancellationToken);

        if (user == null)
        {
            return Result<AuthResponse>.Failure("Invalid email or password.", 401);
        }

        if (!user.IsActive)
        {
            return Result<AuthResponse>.Failure("Account is deactivated.", 403);
        }

        if (user.LockedUntilUtc.HasValue && user.LockedUntilUtc > DateTimeOffset.UtcNow)
        {
            var remaining = Math.Ceiling((user.LockedUntilUtc.Value - DateTimeOffset.UtcNow).TotalMinutes);
            return Result<AuthResponse>.Failure($"Account is temporarily locked. Try again in {remaining} minute(s).", 423);
        }

        bool isPasswordValid = _passwordHasher.VerifyPassword(request.Password, user.PasswordHash);
        if (!isPasswordValid)
        {
            user.FailedLoginAttempts++;
            if (user.FailedLoginAttempts >= 5)
            {
                user.LockedUntilUtc = DateTimeOffset.UtcNow.AddMinutes(15);
                _logger.LogWarning("Account {Email} locked due to 5 consecutive failed login attempts.", normalizedEmail);
            }

            _dbContext.AuditLogs.Add(new AuditLog
            {
                ActorUserId = user.Id,
                Action = "LoginFailed",
                EntityType = "User",
                EntityId = user.Id.ToString(),
                IpAddress = ipAddress,
                CreatedAtUtc = DateTimeOffset.UtcNow
            });

            await _dbContext.SaveChangesAsync(cancellationToken);
            return Result<AuthResponse>.Failure("Invalid email or password.", 401);
        }

        if (!user.IsEmailVerified)
        {
            return Result<AuthResponse>.Failure("Email address is not verified. Please verify your email before logging in.", 403);
        }

        // Reset failed login tracking on success
        user.FailedLoginAttempts = 0;
        user.LockedUntilUtc = null;

        var roles = user.UserRoles.Select(ur => ur.Role.Name).ToList();
        var accessToken = _tokenService.GenerateAccessToken(user, roles);
        var rawRefreshToken = _tokenService.GenerateRefreshToken();
        var tokenHash = _tokenService.HashToken(rawRefreshToken);

        var refreshTokenEntity = new RefreshToken
        {
            UserId = user.Id,
            TokenHash = tokenHash,
            ExpiresAtUtc = DateTimeOffset.UtcNow.AddDays(7),
            CreatedByIp = TryParseIp(ipAddress),
            CreatedAtUtc = DateTimeOffset.UtcNow
        };

        _dbContext.RefreshTokens.Add(refreshTokenEntity);

        _dbContext.AuditLogs.Add(new AuditLog
        {
            ActorUserId = user.Id,
            Action = "LoginSuccess",
            EntityType = "User",
            EntityId = user.Id.ToString(),
            IpAddress = ipAddress,
            CreatedAtUtc = DateTimeOffset.UtcNow
        });

        await _dbContext.SaveChangesAsync(cancellationToken);

        var userDto = new UserDto(user.Id, user.Email, user.FirstName, user.LastName, roles, user.IsActive, user.IsEmailVerified, user.CreatedAtUtc);
        var response = new AuthResponse(
            accessToken,
            _tokenService.AccessTokenExpiryMinutes * 60,
            rawRefreshToken,
            userDto
        );

        return Result<AuthResponse>.Success(response);
    }

    public async Task<Result<AuthResponse>> RefreshTokenAsync(string refreshToken, string? ipAddress, CancellationToken cancellationToken = default)
    {
        var tokenHash = _tokenService.HashToken(refreshToken);

        var existingToken = await _dbContext.RefreshTokens
            .Include(t => t.User)
                .ThenInclude(u => u.UserRoles)
                    .ThenInclude(ur => ur.Role)
            .FirstOrDefaultAsync(t => t.TokenHash == tokenHash, cancellationToken);

        if (existingToken == null)
        {
            return Result<AuthResponse>.Failure("Invalid refresh token.", 401);
        }

        // Token reuse detection: if an already-revoked token is used, assume token theft and revoke all tokens for this user!
        if (existingToken.RevokedAtUtc != null)
        {
            _logger.LogWarning("ALERT: Revoked refresh token reuse detected for User {UserId}! Revoking entire token family.", existingToken.UserId);

            var activeTokens = await _dbContext.RefreshTokens
                .Where(t => t.UserId == existingToken.UserId && t.RevokedAtUtc == null)
                .ToListAsync(cancellationToken);

            foreach (var active in activeTokens)
            {
                active.RevokedAtUtc = DateTimeOffset.UtcNow;
                active.RevokedReason = "Token family compromised (token reuse detected)";
            }

            _dbContext.AuditLogs.Add(new AuditLog
            {
                ActorUserId = existingToken.UserId,
                Action = "TokenCompromised",
                EntityType = "RefreshToken",
                EntityId = existingToken.Id.ToString(),
                IpAddress = ipAddress,
                CreatedAtUtc = DateTimeOffset.UtcNow
            });

            await _dbContext.SaveChangesAsync(cancellationToken);
            return Result<AuthResponse>.Failure("Session revoked due to security violation. Please log in again.", 401);
        }

        if (existingToken.ExpiresAtUtc <= DateTimeOffset.UtcNow)
        {
            return Result<AuthResponse>.Failure("Refresh token has expired. Please log in again.", 401);
        }

        var user = existingToken.User;
        if (!user.IsActive)
        {
            return Result<AuthResponse>.Failure("Account is deactivated.", 403);
        }

        // Revoke the old token
        existingToken.RevokedAtUtc = DateTimeOffset.UtcNow;
        existingToken.RevokedReason = "Rotated";

        // Generate and issue replacement refresh token
        var newRawRefreshToken = _tokenService.GenerateRefreshToken();
        var newTokenHash = _tokenService.HashToken(newRawRefreshToken);

        var newRefreshTokenEntity = new RefreshToken
        {
            UserId = user.Id,
            TokenHash = newTokenHash,
            ExpiresAtUtc = DateTimeOffset.UtcNow.AddDays(7),
            CreatedByIp = TryParseIp(ipAddress),
            CreatedAtUtc = DateTimeOffset.UtcNow
        };

        existingToken.ReplacedByTokenId = newRefreshTokenEntity.Id;

        _dbContext.RefreshTokens.Add(newRefreshTokenEntity);

        _dbContext.AuditLogs.Add(new AuditLog
        {
            ActorUserId = user.Id,
            Action = "TokenRefreshed",
            EntityType = "RefreshToken",
            EntityId = newRefreshTokenEntity.Id.ToString(),
            IpAddress = ipAddress,
            CreatedAtUtc = DateTimeOffset.UtcNow
        });

        await _dbContext.SaveChangesAsync(cancellationToken);

        var roles = user.UserRoles.Select(ur => ur.Role.Name).ToList();
        var newAccessToken = _tokenService.GenerateAccessToken(user, roles);

        var userDto = new UserDto(user.Id, user.Email, user.FirstName, user.LastName, roles, user.IsActive, user.IsEmailVerified, user.CreatedAtUtc);
        var response = new AuthResponse(
            newAccessToken,
            _tokenService.AccessTokenExpiryMinutes * 60,
            newRawRefreshToken,
            userDto
        );

        return Result<AuthResponse>.Success(response);
    }

    public async Task<Result<bool>> LogoutAsync(string refreshToken, CancellationToken cancellationToken = default)
    {
        var tokenHash = _tokenService.HashToken(refreshToken);

        var token = await _dbContext.RefreshTokens
            .FirstOrDefaultAsync(t => t.TokenHash == tokenHash, cancellationToken);

        if (token != null && token.RevokedAtUtc == null)
        {
            token.RevokedAtUtc = DateTimeOffset.UtcNow;
            token.RevokedReason = "User logged out";

            _dbContext.AuditLogs.Add(new AuditLog
            {
                ActorUserId = token.UserId,
                Action = "UserLoggedOut",
                EntityType = "RefreshToken",
                EntityId = token.Id.ToString(),
                CreatedAtUtc = DateTimeOffset.UtcNow
            });

            await _dbContext.SaveChangesAsync(cancellationToken);
        }

        // Logout is idempotent: always return true
        return Result<bool>.Success(true);
    }

    public async Task<Result<bool>> ForgotPasswordAsync(ForgotPasswordRequest request, CancellationToken cancellationToken = default)
    {
        var normalizedEmail = request.Email.Trim().ToLowerInvariant();

        var user = await _dbContext.Users
            .FirstOrDefaultAsync(u => u.Email == normalizedEmail, cancellationToken);

        // Always return success even if email is not found to prevent user enumeration
        if (user == null || !user.IsActive)
        {
            return Result<bool>.Success(true);
        }

        // Invalidate older unverified password reset challenges
        var pendingChallenges = await _dbContext.OtpChallenges
            .Where(c => c.UserId == user.Id && c.Purpose == "PasswordReset" && c.VerifiedAtUtc == null)
            .ToListAsync(cancellationToken);

        foreach (var c in pendingChallenges)
        {
            c.VerifiedAtUtc = DateTimeOffset.UtcNow; // mark completed/invalidated
        }

        var otpCode = _tokenService.GenerateOtpCode();
        var otpHash = _tokenService.HashOtp(otpCode);

        var challenge = new OtpChallenge
        {
            UserId = user.Id,
            Purpose = "PasswordReset",
            CodeHash = otpHash,
            ExpiresAtUtc = DateTimeOffset.UtcNow.AddMinutes(10),
            CreatedAtUtc = DateTimeOffset.UtcNow
        };

        _dbContext.OtpChallenges.Add(challenge);

        _dbContext.AuditLogs.Add(new AuditLog
        {
            ActorUserId = user.Id,
            Action = "PasswordResetRequested",
            EntityType = "User",
            EntityId = user.Id.ToString(),
            CreatedAtUtc = DateTimeOffset.UtcNow
        });

        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(">>> [PASSWORD RESET OTP] Email: {Email}, OTP Code: {OtpCode} (Expires in 10 minutes) <<<",
            normalizedEmail, otpCode);

        try
        {
            await _notificationClient.SendOtpEmailAsync(normalizedEmail, "PasswordReset", otpCode, challenge.ExpiresAtUtc, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send password reset OTP email to {Email}", normalizedEmail);
        }

        return Result<bool>.Success(true);
    }

    public async Task<Result<bool>> ResetPasswordAsync(ResetPasswordRequest request, CancellationToken cancellationToken = default)
    {
        var normalizedEmail = request.Email.Trim().ToLowerInvariant();

        var user = await _dbContext.Users
            .FirstOrDefaultAsync(u => u.Email == normalizedEmail, cancellationToken);

        if (user == null)
        {
            return Result<bool>.Failure("Invalid email or verification code.", 400);
        }

        var challenge = await _dbContext.OtpChallenges
            .Where(c => c.UserId == user.Id && c.Purpose == "PasswordReset" && c.VerifiedAtUtc == null)
            .OrderByDescending(c => c.CreatedAtUtc)
            .FirstOrDefaultAsync(cancellationToken);

        if (challenge == null || challenge.ExpiresAtUtc <= DateTimeOffset.UtcNow)
        {
            return Result<bool>.Failure("Reset code has expired or is invalid. Please request a new code.", 400);
        }

        if (challenge.AttemptCount >= 5)
        {
            return Result<bool>.Failure("Too many failed attempts. Please request a new password reset.", 400);
        }

        var submittedOtpHash = _tokenService.HashOtp(request.Otp.Trim());
        if (challenge.CodeHash != submittedOtpHash)
        {
            challenge.AttemptCount++;
            await _dbContext.SaveChangesAsync(cancellationToken);
            return Result<bool>.Failure("Invalid email or verification code.", 400);
        }

        // Verify and update password
        challenge.VerifiedAtUtc = DateTimeOffset.UtcNow;
        user.PasswordHash = _passwordHasher.HashPassword(request.NewPassword);
        user.FailedLoginAttempts = 0;
        user.LockedUntilUtc = null;
        user.UpdatedAtUtc = DateTimeOffset.UtcNow;

        // Revoke all active refresh tokens to force re-login on all devices
        var activeTokens = await _dbContext.RefreshTokens
            .Where(t => t.UserId == user.Id && t.RevokedAtUtc == null)
            .ToListAsync(cancellationToken);

        foreach (var t in activeTokens)
        {
            t.RevokedAtUtc = DateTimeOffset.UtcNow;
            t.RevokedReason = "Password reset";
        }

        _dbContext.AuditLogs.Add(new AuditLog
        {
            ActorUserId = user.Id,
            Action = "PasswordResetSuccess",
            EntityType = "User",
            EntityId = user.Id.ToString(),
            CreatedAtUtc = DateTimeOffset.UtcNow
        });

        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Password reset successfully completed for user {Email}.", normalizedEmail);
        return Result<bool>.Success(true);
    }

    public async Task<Result<UserDto>> GetCurrentUserAsync(Guid userId, CancellationToken cancellationToken = default)
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
        var dto = new UserDto(user.Id, user.Email, user.FirstName, user.LastName, roles, user.IsActive, user.IsEmailVerified, user.CreatedAtUtc);
        return Result<UserDto>.Success(dto);
    }

    private static IPAddress? TryParseIp(string? ipAddress)
    {
        if (string.IsNullOrWhiteSpace(ipAddress)) return null;
        return IPAddress.TryParse(ipAddress, out var ip) ? ip : null;
    }
}
