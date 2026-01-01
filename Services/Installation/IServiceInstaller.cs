namespace UnifiWatch.Services.Installation;

/// <summary>
/// Interface for cross-platform service installation
/// </summary>
public interface IServiceInstaller
{
    /// <summary>
    /// Install the service
    /// </summary>
    Task<bool> InstallAsync(ServiceInstallOptions options, CancellationToken cancellationToken = default);

    /// <summary>
    /// Uninstall the service
    /// </summary>
    Task<bool> UninstallAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Start the service
    /// </summary>
    Task<bool> StartAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Stop the service
    /// </summary>
    Task<bool> StopAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Get current service status
    /// </summary>
    Task<ServiceStatus> GetStatusAsync(CancellationToken cancellationToken = default);
}
