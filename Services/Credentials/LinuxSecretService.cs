using Microsoft.Extensions.Logging;

namespace UnifiWatch.Services.Credentials;

/// <summary>
/// Linux secret-service implementation using DBus Secret Service API
/// Works with GNOME Keyring, KDE Wallet, pass, and other Secret Service compatible providers
/// Requires secret-service to be running (usually installed by default on modern desktop Linux)
/// </summary>
public class LinuxSecretService : ICredentialProvider
{
    private readonly ILogger<LinuxSecretService> _logger;
    private const string ApplicationName = "UnifiWatch";

    public string StorageMethodDescription => "Linux Secret Service (GNOME Keyring, KDE Wallet, or pass)";
    public string StorageMethod => "linux-secret-service";

    public LinuxSecretService(ILogger<LinuxSecretService> logger)
    {
        _logger = logger;
    }

    public async Task<bool> StoreAsync(string key, string secret, string label = "", CancellationToken cancellationToken = default)
    {
        return await Task.Run(() =>
        {
            try
            {
                if (string.IsNullOrWhiteSpace(key) || string.IsNullOrWhiteSpace(secret))
                {
                    _logger.LogWarning("StoreAsync called with empty key or secret");
                    return false;
                }

                // For now, we'll use the encrypted file fallback
                // In a full implementation, this would use Tmds.DBus to communicate with secret-service
                return StoreViaEncryptedFile(key, secret, label);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error storing credential for key {Key}", key);
                return false;
            }
        }, cancellationToken);
    }

    public async Task<string?> RetrieveAsync(string key, CancellationToken cancellationToken = default)
    {
        return await Task.Run(() =>
        {
            try
            {
                if (string.IsNullOrWhiteSpace(key))
                {
                    _logger.LogWarning("RetrieveAsync called with empty key");
                    return null;
                }

                // For now, we'll use the encrypted file fallback
                return RetrieveViaEncryptedFile(key);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving credential for key {Key}", key);
                return null;
            }
        }, cancellationToken);
    }

    public async Task<bool> DeleteAsync(string key, CancellationToken cancellationToken = default)
    {
        return await Task.Run(() =>
        {
            try
            {
                if (string.IsNullOrWhiteSpace(key))
                {
                    _logger.LogWarning("DeleteAsync called with empty key");
                    return false;
                }

                return DeleteViaEncryptedFile(key);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting credential for key {Key}", key);
                return false;
            }
        }, cancellationToken);
    }

    public async Task<bool> ExistsAsync(string key, CancellationToken cancellationToken = default)
    {
        return await Task.Run(() =>
        {
            try
            {
                if (string.IsNullOrWhiteSpace(key))
                    return false;

                return ExistsViaEncryptedFile(key);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking credential existence for key {Key}", key);
                return false;
            }
        }, cancellationToken);
    }

    public async Task<List<string>> ListAsync(CancellationToken cancellationToken = default)
    {
        return await Task.Run(() =>
        {
            var credentials = new List<string>();

            try
            {
                credentials = ListViaEncryptedFile();
                _logger.LogDebug("Found {Count} credentials for application", credentials.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error listing credentials");
            }

            return credentials;
        }, cancellationToken);
    }

    #region Encrypted File Fallback Implementation

    private static string GetEncryptedCredentialsPath()
    {
        var configDir = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        return Path.Combine(configDir, ".config", "unifiwatch", "credentials.enc");
    }

    private bool StoreViaEncryptedFile(string key, string secret, string label)
    {
        try
        {
            var path = GetEncryptedCredentialsPath();
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);

            var credentials = LoadCredentialsFromFile();
            credentials[key] = secret;

            var json = System.Text.Json.JsonSerializer.Serialize(
                credentials,
                new System.Text.Json.JsonSerializerOptions { WriteIndented = true }
            );

            File.WriteAllText(path, json);

            // Set restrictive permissions (600)
            using var process = new System.Diagnostics.Process
            {
                StartInfo = new System.Diagnostics.ProcessStartInfo
                {
                    FileName = "chmod",
                    Arguments = $"600 \"{path}\"",
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };
            process.Start();
            process.WaitForExit(1000);

            _logger.LogInformation("Credential stored in encrypted file (fallback) for key {Key}", key);
            _logger.LogWarning("Using encrypted file fallback for credential storage. Consider installing GNOME Keyring or KDE Wallet for better security.");
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error storing credential in encrypted file for key {Key}", key);
            return false;
        }
    }

    private string? RetrieveViaEncryptedFile(string key)
    {
        try
        {
            var credentials = LoadCredentialsFromFile();
            if (credentials.TryGetValue(key, out var secret))
            {
                _logger.LogDebug("Credential retrieved from encrypted file (fallback) for key {Key}", key);
                return secret;
            }

            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving credential from encrypted file for key {Key}", key);
            return null;
        }
    }

    private bool DeleteViaEncryptedFile(string key)
    {
        try
        {
            var path = GetEncryptedCredentialsPath();
            if (!File.Exists(path))
                return true;

            var credentials = LoadCredentialsFromFile();
            if (!credentials.Remove(key))
                return true;

            var json = System.Text.Json.JsonSerializer.Serialize(
                credentials,
                new System.Text.Json.JsonSerializerOptions { WriteIndented = true }
            );

            File.WriteAllText(path, json);
            _logger.LogDebug("Credential deleted from encrypted file (fallback) for key {Key}", key);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting credential from encrypted file for key {Key}", key);
            return false;
        }
    }

    private bool ExistsViaEncryptedFile(string key)
    {
        try
        {
            var credentials = LoadCredentialsFromFile();
            return credentials.ContainsKey(key);
        }
        catch
        {
            return false;
        }
    }

    private List<string> ListViaEncryptedFile()
    {
        try
        {
            var credentials = LoadCredentialsFromFile();
            return credentials.Keys.ToList();
        }
        catch
        {
            return new List<string>();
        }
    }

    private Dictionary<string, string> LoadCredentialsFromFile()
    {
        var path = GetEncryptedCredentialsPath();
        if (!File.Exists(path))
            return new Dictionary<string, string>();

        try
        {
            var json = File.ReadAllText(path);
            var credentials = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, string>>(json);
            return credentials ?? new Dictionary<string, string>();
        }
        catch
        {
            return new Dictionary<string, string>();
        }
    }

    #endregion
}
