using System.Runtime.InteropServices;

namespace UnifiWatch.Utilities;

/// <summary>
/// Platform detection and OS-specific utilities
/// </summary>
public static class PlatformUtilities
{
    public static bool IsWindows => RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
    public static bool IsMacOS => RuntimeInformation.IsOSPlatform(OSPlatform.OSX);
    public static bool IsLinux => RuntimeInformation.IsOSPlatform(OSPlatform.Linux);

    /// <summary>
    /// Gets the appropriate configuration directory for the current platform
    /// </summary>
    /// <returns>
    /// Windows: %APPDATA%\UnifiWatch
    /// macOS: ~/.config/unifiwatch (preferred) or ~/Library/Application Support/UnifiWatch
    /// Linux: ~/.config/unifiwatch (preferred) or /etc/unifiwatch (system-wide)
    /// </returns>
    public static string GetConfigurationDirectory()
    {
        if (IsWindows)
        {
            var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            return Path.Combine(appData, "UnifiWatch");
        }
        else if (IsMacOS)
        {
            // Prefer ~/.config/unifiwatch, fall back to ~/Library/Application Support
            var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            var configDir = Path.Combine(home, ".config", "unifiwatch");
            if (Directory.Exists(configDir))
                return configDir;

            return Path.Combine(home, "Library", "Application Support", "UnifiWatch");
        }
        else if (IsLinux)
        {
            // Prefer ~/.config/unifiwatch for user, but check /etc/unifiwatch for system-wide
            var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            var userConfig = Path.Combine(home, ".config", "unifiwatch");

            // Check if running as root or with system privileges
            var uid = Environment.GetEnvironmentVariable("UID");
            if (uid == "0") // Running as root
            {
                var systemConfig = "/etc/unifiwatch";
                if (Directory.Exists(systemConfig))
                    return systemConfig;
            }

            return userConfig;
        }

        throw new PlatformNotSupportedException($"Unsupported OS: {RuntimeInformation.OSDescription}");
    }

    /// <summary>
    /// Gets the appropriate credential storage method for the current platform
    /// </summary>
    public static string GetDefaultCredentialStorageMethod()
    {
        if (IsWindows)
            return "windows-credential-manager";
        else if (IsMacOS)
            return "macos-keychain";
        else if (IsLinux)
            return "linux-secret-service";

        return "encrypted-file"; // Fallback
    }

    /// <summary>
    /// Creates the configuration directory if it doesn't exist
    /// Sets appropriate permissions on Unix-like systems
    /// </summary>
    public static void EnsureConfigurationDirectoryExists()
    {
        var dir = GetConfigurationDirectory();
        if (!Directory.Exists(dir))
        {
            Directory.CreateDirectory(dir);
            
            // Set restricted permissions on Unix-like systems (700 = rwx------)
            if (!IsWindows)
            {
                SetDirectoryPermissions(dir, "700");
            }
        }
    }

    /// <summary>
    /// Sets directory permissions on Unix-like systems using chmod
    /// </summary>
    public static void SetDirectoryPermissions(string path, string mode)
    {
        SetPermissionsInternal(path, mode);
    }

    /// <summary>
    /// Sets file permissions on Unix-like systems using chmod
    /// </summary>
    public static void SetFilePermissions(string path, string mode)
    {
        SetPermissionsInternal(path, mode);
    }

    private static void SetPermissionsInternal(string path, string mode)
    {
        if (IsWindows)
            return; // Skip on Windows

        try
        {
            using var process = new System.Diagnostics.Process
            {
                StartInfo = new System.Diagnostics.ProcessStartInfo
                {
                    FileName = "chmod",
                    Arguments = $"{mode} \"{path}\"",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                }
            };

            process.Start();
            process.WaitForExit(5000); // 5 second timeout
        }
        catch
        {
            // Log warning but don't fail - file/directory still usable
        }
    }

    /// <summary>
    /// Gets the appropriate line ending for the current platform
    /// </summary>
    public static string LineEnding => IsWindows ? "\r\n" : "\n";
}
