using System.Runtime.InteropServices;
using System.Text;
using Microsoft.Extensions.Logging;

namespace UnifiWatch.Services.Credentials;

/// <summary>
/// Windows Credential Manager implementation using native Win32 API
/// Stores credentials in Windows Credential Manager visible in Settings
/// </summary>
public class WindowsCredentialManager : ICredentialProvider
{
    private readonly ILogger<WindowsCredentialManager> _logger;
    private const string TargetNamePrefix = "UnifiWatch";

    public string StorageMethodDescription => "Windows Credential Manager";
    public string StorageMethod => "windows-credential-manager";

    // Win32 API declarations
    [DllImport("Advapi32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    private static extern bool CredWrite(ref CREDENTIAL credential, uint flags);

    [DllImport("Advapi32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    private static extern bool CredRead(string targetName, uint type, uint flags, out IntPtr pcredential);

    [DllImport("Advapi32.dll", SetLastError = true)]
    private static extern bool CredDelete(string targetName, uint type, uint flags);

    [DllImport("Advapi32.dll", SetLastError = true)]
    private static extern void CredFree(IntPtr cred);

    [DllImport("Advapi32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    private static extern bool CredEnumerate(string filter, uint flags, out uint count, out IntPtr ppCredentials);

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct CREDENTIAL
    {
        public uint Flags;
        public uint Type;
        public string TargetName;
        public string Comment;
        public long LastWritten;
        public uint CredentialBlobSize;
        public IntPtr CredentialBlob;
        public uint Persist;
        public uint AttributeCount;
        public IntPtr pAttributes;
        public string TargetAlias;
        public string UserName;
    }

    private const uint CRED_TYPE_GENERIC = 1;
    private const uint CRED_PERSIST_LOCAL_MACHINE = 2;
    private const uint CRED_FLAGS_PROMPT_NOW = 2;

    public WindowsCredentialManager(ILogger<WindowsCredentialManager> logger)
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

                var targetName = TargetNamePrefix + key;
                var secretBytes = Encoding.UTF8.GetBytes(secret);

                var credential = new CREDENTIAL
                {
                    Type = CRED_TYPE_GENERIC,
                    TargetName = targetName,
                    UserName = Environment.UserName,
                    CredentialBlob = Marshal.AllocCoTaskMem(secretBytes.Length),
                    CredentialBlobSize = (uint)secretBytes.Length,
                    Persist = CRED_PERSIST_LOCAL_MACHINE,
                    Comment = label
                };

                try
                {
                    Marshal.Copy(secretBytes, 0, credential.CredentialBlob, secretBytes.Length);

                    if (!CredWrite(ref credential, 0))
                    {
                        var error = Marshal.GetLastWin32Error();
                        _logger.LogError("CredWrite failed with error code {ErrorCode} for key {Key}", error, key);
                        return false;
                    }

                    _logger.LogDebug("Credential stored successfully for key {Key}", key);
                    return true;
                }
                finally
                {
                    if (credential.CredentialBlob != IntPtr.Zero)
                        Marshal.FreeCoTaskMem(credential.CredentialBlob);
                }
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

                var targetName = TargetNamePrefix + key;

                if (!CredRead(targetName, CRED_TYPE_GENERIC, 0, out var credPtr))
                {
                    var error = Marshal.GetLastWin32Error();
                    if (error != 1168) // ERROR_NOT_FOUND
                    {
                        _logger.LogWarning("CredRead failed with error code {ErrorCode} for key {Key}", error, key);
                    }
                    return null;
                }

                try
                {
                    var credential = Marshal.PtrToStructure<CREDENTIAL>(credPtr);
                    if (credential.CredentialBlob == IntPtr.Zero || credential.CredentialBlobSize == 0)
                    {
                        _logger.LogWarning("Retrieved credential has no blob for key {Key}", key);
                        return null;
                    }

                    var secretBytes = new byte[credential.CredentialBlobSize];
                    Marshal.Copy(credential.CredentialBlob, secretBytes, 0, (int)credential.CredentialBlobSize);
                    var secret = Encoding.UTF8.GetString(secretBytes);

                    _logger.LogDebug("Credential retrieved successfully for key {Key}", key);
                    return secret;
                }
                finally
                {
                    CredFree(credPtr);
                }
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

                var targetName = TargetNamePrefix + key;

                if (!CredDelete(targetName, CRED_TYPE_GENERIC, 0))
                {
                    var error = Marshal.GetLastWin32Error();
                    if (error != 1168) // ERROR_NOT_FOUND
                    {
                        _logger.LogWarning("CredDelete failed with error code {ErrorCode} for key {Key}", error, key);
                    }
                    return error == 1168; // Return true if not found (already deleted)
                }

                _logger.LogDebug("Credential deleted successfully for key {Key}", key);
                return true;
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

                var targetName = TargetNamePrefix + key;
                var exists = CredRead(targetName, CRED_TYPE_GENERIC, 0, out var credPtr);

                if (exists)
                    CredFree(credPtr);

                return exists;
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
                var filter = TargetNamePrefix + "*";
                if (!CredEnumerate(filter, 0, out var count, out var ppCredentials))
                {
                    _logger.LogDebug("No credentials found for application");
                    return credentials;
                }

                try
                {
                    for (int i = 0; i < count; i++)
                    {
                        var credPtr = Marshal.ReadIntPtr(ppCredentials, i * IntPtr.Size);
                        var credential = Marshal.PtrToStructure<CREDENTIAL>(credPtr);

                        if (credential.TargetName.StartsWith(TargetNamePrefix))
                        {
                            var key = credential.TargetName.Substring(TargetNamePrefix.Length);
                            credentials.Add(key);
                        }
                    }

                    _logger.LogDebug("Found {Count} credentials for application", credentials.Count);
                }
                finally
                {
                    CredFree(ppCredentials);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error listing credentials");
            }

            return credentials;
        }, cancellationToken);
    }
}
