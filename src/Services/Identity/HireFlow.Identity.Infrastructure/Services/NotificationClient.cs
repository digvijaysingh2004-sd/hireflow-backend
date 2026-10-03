using System.Net.Http.Json;
using System.Net.Mail;
using System.Text.Json;
using HireFlow.Identity.Application.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace HireFlow.Identity.Infrastructure.Services;

public class NotificationClient : INotificationClient
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;
    private readonly ILogger<NotificationClient> _logger;

    public NotificationClient(
        HttpClient httpClient,
        IConfiguration configuration,
        ILogger<NotificationClient> logger)
    {
        _httpClient = httpClient;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task SendOtpEmailAsync(
        string email,
        string purpose,
        string otp,
        DateTimeOffset expiresAtUtc,
        CancellationToken cancellationToken = default)
    {
        var notificationBaseUrl = _configuration["Notification:BaseUrl"]
            ?? _configuration["NOTIFICATION_API_BASE_URL"]
            ?? "http://localhost:5289";

        bool notificationApiSucceeded = false;

        try
        {
            var payload = new
            {
                recipientEmail = email,
                purpose = purpose,
                otp = otp,
                expiresAtUtc = expiresAtUtc
            };

            var endpoint = $"{notificationBaseUrl.TrimEnd('/')}/api/v1/internal/notifications/otp";
            _logger.LogInformation("Sending OTP notification to Notification Service at {Endpoint} for {Email}", endpoint, email);

            using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            cts.CancelAfter(TimeSpan.FromSeconds(5)); // fast timeout to allow SMTP fallback if Notification API is not running

            var response = await _httpClient.PostAsJsonAsync(endpoint, payload, cts.Token);
            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation("OTP notification successfully queued via Notification Service for {Email}", email);
                notificationApiSucceeded = true;
                return;
            }
            else
            {
                var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogWarning("Notification service returned status {StatusCode}: {Error}. Falling back to direct SMTP.",
                    response.StatusCode, errorContent);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Could not reach Notification service at {BaseUrl} ({Message}). Falling back to direct SMTP.",
                notificationBaseUrl, ex.Message);
        }

        // Fallback: Send directly via SMTP if configured
        if (!notificationApiSucceeded)
        {
            await SendDirectSmtpAsync(email, purpose, otp, expiresAtUtc, cancellationToken);
        }
    }

    private async Task SendDirectSmtpAsync(
        string email,
        string purpose,
        string otp,
        DateTimeOffset expiresAtUtc,
        CancellationToken cancellationToken)
    {
        var host = _configuration["Smtp:Host"] ?? "smtp.gmail.com";
        var port = int.TryParse(_configuration["Smtp:Port"], out var p) ? p : 587;
        var username = _configuration["Smtp:Username"];
        var password = _configuration["Smtp:Password"];
        var fromAddress = _configuration["Smtp:SenderEmail"] ?? _configuration["Smtp:Username"] ?? "no-reply@hireflow.local";
        var fromDisplayName = _configuration["Smtp:SenderName"] ?? "HireFlow Verification";
        var enableSsl = _configuration.GetValue<bool?>("Smtp:EnableSsl") ?? true;

        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
        {
            _logger.LogWarning("Direct SMTP fallback skipped: Smtp:Username or Smtp:Password is not configured.");
            return;
        }

        try
        {
            using var client = new SmtpClient(host, port)
            {
                EnableSsl = enableSsl,
                Timeout = 15000,
                UseDefaultCredentials = false,
                Credentials = new System.Net.NetworkCredential(username.Trim(), password.Trim())
            };

            var subject = $"Your HireFlow Verification Code: {otp}";
            var body = $@"<html>
<body style='font-family: Arial, sans-serif; line-height: 1.6; color: #333;'>
  <div style='max-width: 600px; margin: 0 auto; padding: 20px; border: 1px solid #e2e8f0; border-radius: 8px;'>
    <h2 style='color: #2563eb;'>HireFlow Verification</h2>
    <p>Purpose: <strong>{purpose}</strong></p>
    <p>Your one-time verification code is:</p>
    <div style='font-size: 32px; font-weight: bold; letter-spacing: 4px; color: #1d4ed8; padding: 12px; background: #eff6ff; border-radius: 6px; text-align: center; margin: 16px 0;'>
      {otp}
    </div>
    <p>This code will expire at: <strong>{expiresAtUtc:yyyy-MM-dd HH:mm:ss} UTC</strong>.</p>
    <p style='color: #64748b; font-size: 13px;'>If you did not request this code, please ignore this email.</p>
  </div>
</body>
</html>";

            using var message = new MailMessage
            {
                From = new MailAddress(fromAddress, fromDisplayName),
                Subject = subject,
                Body = body,
                IsBodyHtml = true
            };
            message.To.Add(email);

            await client.SendMailAsync(message, cancellationToken);
            _logger.LogInformation("Direct SMTP email successfully delivered to {Email} via {Host}:{Port}", email, host, port);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send direct SMTP email to {Email} via {Host}:{Port}: {Message}", email, host, port, ex.Message);
        }
    }
}
