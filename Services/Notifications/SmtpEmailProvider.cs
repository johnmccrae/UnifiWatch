using System.Net;
using System.Net.Mail;
using MailKit;
using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using UnifiWatch.Configuration;
using UnifiWatch.Models;
using UnifiWatch.Services.Credentials;

namespace UnifiWatch.Services.Notifications;

/// <summary>
/// SMTP email notification provider using MailKit
/// Supports standard SMTP servers (Gmail, Office 365, custom servers, etc.)
/// Includes retry logic with exponential backoff for transient errors
/// Uses localization for all user-facing messages
/// </summary>
public class SmtpEmailProvider : INotificationProvider
{
    private readonly EmailNotificationConfig _config;
    private readonly ICredentialProvider _credentialProvider;
    private readonly EmailTemplateBuilder _templateBuilder;
    private readonly IStringLocalizer _localizer;
    private readonly ILogger<SmtpEmailProvider> _logger;

    public SmtpEmailProvider(
        EmailNotificationConfig config,
        ICredentialProvider credentialProvider,
        IStringLocalizer localizer,
        ILogger<SmtpEmailProvider> logger)
    {
        _config = config ?? throw new ArgumentNullException(nameof(config));
        _credentialProvider = credentialProvider ?? throw new ArgumentNullException(nameof(credentialProvider));
        _localizer = localizer ?? throw new ArgumentNullException(nameof(localizer));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _templateBuilder = new EmailTemplateBuilder(localizer);
    }

    public string ProviderName => "SMTP Email";

    public bool IsConfigured =>
        !string.IsNullOrWhiteSpace(_config.SmtpServer) &&
        _config.SmtpPort > 0 &&
        !string.IsNullOrWhiteSpace(_config.FromAddress) &&
        !string.IsNullOrWhiteSpace(_config.CredentialKey);

    public async Task<bool> SendAsync(NotificationMessage message, CancellationToken cancellationToken = default)
    {
        if (!IsConfigured)
        {
            _logger.LogWarning(_localizer["SMTP provider not configured"]);
            return false;
        }

        if (message.Recipients.Count == 0)
        {
            _logger.LogWarning(_localizer["No recipients specified"]);
            return false;
        }

        var maxAttempts = 3;
        var delays = new[] { 1000, 2000, 4000 };

        for (int attempt = 0; attempt < maxAttempts; attempt++)
        {
            try
            {
                var credentials = await _credentialProvider.RetrieveAsync(_config.CredentialKey, cancellationToken);
                if (string.IsNullOrEmpty(credentials))
                {
                    _logger.LogError(_localizer["SMTP credentials not found"]);
                    return false;
                }

                using var client = new MailKit.Net.Smtp.SmtpClient();

                var securityOption = _config.UseSsl ? SecureSocketOptions.StartTls : SecureSocketOptions.None;
                await client.ConnectAsync(_config.SmtpServer, _config.SmtpPort, securityOption, cancellationToken);

                // Parse credentials (format: username:password)
                var credParts = credentials.Split(':');
                if (credParts.Length != 2)
                {
                    _logger.LogError(_localizer["Invalid SMTP credential format"]);
                    return false;
                }

                await client.AuthenticateAsync(credParts[0], credParts[1], cancellationToken);

                var mailMessage = new MimeMessage();
                mailMessage.From.Add(new MailboxAddress(_config.FromName ?? "UnifiWatch", _config.FromAddress));

                foreach (var recipient in message.Recipients)
                {
                    mailMessage.To.Add(new MailboxAddress("", recipient));
                }

                mailMessage.Subject = message.Subject;

                var bodyBuilder = new BodyBuilder
                {
                    TextBody = message.TextBody,
                    HtmlBody = message.HtmlBody ?? _templateBuilder.BuildStockAlertHtml(message.Products, message.Metadata)
                };

                mailMessage.Body = bodyBuilder.ToMessageBody();

                await client.SendAsync(mailMessage, cancellationToken);
                await client.DisconnectAsync(true, cancellationToken);

                _logger.LogInformation(_localizer["Email sent successfully to {0} recipients"], message.Recipients.Count);
                return true;
            }
            catch (Exception ex)
            {
                var isTransient = IsTransientError(ex);

                if (attempt < maxAttempts - 1 && isTransient)
                {
                    _logger.LogWarning(ex, _localizer["SMTP send failed, retrying in {0}ms"], delays[attempt]);
                    await Task.Delay(delays[attempt], cancellationToken);
                }
                else
                {
                    _logger.LogError(ex, _localizer["SMTP send failed after {0} attempts"], maxAttempts);
                    return false;
                }
            }
        }

        return false;
    }

    public async Task<bool> TestConnectionAsync(CancellationToken cancellationToken = default)
    {
        if (!IsConfigured)
        {
            _logger.LogWarning(_localizer["SMTP provider not configured"]);
            return false;
        }

        try
        {
            var credentials = await _credentialProvider.RetrieveAsync(_config.CredentialKey, cancellationToken);
            if (string.IsNullOrEmpty(credentials))
            {
                return false;
            }

            var credParts = credentials.Split(':');
            if (credParts.Length != 2)
            {
                _logger.LogError(_localizer["Invalid SMTP credential format"]);
                return false;
            }

            using var client = new MailKit.Net.Smtp.SmtpClient();
            var securityOption = _config.UseSsl ? SecureSocketOptions.StartTls : SecureSocketOptions.None;

            await client.ConnectAsync(_config.SmtpServer, _config.SmtpPort, securityOption, cancellationToken);
            await client.AuthenticateAsync(credParts[0], credParts[1], cancellationToken);
            await client.DisconnectAsync(true, cancellationToken);

            _logger.LogInformation(_localizer["SMTP connection test successful"]);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, _localizer["SMTP connection test failed"]);
            return false;
        }
    }

    private bool IsTransientError(Exception ex)
    {
        // SMTP transient error codes: 421, 450, 452 (temporary issues)
        if (ex is MailKit.Net.Smtp.SmtpCommandException smtpEx)
        {
            return smtpEx.StatusCode == MailKit.Net.Smtp.SmtpStatusCode.ServiceNotAvailable ||
                   (int)smtpEx.StatusCode == 421 ||  // Service not available
                   (int)smtpEx.StatusCode == 450 ||  // Requested mail action not taken
                   (int)smtpEx.StatusCode == 452;    // Insufficient storage
        }

        // Network timeouts and connection resets are transient
        return ex is TimeoutException || ex is OperationCanceledException;
    }
}
