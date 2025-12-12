namespace UnifiWatch.Services.Credentials;

/// <summary>
/// Platform-agnostic credential storage interface
/// Implementations handle Windows (CredMan), macOS (Keychain), Linux (secret-service, etc)
/// </summary>
public interface ICredentialProvider
{
    /// <summary>
    /// Stores a credential securely in the appropriate OS credential storage
    /// </summary>
    /// <param name="key">Unique identifier for the credential (e.g., "email-smtp", "twilio-api")</param>
    /// <param name="secret">The secret value to store (password, API key, etc)</param>
    /// <param name="label">Human-readable label for the credential (shown in credential manager)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if successfully stored, false otherwise</returns>
    Task<bool> StoreAsync(string key, string secret, string label = "", CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves a credential from secure storage
    /// </summary>
    /// <param name="key">Unique identifier for the credential</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The credential value, or null if not found</returns>
    Task<string?> RetrieveAsync(string key, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a credential from secure storage
    /// </summary>
    /// <param name="key">Unique identifier for the credential</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if successfully deleted, false if not found</returns>
    Task<bool> DeleteAsync(string key, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a credential exists in storage
    /// </summary>
    /// <param name="key">Unique identifier for the credential</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if credential exists</returns>
    Task<bool> ExistsAsync(string key, CancellationToken cancellationToken = default);

    /// <summary>
    /// Lists all credentials managed by this application
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of credential keys</returns>
    Task<List<string>> ListAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a human-readable description of the storage method being used
    /// </summary>
    string StorageMethodDescription { get; }

    /// <summary>
    /// Gets the underlying storage method (e.g., "windows-credential-manager", "macos-keychain")
    /// </summary>
    string StorageMethod { get; }
}
