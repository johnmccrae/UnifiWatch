using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using UnifiWatch.Models;
using UnifiWatch.Utilities;

namespace UnifiWatch.Services.Configuration;

/// <summary>
/// JSON file-based configuration provider
/// Handles loading, saving, and validating service configurations
/// </summary>
public class JsonConfigurationProvider : IConfigurationProvider
{
    private readonly ILogger<JsonConfigurationProvider> _logger;
    private FileSystemWatcher? _fileWatcher;
    private Func<ServiceConfiguration, Task>? _onConfigurationChanged;
    private DateTime _lastSaveTime = DateTime.MinValue;

    public string ConfigurationPath { get; }
    public string ConfigurationDirectory { get; }

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public JsonConfigurationProvider(ILogger<JsonConfigurationProvider> logger)
    {
        _logger = logger;
        ConfigurationDirectory = PlatformUtilities.GetConfigurationDirectory();
        ConfigurationPath = Path.Combine(ConfigurationDirectory, "config.json");
        
        PlatformUtilities.EnsureConfigurationDirectoryExists();
        _logger.LogInformation("Configuration directory: {ConfigDir}", ConfigurationDirectory);
    }

    /// <summary>
    /// Loads configuration from JSON file or creates default
    /// </summary>
    public async Task<ServiceConfiguration> LoadAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            if (!File.Exists(ConfigurationPath))
            {
                _logger.LogInformation("Configuration file not found, creating default: {Path}", ConfigurationPath);
                var defaultConfig = GetDefaultConfiguration();
                await SaveAsync(defaultConfig, cancellationToken);
                return defaultConfig;
            }

            var json = await File.ReadAllTextAsync(ConfigurationPath, cancellationToken);
            var config = JsonSerializer.Deserialize<ServiceConfiguration>(json, JsonOptions);

            if (config == null)
            {
                _logger.LogWarning("Failed to deserialize configuration, using defaults");
                return GetDefaultConfiguration();
            }

            var validationErrors = Validate(config);
            if (validationErrors.Count > 0)
            {
                _logger.LogWarning("Configuration validation errors: {Errors}", string.Join("; ", validationErrors));
            }

            _logger.LogDebug("Configuration loaded successfully from {Path}", ConfigurationPath);
            return config;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading configuration from {Path}, using defaults", ConfigurationPath);
            return GetDefaultConfiguration();
        }
    }

    /// <summary>
    /// Saves configuration to JSON file
    /// </summary>
    public async Task<bool> SaveAsync(ServiceConfiguration config, CancellationToken cancellationToken = default)
    {
        try
        {
            var validationErrors = Validate(config);
            if (validationErrors.Count > 0)
            {
                _logger.LogWarning("Saving configuration with validation errors: {Errors}",
                    string.Join("; ", validationErrors));
            }

            // Ensure directory exists
            Directory.CreateDirectory(ConfigurationDirectory);

            var json = JsonSerializer.Serialize(config, JsonOptions);
            await File.WriteAllTextAsync(ConfigurationPath, json, cancellationToken);

            // Set restrictive permissions on Unix (600 = rw-------)
            if (!PlatformUtilities.IsWindows)
            {
                PlatformUtilities.SetDirectoryPermissions(ConfigurationPath, "600");
            }

            _lastSaveTime = DateTime.UtcNow;
            _logger.LogDebug("Configuration saved to {Path}", ConfigurationPath);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving configuration to {Path}", ConfigurationPath);
            return false;
        }
    }

    /// <summary>
    /// Validates configuration completeness and correctness
    /// </summary>
    public List<string> Validate(ServiceConfiguration config)
    {
        var errors = new List<string>();

        if (config == null)
        {
            errors.Add("Configuration is null");
            return errors;
        }

        // Validate monitoring settings
        if (string.IsNullOrWhiteSpace(config.Monitoring?.Store))
        {
            errors.Add("Monitoring.Store must not be empty");
        }

        // Validate notification settings
        if (config.Notifications != null)
        {
            if (config.Notifications.Email?.Enabled == true)
            {
                if (config.Notifications.Email.Recipients?.Count == 0)
                    errors.Add("Email notifications enabled but no recipients configured");
                if (string.IsNullOrWhiteSpace(config.Notifications.Email.SmtpServer))
                    errors.Add("Email notifications enabled but SMTP server not configured");
                if (string.IsNullOrWhiteSpace(config.Notifications.Email.FromAddress))
                    errors.Add("Email notifications enabled but from address not configured");
            }

            if (config.Notifications.Sms?.Enabled == true)
            {
                if (config.Notifications.Sms.Recipients?.Count == 0)
                    errors.Add("SMS notifications enabled but no recipients configured");
                if (string.IsNullOrWhiteSpace(config.Notifications.Sms.Provider))
                    errors.Add("SMS notifications enabled but provider not configured");
            }
        }

        // Validate service settings
        if (config.Service?.CheckIntervalSeconds < 10)
        {
            errors.Add("CheckIntervalSeconds must be at least 10 seconds");
        }

        return errors;
    }

    /// <summary>
    /// Gets a default configuration template
    /// </summary>
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
                Desktop = new DesktopNotificationSettings { Enabled = true },
                Email = new EmailNotificationSettings
                {
                    Enabled = false,
                    Recipients = new(),
                    SmtpServer = "smtp.gmail.com",
                    SmtpPort = 587,
                    UseTls = true,
                    FromAddress = string.Empty,
                    CredentialKey = "email-smtp"
                },
                Sms = new SmsNotificationSettings
                {
                    Enabled = false,
                    Provider = "twilio",
                    Recipients = new(),
                    CredentialKey = "sms-provider"
                }
            },
            Credentials = new CredentialSettings
            {
                Encrypted = true,
                StorageMethod = "auto"
            }
        };
    }

    /// <summary>
    /// Resets configuration to defaults
    /// </summary>
    public async Task<ServiceConfiguration> ResetAsync(CancellationToken cancellationToken = default)
    {
        var defaultConfig = GetDefaultConfiguration();
        await SaveAsync(defaultConfig, cancellationToken);
        _logger.LogInformation("Configuration reset to defaults");
        return defaultConfig;
    }

    /// <summary>
    /// Watches configuration file for changes
    /// </summary>
    public IDisposable WatchForChanges(Func<ServiceConfiguration, Task> onConfigurationChanged)
    {
        _onConfigurationChanged = onConfigurationChanged;
        _fileWatcher = new FileSystemWatcher(ConfigurationDirectory)
        {
            Filter = "config.json",
            NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.Size
        };

        _fileWatcher.Changed += async (s, e) =>
        {
            // Debounce: ignore rapid successive changes
            if ((DateTime.UtcNow - _lastSaveTime).TotalMilliseconds < 500)
                return;

            try
            {
                _logger.LogDebug("Configuration file changed, reloading");
                var updatedConfig = await LoadAsync();
                await onConfigurationChanged(updatedConfig);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error reloading configuration after file change");
            }
        };

        _fileWatcher.EnableRaisingEvents = true;
        _logger.LogDebug("File watcher enabled for {Path}", ConfigurationPath);

        return new ConfigurationWatcherDisposable(_fileWatcher, _logger);
    }

    private class ConfigurationWatcherDisposable : IDisposable
    {
        private readonly FileSystemWatcher _watcher;
        private readonly ILogger _logger;

        public ConfigurationWatcherDisposable(FileSystemWatcher watcher, ILogger logger)
        {
            _watcher = watcher;
            _logger = logger;
        }

        public void Dispose()
        {
            _watcher?.Dispose();
            _logger.LogDebug("Configuration file watcher disposed");
        }
    }
}
