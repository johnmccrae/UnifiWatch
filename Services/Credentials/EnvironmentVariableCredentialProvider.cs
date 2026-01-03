using Microsoft.Extensions.Logging;

namespace UnifiStockTracker.Services.Credentials;

/// <summary>
/// Environment variable-based credential provider
/// Fallback for headless/containerized environments where no credential storage is available
/// Format: UNIFISTOCK_CRED_{KEY} (all uppercase, hyphens replaced with underscores)
/// WARNING: Least secure option - only use in controlled automation scenarios
/// </summary>
public class EnvironmentVariableCredentialProvider : ICredentialProvider
{
    private readonly ILogger<EnvironmentVariableCredentialProvider> _logger;
    private const string EnvPrefix = "UNIFISTOCK_CRED_";

    public string StorageMethodDescription => "Environment variables (no persistence - headless only)";
    public string StorageMethod => "environment-variables";

    public EnvironmentVariableCredentialProvider(ILogger<EnvironmentVariableCredentialProvider> logger)
    {
        _logger = logger;
        _logger.LogWarning("Using environment variable credential storage. " +
            "Credentials are NOT persisted and must be set each run. " +
            "This method is intended only for headless automation/containerized environments.");
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

                var envVarName = GetEnvironmentVariableName(key);
                Environment.SetEnvironmentVariable(envVarName, secret, EnvironmentVariableTarget.Process);

                _logger.LogDebug("Credential stored in environment variable {EnvVar}", envVarName);
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

                var envVarName = GetEnvironmentVariableName(key);
                var secret = Environment.GetEnvironmentVariable(envVarName);

                if (secret == null)
                {
                    _logger.LogDebug("Environment variable {EnvVar} not found for key {Key}", envVarName, key);
                    return null;
                }

                _logger.LogDebug("Credential retrieved from environment variable for key {Key}", key);
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

                var envVarName = GetEnvironmentVariableName(key);
                Environment.SetEnvironmentVariable(envVarName, null, EnvironmentVariableTarget.Process);

                _logger.LogDebug("Credential deleted from environment variable for key {Key}", key);
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

                var envVarName = GetEnvironmentVariableName(key);
                return Environment.GetEnvironmentVariable(envVarName) != null;
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
                var envVars = Environment.GetEnvironmentVariables();
                foreach (var key in envVars.Keys)
                {
                    var envVarName = key.ToString();
                    if (envVarName?.StartsWith(EnvPrefix, StringComparison.OrdinalIgnoreCase) == true)
                    {
                        var credKey = envVarName.Substring(EnvPrefix.Length);
                        credentials.Add(credKey);
                    }
                }

                _logger.LogDebug("Found {Count} credentials in environment variables", credentials.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error listing credentials");
            }

            return credentials;
        }, cancellationToken);
    }

    /// <summary>
    /// Converts a credential key to environment variable name
    /// Example: "email-smtp" -> "UNIFISTOCK_CRED_EMAIL_SMTP"
    /// </summary>
    private string GetEnvironmentVariableName(string key)
    {
        var normalized = key
            .ToUpperInvariant()
            .Replace("-", "_")
            .Replace(":", "_");

        return EnvPrefix + normalized;
    }
}
