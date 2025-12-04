using UnifiStockTracker.Models;

namespace UnifiStockTracker.Services.Configuration;

/// <summary>
/// Platform-agnostic configuration provider for service mode settings
/// Handles loading, saving, and validating configuration files
/// </summary>
public interface IConfigurationProvider
{
    /// <summary>
    /// Loads the configuration from disk
    /// Creates default configuration if file doesn't exist
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The loaded or default configuration</returns>
    Task<ServiceConfiguration> LoadAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Saves the configuration to disk
    /// </summary>
    /// <param name="config">Configuration to save</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if successfully saved</returns>
    Task<bool> SaveAsync(ServiceConfiguration config, CancellationToken cancellationToken = default);

    /// <summary>
    /// Validates the configuration for completeness and correctness
    /// </summary>
    /// <param name="config">Configuration to validate</param>
    /// <returns>List of validation errors (empty if valid)</returns>
    List<string> Validate(ServiceConfiguration config);

    /// <summary>
    /// Gets the full path to the configuration file
    /// </summary>
    string ConfigurationPath { get; }

    /// <summary>
    /// Gets the directory containing the configuration file
    /// </summary>
    string ConfigurationDirectory { get; }

    /// <summary>
    /// Gets a default/template configuration
    /// </summary>
    /// <returns>A new default configuration instance</returns>
    ServiceConfiguration GetDefaultConfiguration();

    /// <summary>
    /// Resets configuration to defaults and saves
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The reset configuration</returns>
    Task<ServiceConfiguration> ResetAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Watches for changes to the configuration file
    /// </summary>
    /// <param name="onConfigurationChanged">Callback invoked when configuration changes</param>
    /// <returns>A disposable that stops watching when disposed</returns>
    IDisposable WatchForChanges(Func<ServiceConfiguration, Task> onConfigurationChanged);
}
