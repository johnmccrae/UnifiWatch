using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using UnifiStockTracker.Utilities;

namespace UnifiStockTracker.Configuration;

/// <summary>
/// File-based configuration provider with JSON serialization
/// Handles loading, saving, and validating service configuration
/// </summary>
public class ConfigurationProvider : IConfigurationProvider
{
    private readonly ILogger<ConfigurationProvider> _logger;
    private readonly string _configurationDirectory;
    private readonly string _configurationFilePath;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public string ConfigurationDirectory => _configurationDirectory;
    public string ConfigurationFilePath => _configurationFilePath;

    public ConfigurationProvider(ILogger<ConfigurationProvider> logger)
    {
        _logger = logger;
        _configurationDirectory = PlatformUtilities.GetConfigurationDirectory();
        _configurationFilePath = Path.Combine(_configurationDirectory, "config.json");
        
        PlatformUtilities.EnsureConfigurationDirectoryExists();
    }

    public async Task<ServiceConfiguration?> LoadAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            if (!File.Exists(_configurationFilePath))
            {
                _logger.LogWarning("Configuration file not found at {ConfigPath}", _configurationFilePath);
                return null;
            }

            var json = await File.ReadAllTextAsync(_configurationFilePath, cancellationToken);
            var config = JsonSerializer.Deserialize<ServiceConfiguration>(json, JsonOptions);

            if (config == null)
            {
                _logger.LogError("Failed to deserialize configuration from {ConfigPath}", _configurationFilePath);
                return null;
            }

            var validationErrors = Validate(config);
            if (validationErrors.Count > 0)
            {
                _logger.LogWarning("Configuration validation warnings: {Warnings}", 
                    string.Join(", ", validationErrors));
            }

            _logger.LogInformation("Configuration loaded successfully from {ConfigPath}", _configurationFilePath);
            return config;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading configuration from {ConfigPath}", _configurationFilePath);
            return null;
        }
    }

    public async Task<bool> SaveAsync(ServiceConfiguration config, CancellationToken cancellationToken = default)
    {
        try
        {
            var validationErrors = Validate(config);
            if (validationErrors.Count > 0)
            {
                _logger.LogWarning("Configuration has validation errors: {Errors}", 
                    string.Join(", ", validationErrors));
            }

            // Ensure directory exists
            if (!Directory.Exists(_configurationDirectory))
            {
                Directory.CreateDirectory(_configurationDirectory);
                if (!PlatformUtilities.IsWindows)
                {
                    PlatformUtilities.SetDirectoryPermissions(_configurationDirectory, "700");
                }
            }

            // Create backup if file already exists
            if (File.Exists(_configurationFilePath))
            {
                var backupPath = await BackupAsync(cancellationToken);
                _logger.LogInformation("Backed up existing configuration to {BackupPath}", backupPath);
            }

            var json = JsonSerializer.Serialize(config, JsonOptions);
            await File.WriteAllTextAsync(_configurationFilePath, json, cancellationToken);

            // Set restricted permissions on Unix-like systems
            if (!PlatformUtilities.IsWindows)
            {
                PlatformUtilities.SetFilePermissions(_configurationFilePath, "600");
            }

            _logger.LogInformation("Configuration saved successfully to {ConfigPath}", _configurationFilePath);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving configuration to {ConfigPath}", _configurationFilePath);
            return false;
        }
    }

    public List<string> Validate(ServiceConfiguration config)
    {
        var errors = new List<string>();

        if (config == null)
        {
            errors.Add("Configuration is null");
            return errors;
        }

        // Validate service settings
        if (config.Service.CheckIntervalSeconds < 10)
        {
            errors.Add("CheckIntervalSeconds must be at least 10 seconds");
        }

        if (config.Service.CheckIntervalSeconds > 86400)
        {
            errors.Add("CheckIntervalSeconds must not exceed 24 hours (86400 seconds)");
        }

        // Validate monitoring settings
        if (string.IsNullOrWhiteSpace(config.Monitoring.Store))
        {
            errors.Add("Monitoring.Store is required");
        }

        // Validate email if enabled
        if (!config.Notifications.Email.IsValid())
        {
            errors.Add("Email notification configuration is invalid");
        }

        // Validate SMS if enabled
        if (!config.Notifications.Sms.IsValid())
        {
            errors.Add("SMS notification configuration is invalid");
        }

        return errors;
    }

    public ServiceConfiguration GetDefaultConfiguration()
    {
        return new ServiceConfiguration
        {
            Service = new ServiceSettings
            {
                Enabled = true,
                AutoStart = true,
                CheckIntervalSeconds = 300,
                Paused = false
            },
            Monitoring = new MonitoringSettings
            {
                Store = "USA",
                Collections = new(),
                ProductNames = new(),
                ProductSkus = new(),
                UseModernApi = true
            },
            Notifications = new NotificationSettings
            {
                Desktop = new DesktopNotificationConfig { Enabled = true },
                Email = new EmailNotificationConfig { Enabled = false },
                Sms = new SmsNotificationConfig { Enabled = false }
            },
            Credentials = new CredentialSettings
            {
                Encrypted = true,
                StorageMethod = "auto"
            }
        };
    }

    public bool ConfigurationExists()
    {
        return File.Exists(_configurationFilePath);
    }

    public async Task<bool> DeleteAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            return await Task.Run(() =>
            {
                if (!File.Exists(_configurationFilePath))
                {
                    _logger.LogInformation("Configuration file does not exist, nothing to delete");
                    return true;
                }

                File.Delete(_configurationFilePath);
                _logger.LogInformation("Configuration file deleted: {ConfigPath}", _configurationFilePath);
                return true;
            }, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting configuration file {ConfigPath}", _configurationFilePath);
            return false;
        }
    }

    public async Task<string?> BackupAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            if (!File.Exists(_configurationFilePath))
            {
                return null;
            }

            // Use high-resolution timestamp to avoid collisions
            var timestamp = DateTime.UtcNow.ToString("yyyyMMdd_HHmmssfff");
            var backupPath = Path.Combine(_configurationDirectory, $"config.backup.{timestamp}.json");

            // If file still exists (very unlikely), append a counter
            var counter = 0;
            var originalBackupPath = backupPath;
            while (File.Exists(backupPath) && counter < 100)
            {
                counter++;
                backupPath = Path.Combine(_configurationDirectory, $"config.backup.{timestamp}.{counter}.json");
            }

            await Task.Run(() =>
            {
                File.Copy(_configurationFilePath, backupPath, overwrite: false);
            }, cancellationToken).ConfigureAwait(false);

            return backupPath;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating configuration backup");
            return null;
        }
    }
}
