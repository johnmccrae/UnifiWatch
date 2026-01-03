using System.Text.Json.Serialization;

namespace UnifiStockTracker.Models;

/// <summary>
/// Root configuration model for service mode
/// </summary>
public class ServiceConfiguration
{
    [JsonPropertyName("service")]
    public ServiceSettings Service { get; set; } = new();

    [JsonPropertyName("monitoring")]
    public MonitoringSettings Monitoring { get; set; } = new();

    [JsonPropertyName("notifications")]
    public NotificationSettings Notifications { get; set; } = new();

    [JsonPropertyName("credentials")]
    public CredentialSettings Credentials { get; set; } = new();
}

/// <summary>
/// Service-level configuration (auto-start, check intervals, pause state)
/// </summary>
public class ServiceSettings
{
    [JsonPropertyName("enabled")]
    public bool Enabled { get; set; } = true;

    [JsonPropertyName("autoStart")]
    public bool AutoStart { get; set; } = true;

    [JsonPropertyName("checkIntervalSeconds")]
    public int CheckIntervalSeconds { get; set; } = 300;

    [JsonPropertyName("paused")]
    public bool Paused { get; set; } = false;
}

/// <summary>
/// Stock monitoring configuration
/// </summary>
public class MonitoringSettings
{
    [JsonPropertyName("store")]
    public string Store { get; set; } = "USA";

    [JsonPropertyName("collections")]
    public List<string> Collections { get; set; } = new();

    [JsonPropertyName("productNames")]
    public List<string> ProductNames { get; set; } = new();

    [JsonPropertyName("productSkus")]
    public List<string> ProductSkus { get; set; } = new();

    [JsonPropertyName("useModernApi")]
    public bool UseModernApi { get; set; } = true;
}

/// <summary>
/// Notification channels configuration
/// </summary>
public class NotificationSettings
{
    [JsonPropertyName("desktop")]
    public DesktopNotificationSettings Desktop { get; set; } = new();

    [JsonPropertyName("email")]
    public EmailNotificationSettings Email { get; set; } = new();

    [JsonPropertyName("sms")]
    public SmsNotificationSettings Sms { get; set; } = new();
}

/// <summary>
/// Desktop notification configuration
/// </summary>
public class DesktopNotificationSettings
{
    [JsonPropertyName("enabled")]
    public bool Enabled { get; set; } = true;
}

/// <summary>
/// Email notification configuration
/// </summary>
public class EmailNotificationSettings
{
    [JsonPropertyName("enabled")]
    public bool Enabled { get; set; } = false;

    [JsonPropertyName("recipients")]
    public List<string> Recipients { get; set; } = new();

    [JsonPropertyName("smtpServer")]
    public string SmtpServer { get; set; } = string.Empty;

    [JsonPropertyName("smtpPort")]
    public int SmtpPort { get; set; } = 587;

    [JsonPropertyName("useTls")]
    public bool UseTls { get; set; } = true;

    [JsonPropertyName("fromAddress")]
    public string FromAddress { get; set; } = string.Empty;

    [JsonPropertyName("credentialKey")]
    public string CredentialKey { get; set; } = "email-smtp";
}

/// <summary>
/// SMS notification configuration
/// </summary>
public class SmsNotificationSettings
{
    [JsonPropertyName("enabled")]
    public bool Enabled { get; set; } = false;

    [JsonPropertyName("provider")]
    public string Provider { get; set; } = "twilio"; // twilio, awssns, azure, smtp-gateway

    [JsonPropertyName("recipients")]
    public List<string> Recipients { get; set; } = new();

    [JsonPropertyName("credentialKey")]
    public string CredentialKey { get; set; } = "sms-provider";
}

/// <summary>
/// Credential storage configuration
/// </summary>
public class CredentialSettings
{
    [JsonPropertyName("encrypted")]
    public bool Encrypted { get; set; } = true;

    [JsonPropertyName("storageMethod")]
    public string StorageMethod { get; set; } = "auto"; // auto, windows-credential-manager, macos-keychain, linux-secret-service, encrypted-file, environment-variables
}
