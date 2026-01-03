using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using UnifiWatch.Models;

namespace UnifiWatch.Services.Notifications;

/// <summary>
/// Orchestrates notification fan-out across email and SMS with configurable de-duplication.
/// </summary>
public class NotificationOrchestrator
{
    private readonly EmailNotificationService _emailService;
    private readonly SmsNotificationService _smsService;
    private readonly IOptions<EmailNotificationSettings> _emailOptions;
    private readonly IOptions<SmsNotificationSettings> _smsOptions;
    private readonly ILogger<NotificationOrchestrator> _logger;
    private readonly ConcurrentDictionary<string, DateTimeOffset> _lastSent = new();
    private readonly TimeSpan _dedupeWindow;

    public NotificationOrchestrator(
        EmailNotificationService emailService,
        SmsNotificationService smsService,
        IOptions<EmailNotificationSettings> emailOptions,
        IOptions<SmsNotificationSettings> smsOptions,
        ILogger<NotificationOrchestrator> logger,
        TimeSpan? dedupeWindow = null)
    {
        _emailService = emailService;
        _smsService = smsService;
        _emailOptions = emailOptions;
        _smsOptions = smsOptions;
        _logger = logger;
        // Default to 5 minutes if not specified, validate range (1-60 minutes)
        _dedupeWindow = dedupeWindow ?? TimeSpan.FromMinutes(5);
        if (_dedupeWindow < TimeSpan.FromMinutes(1) || _dedupeWindow > TimeSpan.FromMinutes(60))
        {
            _logger.LogWarning("Dedupe window {Window} is outside valid range (1-60 minutes), defaulting to 5 minutes", _dedupeWindow);
            _dedupeWindow = TimeSpan.FromMinutes(5);
        }
    }

    public async Task NotifyInStockAsync(IEnumerable<UnifiProduct> products, string store, CancellationToken cancellationToken = default)
    {
        foreach (var product in products)
        {
            var key = product.SKU ?? product.Name;
            var emailEnabled = _emailOptions.Value.Enabled && _emailOptions.Value.Recipients.Count > 0;
            var smsEnabled = _smsOptions.Value.Enabled && _smsOptions.Value.Recipients.Count > 0;

            if (!emailEnabled && !smsEnabled)
            {
                continue; // nothing to send
            }

            if (emailEnabled && ShouldSend("email", key))
            {
                await SendEmailAsync(product, store, cancellationToken);
            }

            if (smsEnabled && ShouldSend("sms", key))
            {
                await SendSmsAsync(product, store, cancellationToken);
            }
        }
    }

    private bool ShouldSend(string channel, string key)
    {
        var compositeKey = $"{channel}:{key}";
        var now = DateTimeOffset.UtcNow;

        if (_lastSent.TryGetValue(compositeKey, out var last))
        {
            if (now - last < _dedupeWindow)
            {
                return false;
            }
        }

        _lastSent[compositeKey] = now;
        return true;
    }

    private async Task SendEmailAsync(UnifiProduct product, string store, CancellationToken cancellationToken)
    {
        var settings = _emailOptions.Value;
        if (!settings.Enabled || settings.Recipients.Count == 0)
        {
            return;
        }

        foreach (var recipient in settings.Recipients)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var success = await _emailService.SendProductInStockNotificationAsync(product, recipient, store, culture: null, cancellationToken);
            if (!success)
            {
                _logger.LogWarning("Email notification failed for {Product} -> {Recipient}", product.Name, recipient);
            }
        }
    }

    private async Task SendSmsAsync(UnifiProduct product, string store, CancellationToken cancellationToken)
    {
        var settings = _smsOptions.Value;
        if (!settings.Enabled || settings.Recipients.Count == 0)
        {
            return;
        }

        foreach (var recipient in settings.Recipients)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var success = await _smsService.SendProductInStockNotificationAsync(product, recipient, store: store, culture: null, cancellationToken);
            if (!success)
            {
                _logger.LogWarning("SMS notification failed for {Product} -> {Recipient}", product.Name, recipient);
            }
        }
    }
}
