namespace UnifiWatch.Services.Notifications;

/// <summary>
/// Interface for SMS notification providers
/// Supports multiple backends: Twilio, AWS SNS, Vonage, etc.
/// </summary>
public interface ISmsProvider
{
    /// <summary>
    /// Sends an SMS message to a single recipient
    /// </summary>
    /// <param name="recipient">Phone number in E.164 format (e.g., +12125552368)</param>
    /// <param name="message">Message text (max 160 characters)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if message was sent successfully</returns>
    Task<bool> SendAsync(string recipient, string message, CancellationToken cancellationToken = default);

    /// <summary>
    /// Sends an SMS message to multiple recipients
    /// </summary>
    /// <param name="recipients">Phone numbers in E.164 format</param>
    /// <param name="message">Message text (max 160 characters)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Dictionary mapping recipient to send result (true = success, false = failure)</returns>
    Task<Dictionary<string, bool>> SendBatchAsync(IList<string> recipients, string message, CancellationToken cancellationToken = default);
}
