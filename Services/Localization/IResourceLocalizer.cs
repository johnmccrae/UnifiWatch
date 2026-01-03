namespace UnifiWatch.Services.Localization;

/// <summary>
/// Interface for resource localization with support for notifications, CLI, and error messages.
/// </summary>
public interface IResourceLocalizer
{
    /// <summary>
    /// Gets a localized notification message with optional formatting arguments.
    /// </summary>
    /// <param name="key">The resource key for the notification message.</param>
    /// <param name="args">Optional formatting arguments.</param>
    /// <returns>The localized message, or the key if not found.</returns>
    string Notification(string key, params object[] args);

    /// <summary>
    /// Gets a localized CLI message with optional formatting arguments.
    /// </summary>
    /// <param name="key">The resource key for the CLI message.</param>
    /// <param name="args">Optional formatting arguments.</param>
    /// <returns>The localized message, or the key if not found.</returns>
    string CLI(string key, params object[] args);

    /// <summary>
    /// Gets a localized error message with optional formatting arguments.
    /// </summary>
    /// <param name="key">The resource key for the error message.</param>
    /// <param name="args">Optional formatting arguments.</param>
    /// <returns>The localized message, or the key if not found.</returns>
    string Error(string key, params object[] args);
}
