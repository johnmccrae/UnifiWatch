using System.Text.Json.Serialization;

namespace UnifiWatch.Configuration;

/// <summary>
/// Main service configuration loaded from config.json
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
/// Service runtime settings
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
/// Notification configuration container
/// </summary>
public class NotificationSettings
{
    [JsonPropertyName("desktop")]
    public DesktopNotificationConfig Desktop { get; set; } = new();

    [JsonPropertyName("email")]
    public EmailNotificationConfig Email { get; set; } = new();

    [JsonPropertyName("sms")]
    public SmsNotificationConfig Sms { get; set; } = new();
}

/// <summary>
/// Desktop/toast notification configuration
/// </summary>
public class DesktopNotificationConfig
{
    [JsonPropertyName("enabled")]
    public bool Enabled { get; set; } = true;
}

/// <summary>
/// Email notification configuration
/// </summary>
public class EmailNotificationConfig
{
    [JsonPropertyName("enabled")]
    public bool Enabled { get; set; } = false;

    [JsonPropertyName("recipients")]
    public List<string> Recipients { get; set; } = new();

    [JsonPropertyName("smtpServer")]
    public string SmtpServer { get; set; } = "";

    [JsonPropertyName("smtpPort")]
    public int SmtpPort { get; set; } = 587;

    [JsonPropertyName("useTls")]
    public bool UseTls { get; set; } = true;

    [JsonPropertyName("fromAddress")]
    public string FromAddress { get; set; } = "";

    [JsonPropertyName("credentialKey")]
    public string CredentialKey { get; set; } = "email-smtp";

    /// <summary>
    /// Validates email configuration
    /// </summary>
    public bool IsValid()
    {
        if (!Enabled)
            return true;

        return !string.IsNullOrWhiteSpace(SmtpServer) &&
               SmtpPort > 0 && SmtpPort <= 65535 &&
               Recipients.Count > 0 &&
               !string.IsNullOrWhiteSpace(FromAddress) &&
               !string.IsNullOrWhiteSpace(CredentialKey);
    }
}

/// <summary>
/// SMS notification configuration
/// </summary>
public class SmsNotificationConfig
{
    [JsonPropertyName("enabled")]
    public bool Enabled { get; set; } = false;

    [JsonPropertyName("provider")]
    public string Provider { get; set; } = "twilio";

    [JsonPropertyName("recipients")]
    public List<string> Recipients { get; set; } = new();

    [JsonPropertyName("credentialKey")]
    public string CredentialKey { get; set; } = "sms-api";

    /// <summary>
    /// Validates SMS configuration
    /// </summary>
    public bool IsValid()
    {
        if (!Enabled)
            return true;

        return !string.IsNullOrWhiteSpace(Provider) &&
               Recipients.Count > 0 &&
               !string.IsNullOrWhiteSpace(CredentialKey);
    }
}

/// <summary>
/// Credential storage configuration
/// </summary>
public class CredentialSettings
{
    [JsonPropertyName("encrypted")]
    public bool Encrypted { get; set; } = true;

    [JsonPropertyName("storageMethod")]
    public string StorageMethod { get; set; } = "auto";
}
