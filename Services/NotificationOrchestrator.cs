using Microsoft.Extensions.Logging;
using UnifiWatch.Configuration;
using UnifiWatch.Services.Notifications;
using UnifiWatch.Services.Notifications.Sms;
using ConfigServiceConfiguration = UnifiWatch.Configuration.ServiceConfiguration;

namespace UnifiWatch.Services;

/// <summary>
/// Coordinates notification delivery across multiple channels (desktop, email, SMS)
/// Handles channel orchestration, error handling, and delivery reporting
/// Sends notifications based on configured enabled channels
/// </summary>
public class NotificationOrchestrator
{
    private readonly INotificationProvider? _smtpEmailProvider;
    private readonly INotificationProvider? _graphEmailProvider;
    private readonly ISmsProvider? _twilioSmsProvider;
    private readonly ConfigServiceConfiguration _config;
    private readonly ILogger<NotificationOrchestrator> _logger;

    public NotificationOrchestrator(
        ConfigServiceConfiguration config,
        INotificationProvider? smtpEmailProvider,
        INotificationProvider? graphEmailProvider,
        ISmsProvider? twilioSmsProvider,
        ILogger<NotificationOrchestrator> logger)
    {
        _config = config ?? throw new ArgumentNullException(nameof(config));
        _smtpEmailProvider = smtpEmailProvider;
        _graphEmailProvider = graphEmailProvider;
        _twilioSmsProvider = twilioSmsProvider;
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Sends notifications across all configured and enabled channels
    /// </summary>
    /// <param name="notification">The notification message containing subject, body, products, recipients</param>
    /// <param name="cancellationToken">Cancellation token for async operations</param>
    /// <returns>Notification result with per-channel delivery status</returns>
    public virtual async Task<NotificationResult> SendAsync(NotificationMessage notification, CancellationToken cancellationToken = default)
    {
        if (notification == null)
            throw new ArgumentNullException(nameof(notification));

        var result = new NotificationResult();

        // Send to desktop (always attempt, never fails externally)
        if (_config.Notifications.Desktop.Enabled)
        {
            try
            {
                // Desktop notifications are local system notifications, shown in real-time
                SendDesktopNotification(notification);
                result.DesktopSuccess = true;
                _logger.LogInformation("Desktop notification sent");
            }
            catch (Exception ex)
            {
                result.DesktopSuccess = false;
                _logger.LogWarning(ex, "Desktop notification failed");
            }
        }

        // Send to email (if configured)
        if (_config.Notifications.Email.Enabled)
        {
            result.EmailSuccess = await SendEmailAsync(notification, cancellationToken);
        }

        // Send to SMS (if configured)
        if (_config.Notifications.Sms.Enabled)
        {
            result.SmsSuccess = await SendSmsAsync(notification, cancellationToken);
        }

        // Determine overall success
        result.Success = DetermineOverallSuccess(result);

        if (result.Success)
        {
            _logger.LogInformation("Notifications sent successfully across enabled channels");
        }
        else
        {
            _logger.LogError("Notifications failed on one or more channels");
        }

        return result;
    }

    /// <summary>
    /// Tests all configured notification channels for connectivity
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Test results for each channel</returns>
    public async Task<NotificationChannelTestResults> TestAllChannelsAsync(CancellationToken cancellationToken = default)
    {
        var results = new NotificationChannelTestResults();

        if (_config.Notifications.Desktop.Enabled)
        {
            results.DesktopAvailable = true;
        }

        if (_config.Notifications.Email.Enabled)
        {
            results.EmailTestResult = await TestEmailAsync(cancellationToken);
        }

        if (_config.Notifications.Sms.Enabled)
        {
            results.SmsTestResult = await TestSmsAsync(cancellationToken);
        }

        return results;
    }

    private async Task<bool> SendEmailAsync(NotificationMessage notification, CancellationToken cancellationToken)
    {
        var provider = _config.Notifications.Email.Provider?.ToLowerInvariant() ?? "smtp";

        INotificationProvider? emailProvider = provider == "msgraph" ? _graphEmailProvider : _smtpEmailProvider;

        if (emailProvider == null || !emailProvider.IsConfigured)
        {
            _logger.LogWarning("Email provider ({0}) not configured", provider);
            return false;
        }

        try
        {
            var success = await emailProvider.SendAsync(notification, cancellationToken);
            if (success)
            {
                _logger.LogInformation("Email notification sent successfully via {0}", emailProvider.ProviderName);
            }
            else
            {
                _logger.LogWarning("Email notification failed with {0}", emailProvider.ProviderName);
            }
            return success;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Email notification error with {0}", emailProvider.ProviderName);
            return false;
        }
    }

    private async Task<bool> SendSmsAsync(NotificationMessage notification, CancellationToken cancellationToken)
    {
        if (_twilioSmsProvider == null || !_twilioSmsProvider.IsConfigured)
        {
            _logger.LogWarning("Twilio SMS provider not configured");
            return false;
        }

        try
        {
            // Convert notification message to SMS format (short, simple text)
            var smsMessage = FormatNotificationForSms(notification);

            var success = await _twilioSmsProvider.SendAsync(
                smsMessage,
                _config.Notifications.Sms.ToPhoneNumbers,
                cancellationToken);

            if (success)
            {
                _logger.LogInformation("SMS notification sent successfully");
            }
            else
            {
                _logger.LogWarning("SMS notification failed");
            }
            return success;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "SMS notification error");
            return false;
        }
    }

    private async Task<string> TestEmailAsync(CancellationToken cancellationToken)
    {
        var provider = _config.Notifications.Email.Provider?.ToLowerInvariant() ?? "smtp";
        INotificationProvider? emailProvider = provider == "msgraph" ? _graphEmailProvider : _smtpEmailProvider;

        if (emailProvider == null)
            return "Provider not configured";

        try
        {
            var success = await emailProvider.TestConnectionAsync(cancellationToken);
            return success ? "Connected" : "Failed";
        }
        catch (Exception ex)
        {
            return $"Error: {ex.Message}";
        }
    }

    private async Task<string> TestSmsAsync(CancellationToken cancellationToken)
    {
        if (_twilioSmsProvider == null)
            return "Provider not configured";

        try
        {
            var success = await _twilioSmsProvider.TestConnectionAsync(cancellationToken);
            return success ? "Connected" : "Failed";
        }
        catch (Exception ex)
        {
            return $"Error: {ex.Message}";
        }
    }

    private void SendDesktopNotification(NotificationMessage notification)
    {
        NotificationService.ShowNotification(
            notification.Subject,
            notification.TextBody);
    }

    private string FormatNotificationForSms(NotificationMessage notification)
    {
        // SMS format: concise, plain text, under 160 characters
        var message = notification.Subject;

        if (notification.Products.Count > 0)
        {
            var productNames = string.Join(", ", notification.Products.Take(2).Select(p => p.Name));
            message += $": {productNames}";

            if (notification.Products.Count > 2)
                message += $" +{notification.Products.Count - 2} more";
        }

        // Ensure message fits in SMS limit with smart shortening
        if (message.Length > 160)
        {
            message = SmsMessageFormatter.ShortenToLimit(message, 160);
        }

        return message;
    }

    private bool DetermineOverallSuccess(NotificationResult result)
    {
        // Success if at least one channel succeeded, or if no channels are enabled
        var enabledChannels = new[]
        {
            _config.Notifications.Desktop.Enabled,
            _config.Notifications.Email.Enabled,
            _config.Notifications.Sms.Enabled
        };

        if (!enabledChannels.Any(x => x))
            return true; // No channels enabled, consider it successful (no-op)

        return (result.DesktopSuccess && _config.Notifications.Desktop.Enabled) ||
               (result.EmailSuccess && _config.Notifications.Email.Enabled) ||
               (result.SmsSuccess && _config.Notifications.Sms.Enabled);
    }
}

/// <summary>
/// Result of notification delivery attempt
/// </summary>
public class NotificationResult
{
    /// <summary>
    /// Overall success status (true if at least one enabled channel succeeded)
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Desktop notification result
    /// </summary>
    public bool DesktopSuccess { get; set; }

    /// <summary>
    /// Email notification result
    /// </summary>
    public bool EmailSuccess { get; set; }

    /// <summary>
    /// SMS notification result
    /// </summary>
    public bool SmsSuccess { get; set; }

    /// <summary>
    /// Timestamp of delivery attempt
    /// </summary>
    public DateTime AttemptedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Test results for all notification channels
/// </summary>
public class NotificationChannelTestResults
{
    public bool DesktopAvailable { get; set; }
    public string EmailTestResult { get; set; } = "Not configured";
    public string SmsTestResult { get; set; } = "Not configured";

    public override string ToString()
    {
        return $"Desktop: {(DesktopAvailable ? "Available" : "Unavailable")}, Email: {EmailTestResult}, SMS: {SmsTestResult}";
    }
}
