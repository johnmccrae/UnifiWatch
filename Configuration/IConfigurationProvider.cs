namespace UnifiStockTracker.Configuration;

/// <summary>
/// Platform-agnostic configuration provider interface
/// Handles loading, saving, and validating application configuration
/// </summary>
public interface IConfigurationProvider
{
    /// <summary>
    /// Loads configuration from disk
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Loaded configuration or null if file not found</returns>
    Task<ServiceConfiguration?> LoadAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Saves configuration to disk
    /// </summary>
    /// <param name="config">Configuration to save</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if successful</returns>
    Task<bool> SaveAsync(ServiceConfiguration config, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the full path to the configuration file
    /// </summary>
    string ConfigurationFilePath { get; }

    /// <summary>
    /// Gets the configuration directory path
    /// </summary>
    string ConfigurationDirectory { get; }

    /// <summary>
    /// Validates a configuration object
    /// </summary>
    /// <param name="config">Configuration to validate</param>
    /// <returns>List of validation errors (empty if valid)</returns>
    List<string> Validate(ServiceConfiguration config);

    /// <summary>
    /// Creates a default/sample configuration
    /// </summary>
    /// <returns>Default configuration with reasonable values</returns>
    ServiceConfiguration GetDefaultConfiguration();

    /// <summary>
    /// Checks if configuration file exists
    /// </summary>
    /// <returns>True if configuration file exists</returns>
    bool ConfigurationExists();

    /// <summary>
    /// Deletes the configuration file
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if successful or file didn't exist</returns>
    Task<bool> DeleteAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates backup of current configuration
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Path to backup file or null if failed</returns>
    Task<string?> BackupAsync(CancellationToken cancellationToken = default);
}
