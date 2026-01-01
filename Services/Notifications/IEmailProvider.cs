namespace UnifiWatch.Services.Notifications;

/// <summary>
/// Interface for email notification providers
/// </summary>
public interface IEmailProvider
{
    /// <summary>
    /// Sends an email notification
    /// </summary>
    /// <param name="recipient">Email address of the recipient</param>
    /// <param name="subject">Email subject line</param>
    /// <param name="plainBody">Plain text email body</param>
    /// <param name="htmlBody">HTML email body (optional)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if successful, false otherwise</returns>
    Task<bool> SendAsync(string recipient, string subject, string plainBody, string? htmlBody = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Sends an email to multiple recipients
    /// </summary>
    /// <param name="recipients">List of email addresses</param>
    /// <param name="subject">Email subject line</param>
    /// <param name="plainBody">Plain text email body</param>
    /// <param name="htmlBody">HTML email body (optional)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Dictionary mapping email to success status</returns>
    Task<Dictionary<string, bool>> SendBatchAsync(List<string> recipients, string subject, string plainBody, string? htmlBody = null, CancellationToken cancellationToken = default);
}
