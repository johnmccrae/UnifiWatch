namespace UnifiWatch.Services.Installation;

/// <summary>
/// Enumeration of possible service states
/// </summary>
public enum ServiceState
{
    NotInstalled,
    Installed,
    Running,
    Stopped,
    Unknown
}

/// <summary>
/// Status information for UnifiWatch service
/// </summary>
public class ServiceStatus
{
    /// <summary>
    /// Current service state
    /// </summary>
    public ServiceState State { get; set; } = ServiceState.Unknown;

    /// <summary>
    /// Service display name
    /// </summary>
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>
    /// Startup mode (Automatic, Manual, Disabled, etc.)
    /// </summary>
    public string StartupType { get; set; } = string.Empty;

    /// <summary>
    /// Last known startup timestamp
    /// </summary>
    public DateTime? LastStartTime { get; set; }

    /// <summary>
    /// Service process ID (if running)
    /// </summary>
    public int? ProcessId { get; set; }

    /// <summary>
    /// Human-readable status message
    /// </summary>
    public string Message { get; set; } = string.Empty;

    public override string ToString() => $"{State} - {DisplayName} ({StartupType})";
}
