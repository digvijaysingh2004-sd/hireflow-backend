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
        var port = int.TryParse(_configuration["Smtp:Port"], out var p) ? p : 1025;
        var fromAddress = _configuration["Smtp:FromAddress"] ?? "no-reply@hireflow.local";
        var fromDisplayName = _configuration["Smtp:FromName"] ?? "HireFlow Notification";

        try
        {
            using var client = new SmtpClient(host, port)
            {
                EnableSsl = false,
                Timeout = 10000
            };

            using var message = new MailMessage
            {
                From = new MailAddress(fromAddress, fromDisplayName),
                Subject = subject,
                Body = body,
                IsBodyHtml = true
            };
            message.To.Add(toEmail);

            await client.SendMailAsync(message, cancellationToken);
            _logger.LogInformation("Email successfully sent via SMTP to {ToEmail}", toEmail);
            return (true, null);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to send email to {ToEmail} via {Host}:{Port}: {Message}", toEmail, host, port, ex.Message);
            return (false, ex.Message);
        }
    }
}
