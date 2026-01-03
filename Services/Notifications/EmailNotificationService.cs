using System.Globalization;
using Microsoft.Extensions.Logging;
using UnifiWatch.Models;
using UnifiWatch.Services.Localization;

namespace UnifiWatch.Services.Notifications;

/// <summary>
/// Coordinates email notification delivery with localization support
/// Formats product information and messages in the user's preferred language
/// </summary>
public class EmailNotificationService
{
    private readonly IEmailProvider _emailProvider;
    private readonly IResourceLocalizer _resourceLocalizer;
    private readonly ILogger<EmailNotificationService> _logger;

    public EmailNotificationService(
        IEmailProvider emailProvider,
        IResourceLocalizer resourceLocalizer,
        ILogger<EmailNotificationService> logger)
    {
        _emailProvider = emailProvider;
        _resourceLocalizer = resourceLocalizer;
        _logger = logger;
    }

    /// <summary>
    /// Sends a product in-stock notification email
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

            var subject = _resourceLocalizer.Notification("Email.ProductInStock.Subject", product.Name);
            var body = _resourceLocalizer.Notification("Email.ProductInStock.Body",
                product.Name,
                product.SKU ?? "N/A",
                product.Price?.ToString("C", culture) ?? "N/A",
                store ?? "N/A",
                DateTime.Now.ToString("g", culture));

            return await _emailProvider.SendAsync(
                recipient,
                subject,
                body,
                null,
                cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending in-stock notification for product {ProductName} to {Recipient}", product.Name, recipient);
            return false;
        }
    }

    /// <summary>
    /// Sends a product out-of-stock notification email
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

            var subject = _resourceLocalizer.Notification("Email.ProductOutOfStock.Subject", product.Name);
            var body = _resourceLocalizer.Notification("Email.ProductOutOfStock.Body",
                product.Name,
                product.SKU ?? "N/A",
                store ?? "N/A",
                DateTime.Now.ToString("g", culture));

            return await _emailProvider.SendAsync(
                recipient,
                subject,
                body,
                null,
                cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending out-of-stock notification for product {ProductName} to {Recipient}", product.Name, recipient);
            return false;
        }
    }

    /// <summary>
    /// Sends an error notification email
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

            var subject = _resourceLocalizer.Notification("Email.Error.Subject");
            var body = _resourceLocalizer.Notification("Email.Error.Body",
                errorMessage,
                DateTime.Now.ToString("g", culture));

            return await _emailProvider.SendAsync(
                recipient,
                subject,
                body,
                null,
                cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending error notification to {Recipient}", recipient);
            return false;
        }
    }

    /// <summary>
    /// Sends batch notifications to multiple recipients
    /// </summary>
    public async Task<Dictionary<string, bool>> SendBatchNotificationAsync(
        List<string> recipients,
        string subject,
        string body,
        CultureInfo? culture = null,
        CancellationToken cancellationToken = default)
    {
        culture ??= CultureInfo.CurrentCulture;

        return await _emailProvider.SendBatchAsync(
            recipients,
            subject,
            body,
            null,
            cancellationToken);
    }
}
