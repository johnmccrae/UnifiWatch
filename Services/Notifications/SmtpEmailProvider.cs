using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Logging;
using UnifiWatch.Models;
using UnifiWatch.Services.Configuration;
using UnifiWatch.Services.Credentials;

namespace UnifiWatch.Services.Notifications;

/// <summary>
/// SMTP-based email provider implementation
/// Supports TLS/SSL authentication with credential provider integration
/// </summary>
public class SmtpEmailProvider : IEmailProvider
{
    private readonly ILogger<SmtpEmailProvider> _logger;
    private readonly EmailNotificationSettings _emailSettings;
    private readonly ICredentialProvider _credentialProvider;

    public SmtpEmailProvider(
        ILogger<SmtpEmailProvider> logger,
        Microsoft.Extensions.Options.IOptions<EmailNotificationSettings> emailOptions,
        ICredentialProvider credentialProvider)
    {
        _logger = logger;
        _emailSettings = emailOptions.Value;
        _credentialProvider = credentialProvider;
    }

    public async Task<bool> SendAsync(
        string recipient,
        string subject,
        string plainBody,
        string? htmlBody = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (!IsValidEmailAddress(recipient))
            {
                _logger.LogWarning("Invalid email address format: {Recipient}", recipient);
                return false;
            }

            // Ensure TLS 1.2+ for secure connections
            System.Net.ServicePointManager.SecurityProtocol = 
                System.Net.SecurityProtocolType.Tls12 | 
                System.Net.SecurityProtocolType.Tls13;

            using var client = new SmtpClient(_emailSettings.SmtpServer, _emailSettings.SmtpPort)
            {
                EnableSsl = _emailSettings.UseTls,
                DeliveryMethod = SmtpDeliveryMethod.Network,
                Timeout = 10000
            };

            // Load credentials from credential provider
            var credentialSecret = await _credentialProvider.RetrieveAsync(_emailSettings.CredentialKey, cancellationToken);
            if (!string.IsNullOrEmpty(credentialSecret))
            {
                // Assume format is "username:password" from the credential provider
                var parts = credentialSecret.Split(':', 2);
                if (parts.Length == 2)
                {
                    client.Credentials = new NetworkCredential(parts[0], parts[1]);
                }
                else
                {
                    client.Credentials = new NetworkCredential("", credentialSecret);
                }
            }

            using var message = new MailMessage
            {
                From = new MailAddress(_emailSettings.FromAddress),
                Subject = subject,
                Body = plainBody,
                IsBodyHtml = !string.IsNullOrEmpty(htmlBody)
            };

            message.To.Add(recipient);

            if (!string.IsNullOrEmpty(htmlBody))
            {
                // Add alternative view for HTML
                var plainView = AlternateView.CreateAlternateViewFromString(plainBody, null, "text/plain");
                var htmlView = AlternateView.CreateAlternateViewFromString(htmlBody, null, "text/html");
                message.AlternateViews.Add(plainView);
                message.AlternateViews.Add(htmlView);
            }

            await client.SendMailAsync(message, cancellationToken);

            _logger.LogInformation("Email sent successfully to {Recipient}", recipient);
            return true;
        }
        catch (SmtpException ex)
        {
            _logger.LogError(ex, "SMTP error sending email to {Recipient}", recipient);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error sending email to {Recipient}", recipient);
            return false;
        }
    }

    public async Task<Dictionary<string, bool>> SendBatchAsync(
        List<string> recipients,
        string subject,
        string plainBody,
        string? htmlBody = null,
        CancellationToken cancellationToken = default)
    {
        var results = new Dictionary<string, bool>();

        foreach (var recipient in recipients)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var success = await SendAsync(recipient, subject, plainBody, htmlBody, cancellationToken);
            results[recipient] = success;
        }

        return results;
    }

    private bool IsValidEmailAddress(string email)
    {
        try
        {
            var addr = new MailAddress(email);
            return addr.Address == email;
        }
        catch
        {
            return false;
        }
    }
}
