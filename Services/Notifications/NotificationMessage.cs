using UnifiWatch.Models;

namespace UnifiWatch.Services.Notifications;

/// <summary>
/// Represents a notification message to be sent via one or more channels
/// Supports email, SMS, desktop notifications with optional metadata
/// </summary>
public class NotificationMessage
{
    /// <summary>
    /// Email subject or primary notification title
    /// </summary>
    public string Subject { get; set; } = string.Empty;

    /// <summary>
    /// Plain text version of the notification body
    /// </summary>
    public string TextBody { get; set; } = string.Empty;

    /// <summary>
    /// HTML version of the notification body (optional, for email)
    /// </summary>
    public string? HtmlBody { get; set; }

    /// <summary>
    /// List of UniFi products referenced in this notification
    /// </summary>
    public List<UnifiProduct> Products { get; set; } = new();

    /// <summary>
    /// List of recipient email addresses or phone numbers
    /// </summary>
    public List<string> Recipients { get; set; } = new();

    /// <summary>
    /// Additional metadata (store name, check timestamp, etc.)
    /// </summary>
    public Dictionary<string, string> Metadata { get; set; } = new();

    /// <summary>
    /// Priority level of this notification
    /// </summary>
    public NotificationPriority Priority { get; set; } = NotificationPriority.Normal;

    /// <summary>
    /// Timestamp when this notification was created
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Enumeration for notification priority levels
/// </summary>
public enum NotificationPriority
{
    Low,
    Normal,
    High,
    Critical
}
