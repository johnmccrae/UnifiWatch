namespace UnifiWatch.Services.Notifications;

/// <summary>
/// Interface for notification providers (email, SMS, desktop notifications, etc.)
/// All providers must implement send and test connection functionality
/// </summary>
public interface INotificationProvider
{
    /// <summary>
    /// Sends a notification message asynchronously
    /// </summary>
    /// <param name="message">The notification message to send</param>
    /// <param name="cancellationToken">Cancellation token for async operations</param>
    /// <returns>True if successful, false otherwise</returns>
    Task<bool> SendAsync(NotificationMessage message, CancellationToken cancellationToken = default);

    /// <summary>
    /// Tests the connection to the notification provider asynchronously
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for async operations</param>
    /// <returns>True if connection successful, false otherwise</returns>
    Task<bool> TestConnectionAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the friendly name of the notification provider
    /// </summary>
    string ProviderName { get; }

    /// <summary>
    /// Indicates whether the provider is properly configured
    /// </summary>
    bool IsConfigured { get; }
}
