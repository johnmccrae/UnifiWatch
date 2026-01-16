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
    [JsonPropertyName("language")]
    public string Language { get; set; } = "auto";

    [JsonPropertyName("timeZone")]
    public string TimeZone { get; set; } = "auto";}

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
/// Supports both SMTP and Microsoft Graph (Office 365/Outlook) providers
/// </summary>
public class EmailNotificationConfig
{
    [JsonPropertyName("enabled")]
    public bool Enabled { get; set; } = false;

    [JsonPropertyName("provider")]
    public string Provider { get; set; } = "smtp";  // "smtp" or "msgraph"

    // Sender configuration
    [JsonPropertyName("fromAddress")]
    public string FromAddress { get; set; } = "";

    [JsonPropertyName("fromName")]
    public string FromName { get; set; } = "UnifiWatch Alert";

    [JsonPropertyName("toAddresses")]
    public List<string> ToAddresses { get; set; } = new();

    // SMTP-specific configuration
    [JsonPropertyName("smtpServer")]
    public string SmtpServer { get; set; } = "smtp.gmail.com";

    [JsonPropertyName("smtpPort")]
    public int SmtpPort { get; set; } = 587;

    [JsonPropertyName("useSsl")]
    public bool UseSsl { get; set; } = true;

    [JsonPropertyName("credentialKey")]
    public string CredentialKey { get; set; } = "smtp:password";

    // Microsoft Graph-specific configuration (OAuth2)
    [JsonPropertyName("tenantId")]
    public string TenantId { get; set; } = "";

    [JsonPropertyName("clientId")]
    public string ClientId { get; set; } = "";

    [JsonPropertyName("clientSecret")]
    public string ClientSecret { get; set; } = "msgraph-client-secret";  // Credential key for secret

    // Backward compatibility aliases
    [JsonPropertyName("senderEmail")]
    public string SenderEmail
    {
        get => FromAddress;
        set => FromAddress = value;
    }

    [JsonPropertyName("recipients")]
    public List<string> Recipients
    {
        get => ToAddresses;
        set => ToAddresses = value;
    }

    [JsonPropertyName("useTls")]
    public bool UseTls
    {
        get => UseSsl;
        set => UseSsl = value;
    }

    /// <summary>
    /// Validates email configuration
    /// </summary>
    public bool IsValid()
    {
        if (!Enabled)
            return true;

        // Recipients are required when enabled
        if (ToAddresses == null || ToAddresses.Count == 0)
            return false;

        var provider = Provider?.ToLowerInvariant() ?? "smtp";

        if (provider == "smtp")
        {
            return !string.IsNullOrWhiteSpace(SmtpServer) &&
                   SmtpPort > 0 && SmtpPort <= 65535 &&
                   !string.IsNullOrWhiteSpace(FromAddress) &&
                   !string.IsNullOrWhiteSpace(CredentialKey);
        }
        else if (provider == "msgraph")
        {
            return !string.IsNullOrWhiteSpace(TenantId) &&
                   !string.IsNullOrWhiteSpace(ClientId) &&
                   !string.IsNullOrWhiteSpace(FromAddress);
        }

        return false;
    }
}

/// <summary>
/// SMS notification configuration
/// </summary>
public class SmsNotificationConfig
{
    [JsonPropertyName("enabled")]
    public bool Enabled { get; set; } = false;

    [JsonPropertyName("serviceType")]
    public string ServiceType { get; set; } = "twilio";

    [JsonPropertyName("toPhoneNumbers")]
    public List<string> ToPhoneNumbers { get; set; } = new();

    [JsonPropertyName("fromPhoneNumber")]
    public string FromPhoneNumber { get; set; } = "";

    // Twilio-specific configuration
    [JsonPropertyName("twilioAccountSid")]
    public string TwilioAccountSid { get; set; } = "";

    [JsonPropertyName("authTokenKeyName")]
    public string AuthTokenKeyName { get; set; } = "sms:twilio:auth-token";

    [JsonPropertyName("maxMessageLength")]
    public int MaxMessageLength { get; set; } = 160;

    [JsonPropertyName("allowMessageShortening")]
    public bool AllowMessageShortening { get; set; } = true;

    // Backward compatibility aliases
    [JsonPropertyName("provider")]
    public string Provider
    {
        get => ServiceType;
        set => ServiceType = value;
    }

    [JsonPropertyName("recipients")]
    public List<string> Recipients
    {
        get => ToPhoneNumbers;
        set => ToPhoneNumbers = value;
    }

    [JsonPropertyName("credentialKey")]
    public string CredentialKey
    {
        get => AuthTokenKeyName;
        set => AuthTokenKeyName = value;
    }

    /// <summary>
    /// Validates SMS configuration
    /// </summary>
    public bool IsValid()
    {
        if (!Enabled)
            return true;

        // Phone numbers are required when enabled
        if (ToPhoneNumbers == null || ToPhoneNumbers.Count == 0)
            return false;

        var serviceType = ServiceType?.ToLowerInvariant() ?? "twilio";

        if (serviceType == "twilio")
        {
            return !string.IsNullOrWhiteSpace(TwilioAccountSid) &&
                   !string.IsNullOrWhiteSpace(FromPhoneNumber) &&
                   !string.IsNullOrWhiteSpace(AuthTokenKeyName);
        }

        return !string.IsNullOrWhiteSpace(ServiceType) &&
               !string.IsNullOrWhiteSpace(AuthTokenKeyName);
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
