using System.Globalization;
using Microsoft.Extensions.Logging;
using UnifiWatch.Models;
using UnifiWatch.Services.Localization;

namespace UnifiWatch.Services.Notifications;

/// <summary>
/// Coordinates SMS notification delivery with localization support
/// Formats product information and messages for SMS (160 char limit)
/// Automatically shortens messages to fit SMS constraints
/// </summary>
public class SmsNotificationService
{
    private readonly ISmsProvider _smsProvider;
    private readonly IResourceLocalizer _resourceLocalizer;
    private readonly ILogger<SmsNotificationService> _logger;
    private readonly SmsNotificationSettings _settings;

    private enum NotificationKind
    {
        InStock,
        OutOfStock,
        Error
    }

    public SmsNotificationService(
        ISmsProvider smsProvider,
        IResourceLocalizer resourceLocalizer,
        ILogger<SmsNotificationService> logger,
        SmsNotificationSettings settings)
    {
        _smsProvider = smsProvider;
        _resourceLocalizer = resourceLocalizer;
        _logger = logger;
        _settings = settings;
    }

    /// <summary>
    /// Sends a product in-stock notification SMS
    /// Automatically shortens message if needed to fit 160 character limit
    /// </summary>
    public async Task<bool> SendProductInStockNotificationAsync(
        UnifiProduct product,
        string recipient,
        string? store = null,
        CultureInfo? culture = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            culture ??= CultureInfo.CurrentCulture;

            // Get localized message template
            var subject = _resourceLocalizer.Notification("Sms.ProductInStock.Subject", product.Name);
            var body = _resourceLocalizer.Notification("Sms.ProductInStock.Body",
                product.Name,
                product.SKU ?? "N/A",
                product.Price?.ToString("C", culture) ?? "N/A",
                store ?? "N/A");

            // Combine and shorten if necessary
            var fullMessage = $"{GetPrefix(NotificationKind.InStock, culture)}{subject}: {body}";
            var finalMessage = ShortenMessageIfNeeded(fullMessage);

            return await _smsProvider.SendAsync(recipient, finalMessage, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error sending in-stock SMS notification for {ProductName} to {Recipient}: {ErrorMessage}",
                product.Name,
                recipient,
                ex.Message);
            return false;
        }
    }

    /// <summary>
    /// Sends a product out-of-stock notification SMS
    /// Automatically shortens message if needed to fit 160 character limit
    /// </summary>
    public async Task<bool> SendProductOutOfStockNotificationAsync(
        UnifiProduct product,
        string recipient,
        string? store = null,
        CultureInfo? culture = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            culture ??= CultureInfo.CurrentCulture;

            // Get localized message template
            var subject = _resourceLocalizer.Notification("Sms.ProductOutOfStock.Subject", product.Name);
            var body = _resourceLocalizer.Notification("Sms.ProductOutOfStock.Body",
                product.Name,
                product.SKU ?? "N/A",
                store ?? "N/A");

            // Combine and shorten if necessary
            var fullMessage = $"{GetPrefix(NotificationKind.OutOfStock, culture)}{subject}: {body}";
            var finalMessage = ShortenMessageIfNeeded(fullMessage);

            return await _smsProvider.SendAsync(recipient, finalMessage, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error sending out-of-stock SMS notification for {ProductName} to {Recipient}: {ErrorMessage}",
                product.Name,
                recipient,
                ex.Message);
            return false;
        }
    }

    /// <summary>
    /// Sends an error notification SMS
    /// Automatically shortens message if needed to fit 160 character limit
    /// </summary>
    public async Task<bool> SendErrorNotificationAsync(
        string errorMessage,
        string recipient,
        CultureInfo? culture = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            culture ??= CultureInfo.CurrentCulture;

            // Get localized error message template
            var subject = _resourceLocalizer.Notification("Sms.Error.Subject");
            var body = _resourceLocalizer.Notification("Sms.Error.Body", errorMessage);

            // Combine and shorten if necessary
            var fullMessage = $"{GetPrefix(NotificationKind.Error, culture)}{subject}: {body}";
            var finalMessage = ShortenMessageIfNeeded(fullMessage);

            return await _smsProvider.SendAsync(recipient, finalMessage, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error sending error SMS notification to {Recipient}: {ErrorMessage}",
                recipient,
                ex.Message);
            return false;
        }
    }

    /// <summary>
    /// Sends SMS notification to multiple recipients
    /// </summary>
    public async Task<Dictionary<string, bool>> SendBatchNotificationAsync(
        IList<string> recipients,
        string subject,
        string body,
        CancellationToken cancellationToken = default)
    {
        var fullMessage = $"{subject}: {body}";
        var finalMessage = ShortenMessageIfNeeded(fullMessage);

        return await _smsProvider.SendBatchAsync(recipients, finalMessage, cancellationToken);
    }

    /// <summary>
    /// Shortens message to fit SMS character limit
    /// Strategy: Truncate intelligently to keep essential information
    /// </summary>
    private string ShortenMessageIfNeeded(string message)
    {
        if (!_settings.AllowMessageShortening)
            return message;

        if (message.Length <= _settings.MaxMessageLength)
            return message;

        // Reserve 3 chars for ellipsis
        var maxUsableLength = _settings.MaxMessageLength - 3;

        if (maxUsableLength <= 0)
        {
            _logger.LogWarning("Message cannot be shortened to fit SMS limit of {MaxLength}", _settings.MaxMessageLength);
            return message[.._settings.MaxMessageLength];
        }

        // Try to break at word boundary
        var shortened = message[..maxUsableLength];

        // Find last space before limit
        var lastSpace = shortened.LastIndexOf(' ');
        if (lastSpace > maxUsableLength / 2)
        {
            // If space found in second half, use it to avoid cutting word
            shortened = shortened[..lastSpace];
        }

        return shortened + "...";
    }

    private string GetPrefix(NotificationKind kind, CultureInfo culture)
    {
        var lang = culture.TwoLetterISOLanguageName.ToLowerInvariant();
        return kind switch
        {
            NotificationKind.InStock => lang switch
            {
                "fr" => "[EN STOCK] ",
                "de" => "[VERFÜGBAR] ",
                "es" => "[DISPONIBLE] ",
                "it" => "[DISPONIBILE] ",
                _ => "[IN STOCK] "
            },
            NotificationKind.OutOfStock => lang switch
            {
                "fr" => "[HORS STOCK] ",
                "de" => "[NICHT VERFÜGBAR] ",
                "es" => "[SIN STOCK] ",
                "it" => "[ESAURITO] ",
                _ => "[OUT OF STOCK] "
            },
            NotificationKind.Error => lang switch
            {
                "fr" => "[ERREUR] ",
                "de" => "[FEHLER] ",
                "es" => "[ERROR] ",
                "it" => "[ERRORE] ",
                _ => "[ERROR] "
            },
            _ => string.Empty
        };
    }
}
