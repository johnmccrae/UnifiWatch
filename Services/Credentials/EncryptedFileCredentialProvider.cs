using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using UnifiStockTracker.Utilities;

namespace UnifiStockTracker.Services.Credentials;

/// <summary>
/// Encrypted file-based credential provider using DPAPI (Windows) or AES (others)
/// Fallback option when OS-native credential storage is not available
/// WARNING: Less secure than OS credential storage - use only as fallback
/// </summary>
public class EncryptedFileCredentialProvider : ICredentialProvider
{
    private readonly ILogger<EncryptedFileCredentialProvider> _logger;
    private readonly string _credentialsFilePath;

    public string StorageMethodDescription => "Encrypted local file (fallback method)";
    public string StorageMethod => "encrypted-file";

    public EncryptedFileCredentialProvider(ILogger<EncryptedFileCredentialProvider> logger)
    {
        _logger = logger;
        var configDir = PlatformUtilities.GetConfigurationDirectory();
        _credentialsFilePath = Path.Combine(configDir, "credentials.enc.json");

        _logger.LogWarning("Using encrypted file credential storage. " +
            "For better security, consider using native OS credential storage (Credential Manager, Keychain, or secret-service).");
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

                var credentials = LoadCredentials();
                credentials[key] = secret;

                return SaveCredentials(credentials);
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

                var credentials = LoadCredentials();
                if (credentials.TryGetValue(key, out var secret))
                {
                    _logger.LogDebug("Credential retrieved for key {Key}", key);
                    return secret;
                }

                return null;
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

                var credentials = LoadCredentials();
                if (!credentials.Remove(key))
                    return true; // Already doesn't exist

                return SaveCredentials(credentials);
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

                var credentials = LoadCredentials();
                return credentials.ContainsKey(key);
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
            try
            {
                var credentials = LoadCredentials();
                return credentials.Keys.ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error listing credentials");
                return new List<string>();
            }
        }, cancellationToken);
    }

    private Dictionary<string, string> LoadCredentials()
    {
        try
        {
            if (!File.Exists(_credentialsFilePath))
                return new Dictionary<string, string>();

            var encryptedJson = File.ReadAllBytes(_credentialsFilePath);
            var decryptedJson = Decrypt(encryptedJson);
            var credentials = JsonSerializer.Deserialize<Dictionary<string, string>>(decryptedJson);

            return credentials ?? new Dictionary<string, string>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading credentials from {Path}", _credentialsFilePath);
            return new Dictionary<string, string>();
        }
    }

    private bool SaveCredentials(Dictionary<string, string> credentials)
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(_credentialsFilePath)!);

            var json = JsonSerializer.Serialize(credentials, new JsonSerializerOptions { WriteIndented = true });
            var encryptedJson = Encrypt(json);

            File.WriteAllBytes(_credentialsFilePath, encryptedJson);

            // Set restrictive permissions on Unix (600 = rw-------)
            if (!PlatformUtilities.IsWindows)
            {
                PlatformUtilities.SetDirectoryPermissions(_credentialsFilePath, "600");
            }

            _logger.LogDebug("Credentials saved to {Path}", _credentialsFilePath);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving credentials to {Path}", _credentialsFilePath);
            return false;
        }
    }

    /// <summary>
    /// Encrypts a JSON string using platform-appropriate method
    /// Windows: DPAPI, Others: AES-256-GCM with random key (stored in file header)
    /// </summary>
    private byte[] Encrypt(string plainJson)
    {
        try
        {
#if WINDOWS
            // Use DPAPI on Windows
            var plainBytes = Encoding.UTF8.GetBytes(plainJson);
            var encryptedBytes = System.Security.Cryptography.ProtectedData.Protect(
                plainBytes,
                null,
                System.Security.Cryptography.DataProtectionScope.CurrentUser
            );
            return encryptedBytes;
#else
            // Use AES-256-CBC on Linux/macOS (more portable than GCM)
            using var aes = Aes.Create();
            aes.KeySize = 256;
            aes.Mode = CipherMode.CBC;
            aes.Padding = PaddingMode.PKCS7;

            var plainBytes = Encoding.UTF8.GetBytes(plainJson);
            
            // Generate random IV
            aes.GenerateIV();
            
            using var encryptor = aes.CreateEncryptor();
            var ciphertext = encryptor.TransformFinalBlock(plainBytes, 0, plainBytes.Length);

            // Return: [key length][key][iv length][iv][ciphertext]
            using var ms = new MemoryStream();
            
            ms.WriteByte((byte)aes.Key.Length);
            ms.Write(aes.Key);

            ms.WriteByte((byte)aes.IV.Length);
            ms.Write(aes.IV);

            ms.Write(ciphertext);
            return ms.ToArray();
#endif
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error encrypting credentials");
            throw;
        }
    }

    /// <summary>
    /// Decrypts an encrypted credentials blob
    /// </summary>
    private string Decrypt(byte[] encryptedData)
    {
        try
        {
#if WINDOWS
            // Use DPAPI on Windows
            var decryptedBytes = System.Security.Cryptography.ProtectedData.Unprotect(
                encryptedData,
                null,
                System.Security.Cryptography.DataProtectionScope.CurrentUser
            );
            return Encoding.UTF8.GetString(decryptedBytes);
#else
            // Use AES-256-CBC on Linux/macOS
            using var ms = new MemoryStream(encryptedData);
            
            var keyLength = ms.ReadByte();
            var key = new byte[keyLength];
            ms.Read(key, 0, keyLength);

            var ivLength = ms.ReadByte();
            var iv = new byte[ivLength];
            ms.Read(iv, 0, ivLength);

            var ciphertext = new byte[ms.Length - ms.Position];
            ms.Read(ciphertext, 0, ciphertext.Length);

            using var aes = Aes.Create();
            aes.Key = key;
            aes.IV = iv;
            aes.Mode = CipherMode.CBC;
            aes.Padding = PaddingMode.PKCS7;

            using var decryptor = aes.CreateDecryptor();
            var plaintext = decryptor.TransformFinalBlock(ciphertext, 0, ciphertext.Length);
            return Encoding.UTF8.GetString(plaintext);
#endif
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error decrypting credentials");
            throw;
        }
    }
}
