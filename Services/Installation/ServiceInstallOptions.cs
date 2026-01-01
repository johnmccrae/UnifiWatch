namespace UnifiWatch.Services.Installation;

/// <summary>
/// Options for service installation
/// </summary>
public class ServiceInstallOptions
{
    /// <summary>
    /// Service name (used in registry/systemd, e.g., "UnifiWatch")
    /// </summary>
    public string ServiceName { get; set; } = "UnifiWatch";

    /// <summary>
    /// Display name for UI (e.g., "UnifiWatch Stock Monitor")
    /// </summary>
    public string DisplayName { get; set; } = "UnifiWatch Stock Monitor";

    /// <summary>
    /// Service description
    /// </summary>
    public string Description { get; set; } = "Background service to monitor Ubiquiti product stock and send notifications";

    /// <summary>
    /// Path to the executable or DLL to run
    /// </summary>
    public string ExecutablePath { get; set; } = string.Empty;

    /// <summary>
    /// Startup type: Automatic, Manual, Disabled
    /// </summary>
    public string StartupType { get; set; } = "Automatic";

    /// <summary>
    /// Whether to enable delayed auto-start (Windows only)
    /// </summary>
    public bool DelayedAutoStart { get; set; } = true;

    /// <summary>
    /// User account to run service as (if applicable)
    /// </summary>
    public string? UserAccount { get; set; }

    /// <summary>
    /// Number of restart attempts on failure
    /// </summary>
    public int RestartAttemptsOnFailure { get; set; } = 3;

    /// <summary>
    /// Delay in seconds between restart attempts
    /// </summary>
    public int RestartDelaySeconds { get; set; } = 10;

    /// <summary>
    /// Dependencies (e.g., "Tcpip" on Windows)
    /// </summary>
    public List<string> Dependencies { get; set; } = new();

    public override string ToString() => $"{DisplayName} ({ServiceName})";
}
