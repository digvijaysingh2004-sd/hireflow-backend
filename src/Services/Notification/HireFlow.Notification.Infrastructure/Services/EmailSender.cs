using System.Net.Mail;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace HireFlow.Notification.Infrastructure.Services;

public interface IEmailSender
{
    Task<(bool Success, string? ErrorMessage)> SendEmailAsync(string toEmail, string subject, string body, CancellationToken cancellationToken = default);
}

public class SmtpEmailSender : IEmailSender
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<SmtpEmailSender> _logger;

    public SmtpEmailSender(IConfiguration configuration, ILogger<SmtpEmailSender> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<(bool Success, string? ErrorMessage)> SendEmailAsync(string toEmail, string subject, string body, CancellationToken cancellationToken = default)
    {
        var host = _configuration["Smtp:Host"] ?? "localhost";
        var port = int.TryParse(_configuration["Smtp:Port"], out var p) ? p : 587;
        var fromAddress = _configuration["Smtp:SenderEmail"] ?? _configuration["Smtp:FromAddress"] ?? _configuration["Smtp:Username"] ?? "no-reply@hireflow.local";
        var fromDisplayName = _configuration["Smtp:SenderName"] ?? _configuration["Smtp:FromName"] ?? "HireFlow Notifications";
        var username = _configuration["Smtp:Username"];
        var password = _configuration["Smtp:Password"];
        var enableSsl = _configuration.GetValue<bool?>("Smtp:EnableSsl") ?? (port == 587 || port == 465 || host.Contains("gmail.com"));

        try
        {
            using var client = new SmtpClient(host, port)
            {
                EnableSsl = enableSsl,
                Timeout = 15000
            };

            if (!string.IsNullOrWhiteSpace(username) && !string.IsNullOrWhiteSpace(password))
            {
                client.UseDefaultCredentials = false;
                client.Credentials = new System.Net.NetworkCredential(username.Trim(), password.Trim());
            }

            using var message = new MailMessage
            {
                From = new MailAddress(fromAddress, fromDisplayName),
                Subject = subject,
                Body = body,
                IsBodyHtml = true
            };
            message.To.Add(toEmail);

            await client.SendMailAsync(message, cancellationToken);
            _logger.LogInformation("Email successfully sent via SMTP ({Host}) to {ToEmail}", host, toEmail);
            return (true, null);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to send email to {ToEmail} via {Host}:{Port}: {Message}", toEmail, host, port, ex.Message);
            return (false, ex.Message);
        }
    }
}
