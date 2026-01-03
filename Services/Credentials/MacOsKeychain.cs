using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace UnifiWatch.Services.Credentials;

/// <summary>
/// macOS Keychain implementation using native security command
/// Stores credentials in the default login keychain
/// </summary>
public class MacOsKeychain : ICredentialProvider
{
    private readonly ILogger<MacOsKeychain> _logger;
    private const string ServiceName = "UnifiWatch";

    public string StorageMethodDescription => "macOS Keychain";
    public string StorageMethod => "macos-keychain";

    public MacOsKeychain(ILogger<MacOsKeychain> logger)
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

                // First, delete any existing entry to avoid duplicates
                _ = DeleteAsync(key, cancellationToken).Result;

                // Create the credential description
                var credentialDescription = string.IsNullOrWhiteSpace(label)
                    ? $"UnifiWatch: {key}"
                    : label;

                // Use security add-generic-password command
                var process = CreateProcess(
                    "security",
                    $"add-generic-password -s \"{ServiceName}\" -a \"{key}\" -w \"{EscapeShellString(secret)}\" -l \"{credentialDescription}\" -U",
                    cancellationToken
                );

                if (!process.Result)
                {
                    _logger.LogError("Failed to store credential for key {Key}", key);
                    return false;
                }

                _logger.LogDebug("Credential stored successfully for key {Key}", key);
                return true;
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

                // Use security find-generic-password command to retrieve the password
                var result = ExecuteCommand(
                    "security",
                    $"find-generic-password -s \"{ServiceName}\" -a \"{key}\" -w",
                    cancellationToken
                ).Result;

                if (result.exitCode != 0)
                {
                    if (result.exitCode != 44) // 44 = security: SecItemNotFound
                    {
                        _logger.LogWarning("security find-generic-password failed with exit code {ExitCode} for key {Key}",
                            result.exitCode, key);
                    }
                    return null;
                }

                var secret = result.output.Trim();
                if (string.IsNullOrEmpty(secret))
                {
                    _logger.LogWarning("Retrieved empty credential for key {Key}", key);
                    return null;
                }

                _logger.LogDebug("Credential retrieved successfully for key {Key}", key);
                return secret;
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

                var result = ExecuteCommand(
                    "security",
                    $"delete-generic-password -s \"{ServiceName}\" -a \"{key}\"",
                    cancellationToken
                ).Result;

                if (result.exitCode != 0)
                {
                    if (result.exitCode != 44) // Not found is ok
                    {
                        _logger.LogWarning("security delete-generic-password failed with exit code {ExitCode} for key {Key}",
                            result.exitCode, key);
                    }
                    return result.exitCode == 44; // Return true if not found
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

                var result = ExecuteCommand(
                    "security",
                    $"find-generic-password -s \"{ServiceName}\" -a \"{key}\"",
                    cancellationToken
                ).Result;

                return result.exitCode == 0;
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
                // Use security dump-keychain to list all items for our service
                var result = ExecuteCommand(
                    "security",
                    $"dump-keychain -a",
                    cancellationToken
                ).Result;

                if (result.exitCode != 0)
                {
                    _logger.LogDebug("No credentials found for application");
                    return credentials;
                }

                // Parse the output - look for our service names
                foreach (var line in result.output.Split('\n'))
                {
                    if (line.Contains($"\"svc\"=\"{ServiceName}\""))
                    {
                        // Extract the account name
                        var match = System.Text.RegularExpressions.Regex.Match(line, @"""acct""=""([^""]+)""");
                        if (match.Success)
                        {
                            credentials.Add(match.Groups[1].Value);
                        }
                    }
                }

                _logger.LogDebug("Found {Count} credentials for application", credentials.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error listing credentials");
            }

            return credentials;
        }, cancellationToken);
    }

    /// <summary>
    /// Executes a command and returns exit code and output
    /// </summary>
    private static Task<(int exitCode, string output)> ExecuteCommand(
        string fileName,
        string arguments,
        CancellationToken cancellationToken)
    {
        var tcs = new TaskCompletionSource<(int, string)>();

        if (cancellationToken.IsCancellationRequested)
        {
            tcs.SetCanceled();
            return tcs.Task;
        }

        try
        {
            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = fileName,
                    Arguments = arguments,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                }
            };

            var output = new System.Text.StringBuilder();
            var error = new System.Text.StringBuilder();

            process.OutputDataReceived += (s, e) =>
            {
                if (!string.IsNullOrEmpty(e.Data))
                    output.AppendLine(e.Data);
            };

            process.ErrorDataReceived += (s, e) =>
            {
                if (!string.IsNullOrEmpty(e.Data))
                    error.AppendLine(e.Data);
            };

            process.EnableRaisingEvents = true;
            process.Exited += (s, e) =>
            {
                try
                {
                    tcs.SetResult((process.ExitCode, output.ToString()));
                }
                finally
                {
                    process.Dispose();
                }
            };

            if (!process.Start())
            {
                tcs.SetException(new InvalidOperationException("Failed to start process"));
            }
            else
            {
                process.BeginOutputReadLine();
                process.BeginErrorReadLine();

                // Add timeout
                using (cancellationToken.Register(() =>
                {
                    try { process?.Kill(); } catch { }
                }))
                {
                    var timeoutTask = Task.Delay(TimeSpan.FromSeconds(10), cancellationToken);
                    _ = Task.WhenAny(tcs.Task, timeoutTask).ContinueWith(_ =>
                    {
                        if (!tcs.Task.IsCompleted)
                        {
                            try { process?.Kill(); } catch { }
                            tcs.TrySetException(new OperationCanceledException("Command execution timed out"));
                        }
                    });
                }
            }
        }
        catch (Exception ex)
        {
            tcs.SetException(ex);
        }

        return tcs.Task;
    }

    /// <summary>
    /// Creates a process task for executing security command
    /// </summary>
    private static Task<bool> CreateProcess(
        string fileName,
        string arguments,
        CancellationToken cancellationToken)
    {
        return ExecuteCommand(fileName, arguments, cancellationToken)
            .ContinueWith(t => t.Result.exitCode == 0, cancellationToken);
    }

    /// <summary>
    /// Escapes shell special characters in a string
    /// </summary>
    private static string EscapeShellString(string input)
    {
        // Escape single quotes and wrap in single quotes
        return "'" + input.Replace("'", "'\\''") + "'";
    }
}
