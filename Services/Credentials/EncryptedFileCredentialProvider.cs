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
    private static string? _cachedMachineIdentifier; // Cache the machine ID
    private static readonly object _machineIdLock = new object(); // Thread safety

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
                throw; // Rethrow so tests can see the error
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
        if (!File.Exists(_credentialsFilePath))
            return new Dictionary<string, string>();

        try
        {
            var encryptedJson = File.ReadAllBytes(_credentialsFilePath);
            var decryptedJson = Decrypt(encryptedJson);
            var credentials = JsonSerializer.Deserialize<Dictionary<string, string>>(decryptedJson);

            return credentials ?? new Dictionary<string, string>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading credentials from {Path}", _credentialsFilePath);
            throw;
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
    /// Derives an encryption key from machine/user context using PBKDF2
    /// This ensures the key is tied to the system and not stored in the file
    /// </summary>
    private byte[] DeriveEncryptionKey(byte[] salt)
    {
#if WINDOWS
        // Windows: Let DPAPI handle key derivation (this method not used on Windows)
        throw new NotSupportedException("Key derivation not needed on Windows - DPAPI handles this");
#else
        // Linux/macOS: Derive key from machine ID + username + hostname
        // This ties the key to the specific user on the specific machine
        var machineId = GetMachineIdentifier();
        var username = Environment.UserName;
        var hostname = Environment.MachineName;
        
        // Combine identifiers to create passphrase
        var passphrase = $"{machineId}:{username}:{hostname}";
        var passphraseBytes = Encoding.UTF8.GetBytes(passphrase);
        
        // Use PBKDF2 with 100,000 iterations (OWASP recommendation for 2023+)
        using var deriveBytes = new Rfc2898DeriveBytes(
            passphraseBytes,
            salt,
            100000,
            HashAlgorithmName.SHA256
        );
        
        return deriveBytes.GetBytes(32); // 256-bit key for AES-256
#endif
    }

    /// <summary>
    /// Gets a unique machine identifier for key derivation
    /// </summary>
    private string GetMachineIdentifier()
    {
        try
        {
            // Try to get machine ID from various sources
            // Linux: /etc/machine-id or /var/lib/dbus/machine-id
            // macOS: IOPlatformUUID
            
            if (PlatformUtilities.IsLinux)
            {
                // Try /etc/machine-id first (systemd)
                if (File.Exists("/etc/machine-id"))
                {
                    var machineId = File.ReadAllText("/etc/machine-id").Trim();
                    return machineId;
                }
                
                // Try D-Bus machine ID
                if (File.Exists("/var/lib/dbus/machine-id"))
                {
                    var machineId = File.ReadAllText("/var/lib/dbus/machine-id").Trim();
                    return machineId;
                }
            }
            else if (PlatformUtilities.IsMacOS)
            {
                // Get IOPlatformUUID on macOS (cache it since ioreg is slow)
                if (_cachedMachineIdentifier != null)
                {
                    return _cachedMachineIdentifier;
                }

                lock (_machineIdLock)
                {
                    if (_cachedMachineIdentifier != null)
                    {
                        return _cachedMachineIdentifier;
                    }

                    // Get IOPlatformUUID on macOS
                    var process = new System.Diagnostics.Process
                    {
                        StartInfo = new System.Diagnostics.ProcessStartInfo
                        {
                            FileName = "ioreg",
                            Arguments = "-rd1 -c IOPlatformExpertDevice",
                            UseShellExecute = false,
                            RedirectStandardOutput = true,
                            CreateNoWindow = true
                        }
                    };
                    
                    process.Start();
                    var output = process.StandardOutput.ReadToEnd();
                    process.WaitForExit();
                    
                    // Parse IOPlatformUUID from output
                    var match = System.Text.RegularExpressions.Regex.Match(
                        output, 
                        @"""IOPlatformUUID""\s*=\s*""([^""]+)"""
                    );
                    
                    if (match.Success)
                    {
                        _cachedMachineIdentifier = match.Groups[1].Value;
                        return _cachedMachineIdentifier;
                    }
                }
            }
            
            // Fallback: Use combination of hostname and MAC address hash (cache it)
            if (_cachedMachineIdentifier != null)
            {
                return _cachedMachineIdentifier;
            }

            lock (_machineIdLock)
            {
                if (_cachedMachineIdentifier != null)
                {
                    return _cachedMachineIdentifier;
                }

                var networkInterfaces = System.Net.NetworkInformation.NetworkInterface.GetAllNetworkInterfaces();
                var macAddress = networkInterfaces
                    .Where(ni => ni.OperationalStatus == System.Net.NetworkInformation.OperationalStatus.Up)
                    .Where(ni => ni.NetworkInterfaceType != System.Net.NetworkInformation.NetworkInterfaceType.Loopback)
                    .OrderBy(ni => ni.Name) // Sort by name for consistency
                    .Select(ni => ni.GetPhysicalAddress().ToString())
                    .FirstOrDefault() ?? "00:00:00:00:00:00";
                
                var fallbackId = $"{Environment.MachineName}:{macAddress}";
                using var sha256 = SHA256.Create();
                var hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(fallbackId));
                _cachedMachineIdentifier = Convert.ToHexString(hashBytes);
                return _cachedMachineIdentifier;
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Could not get machine identifier, using fallback");
            // Last resort: hash of hostname (cache it)
            if (_cachedMachineIdentifier != null)
            {
                return _cachedMachineIdentifier;
            }

            lock (_machineIdLock)
            {
                if (_cachedMachineIdentifier != null)
                {
                    return _cachedMachineIdentifier;
                }

                using var sha256 = SHA256.Create();
                var hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(Environment.MachineName));
                _cachedMachineIdentifier = Convert.ToHexString(hashBytes);
                return _cachedMachineIdentifier;
            }
        }
    }

    /// <summary>
    /// Encrypts a JSON string using platform-appropriate method
    /// Windows: DPAPI, Others: AES-256-CBC with PBKDF2-derived key (salt stored in file)
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
            // Use AES-256-CBC on Linux/macOS with PBKDF2-derived key
            // Generate random salt (32 bytes for extra security)
            var salt = new byte[32];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(salt);
            }
            
            // Derive encryption key from machine/user context
            var key = DeriveEncryptionKey(salt);
            
            using var aes = Aes.Create();
            aes.KeySize = 256;
            aes.Mode = CipherMode.CBC;
            aes.Padding = PaddingMode.PKCS7;
            aes.Key = key;
            aes.GenerateIV();

            var plainBytes = Encoding.UTF8.GetBytes(plainJson);
            
            using var encryptor = aes.CreateEncryptor();
            var ciphertext = encryptor.TransformFinalBlock(plainBytes, 0, plainBytes.Length);

            // Return: [salt length: 1 byte][salt: 32 bytes][iv length: 1 byte][iv: 16 bytes][ciphertext]
            // NOTE: Salt is stored, NOT the key (key is derived from salt + machine context)
            using var ms = new MemoryStream();
            
            ms.WriteByte((byte)salt.Length);
            ms.Write(salt, 0, salt.Length);

            ms.WriteByte((byte)aes.IV.Length);
            ms.Write(aes.IV, 0, aes.IV.Length);

            ms.Write(ciphertext, 0, ciphertext.Length);
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
            // Use AES-256-CBC on Linux/macOS with PBKDF2-derived key
            using var ms = new MemoryStream(encryptedData);
            
            // Read salt (used for key derivation)
            var saltLength = ms.ReadByte();
            var salt = new byte[saltLength];
            ms.Read(salt, 0, saltLength);

            // Read IV
            var ivLength = ms.ReadByte();
            var iv = new byte[ivLength];
            ms.Read(iv, 0, ivLength);

            // Read ciphertext
            var ciphertext = new byte[ms.Length - ms.Position];
            ms.Read(ciphertext, 0, ciphertext.Length);

            // Derive encryption key from machine/user context + salt
            var key = DeriveEncryptionKey(salt);

            using var aes = Aes.Create();
            aes.KeySize = 256;
            aes.Mode = CipherMode.CBC;
            aes.Padding = PaddingMode.PKCS7;
            aes.Key = key;
            aes.IV = iv;

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
