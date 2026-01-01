using Microsoft.Extensions.Logging;

namespace UnifiWatch.Services.Installation;

/// <summary>
/// Factory for creating platform-specific service installers
/// </summary>
public static class ServiceInstallerFactory
{
    /// <summary>
    /// Create a service installer for the current platform
    /// </summary>
    public static IServiceInstaller CreateInstaller(ILogger? logger = null)
    {
        var loggerFactory = new LoggerFactory();
        
        return GetPlatform() switch
        {
            Platform.Windows => new WindowsServiceInstaller(loggerFactory.CreateLogger<WindowsServiceInstaller>()),
            Platform.Linux => new LinuxServiceInstaller(loggerFactory.CreateLogger<LinuxServiceInstaller>()),
            Platform.MacOs => new MacOsServiceInstaller(loggerFactory.CreateLogger<MacOsServiceInstaller>()),
            _ => throw new PlatformNotSupportedException($"Service installation is not supported on this platform")
        };
    }

    /// <summary>
    /// Create a service installer for a specific platform
    /// </summary>
    public static IServiceInstaller CreateInstaller(Platform platform, ILogger? logger = null)
    {
        var loggerFactory = new LoggerFactory();

        return platform switch
        {
            Platform.Windows => new WindowsServiceInstaller(loggerFactory.CreateLogger<WindowsServiceInstaller>()),
            Platform.Linux => new LinuxServiceInstaller(loggerFactory.CreateLogger<LinuxServiceInstaller>()),
            Platform.MacOs => new MacOsServiceInstaller(loggerFactory.CreateLogger<MacOsServiceInstaller>()),
            _ => throw new PlatformNotSupportedException($"Service installation is not supported on platform: {platform}")
        };
    }

    /// <summary>
    /// Get current platform
    /// </summary>
    public static Platform GetPlatform()
    {
        if (OperatingSystem.IsWindows())
            return Platform.Windows;
        if (OperatingSystem.IsLinux())
            return Platform.Linux;
        if (OperatingSystem.IsMacOS())
            return Platform.MacOs;

        throw new PlatformNotSupportedException("Unknown platform");
    }

    /// <summary>
    /// Supported platforms
    /// </summary>
    public enum Platform
    {
        Windows,
        Linux,
        MacOs
    }
}
