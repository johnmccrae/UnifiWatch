using Microsoft.Extensions.Logging;
using UnifiWatch.Utilities;

namespace UnifiWatch.Services.Credentials;

/// <summary>
/// Factory for creating platform-appropriate credential providers
/// </summary>
public static class CredentialProviderFactory
{
    /// <summary>
    /// Creates the appropriate credential provider for the current platform
    /// </summary>
    /// <param name="storageMethod">Preferred storage method ("auto" = platform default)</param>
    /// <param name="loggerFactory">Logger factory instance</param>
    /// <returns>A platform-appropriate credential provider</returns>
    public static ICredentialProvider CreateProvider(
        string storageMethod,
        ILoggerFactory loggerFactory)
    {
        // Normalize the storage method
        storageMethod = storageMethod?.ToLowerInvariant()?.Trim() ?? "auto";

        // If "auto", use platform default
        if (storageMethod == "auto")
        {
            storageMethod = PlatformUtilities.GetDefaultCredentialStorageMethod();
        }

        // Create the appropriate provider
        return storageMethod switch
        {
            "windows-credential-manager" when PlatformUtilities.IsWindows =>
                new WindowsCredentialManager(loggerFactory.CreateLogger<WindowsCredentialManager>()),

            "macos-keychain" when PlatformUtilities.IsMacOS =>
                new MacOsKeychain(loggerFactory.CreateLogger<MacOsKeychain>()),

            "linux-secret-service" when PlatformUtilities.IsLinux =>
                new LinuxSecretService(loggerFactory.CreateLogger<LinuxSecretService>()),

            "encrypted-file" =>
                new EncryptedFileCredentialProvider(loggerFactory.CreateLogger<EncryptedFileCredentialProvider>()),

            "environment-variables" =>
                new EnvironmentVariableCredentialProvider(loggerFactory.CreateLogger<EnvironmentVariableCredentialProvider>()),

            _ => throw new NotSupportedException($"Credential storage method '{storageMethod}' is not supported on this platform")
        };
    }

    /// <summary>
    /// Creates a provider with logging support (legacy, use CreateProvider with ILoggerFactory)
    /// </summary>
    public static ICredentialProvider CreateProvider<TLogger>(
        string storageMethod,
        ILoggerFactory loggerFactory) where TLogger : class
    {
        return CreateProvider(storageMethod, loggerFactory);
    }
}
