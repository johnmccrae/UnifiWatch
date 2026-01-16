namespace UnifiWatch.Services.Notifications.Sms;

/// <summary>
/// Interface for SMS notification providers (Twilio, AWS SNS, Azure Communication Services, etc.)
/// All providers must implement send and test connection functionality
/// </summary>
public interface ISmsProvider
{
    /// <summary>
    /// Sends an SMS notification asynchronously
    /// </summary>
    /// <param name="message">The SMS message to send (plain text, max 160 characters per segment)</param>
    /// <param name="toPhoneNumbers">List of recipient phone numbers in E.164 format</param>
    /// <param name="cancellationToken">Cancellation token for async operations</param>
    /// <returns>True if successful, false otherwise</returns>
    Task<bool> SendAsync(string message, List<string> toPhoneNumbers, CancellationToken cancellationToken = default);

    /// <summary>
    /// Tests the connection to the SMS provider asynchronously
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for async operations</param>
    /// <returns>True if connection successful, false otherwise</returns>
    Task<bool> TestConnectionAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the friendly name of the SMS provider
    /// </summary>
    string ProviderName { get; }

    /// <summary>
    /// Indicates whether the provider is properly configured
    /// </summary>
    bool IsConfigured { get; }

    /// <summary>
    /// Gets the maximum message length for this provider (typically 160 for SMS, up to 1600 for MMS)
    /// </summary>
    int MaxMessageLength { get; }
}
