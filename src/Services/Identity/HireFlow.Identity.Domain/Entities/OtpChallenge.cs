namespace HireFlow.Identity.Domain.Entities;

public class OtpChallenge
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid UserId { get; set; }
    public User User { get; set; } = null!;
    public string Purpose { get; set; } = string.Empty; // Registration, PasswordReset, LoginStepUp
    public string CodeHash { get; set; } = string.Empty;
    public DateTimeOffset ExpiresAtUtc { get; set; }
    public DateTimeOffset? VerifiedAtUtc { get; set; }
    public int AttemptCount { get; set; } = 0;
    public DateTimeOffset CreatedAtUtc { get; set; } = DateTimeOffset.UtcNow;
}
