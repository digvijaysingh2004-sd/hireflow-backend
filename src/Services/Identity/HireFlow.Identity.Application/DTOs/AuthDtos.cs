namespace HireFlow.Identity.Application.DTOs;

public record RegisterRequest(
    string Email,
    string Password,
    string FirstName,
    string LastName,
    string? Role = "Candidate"
);

public record VerifyEmailRequest(
    string Email,
    string Otp
);

public record LoginRequest(
    string Email,
    string Password
);

public record RefreshTokenRequest(
    string RefreshToken
);

public record ForgotPasswordRequest(
    string Email
);

public record ResetPasswordRequest(
    string Email,
    string Otp,
    string NewPassword
);

public record UserDto(
    Guid Id,
    string Email,
    string FirstName,
    string LastName,
    IReadOnlyList<string> Roles,
    bool IsActive = true,
    bool IsEmailVerified = false,
    DateTimeOffset? CreatedAtUtc = null
);

public record AuthResponse(
    string AccessToken,
    int ExpiresIn,
    string RefreshToken,
    UserDto User
);
