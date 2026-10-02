using System.Net;

namespace HireFlow.Identity.Domain.Entities;

public class RefreshToken
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid UserId { get; set; }
    public User User { get; set; } = null!;
    public string TokenHash { get; set; } = string.Empty;
    public DateTimeOffset ExpiresAtUtc { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? RevokedAtUtc { get; set; }
    public Guid? ReplacedByTokenId { get; set; }
    public IPAddress? CreatedByIp { get; set; }
    public string? RevokedReason { get; set; }

    public bool IsActive => RevokedAtUtc == null && DateTimeOffset.UtcNow < ExpiresAtUtc;
}
