namespace HireFlow.Identity.Application.Interfaces;

public interface INotificationClient
{
    Task SendOtpEmailAsync(
        string email,
        string purpose,
        string otp,
        DateTimeOffset expiresAtUtc,
        CancellationToken cancellationToken = default);
}
