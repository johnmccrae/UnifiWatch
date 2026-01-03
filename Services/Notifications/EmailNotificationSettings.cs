using System.Text.Json.Serialization;

namespace UnifiWatch.Services.Notifications;

/// <summary>
/// Configuration for email notification settings
/// </summary>
public class EmailNotificationSettings
{
    /// <summary>
    /// Whether email notifications are enabled
    /// </summary>
    [JsonPropertyName("enabled")]
    public bool Enabled { get; set; } = false;

    /// <summary>
    /// Recipients for email notifications
    /// </summary>
    [JsonPropertyName("recipients")]
    public List<string> Recipients { get; set; } = new();

    /// <summary>
    /// SMTP server address
    /// </summary>
    [JsonPropertyName("smtpServer")]
    public string SmtpServer { get; set; } = string.Empty;

    /// <summary>
    /// SMTP server port
    /// </summary>
    [JsonPropertyName("smtpPort")]
    public int SmtpPort { get; set; } = 587;

    /// <summary>
    /// Whether to use TLS for SMTP
    /// </summary>
    [JsonPropertyName("useTls")]
    public bool UseTls { get; set; } = true;

    /// <summary>
    /// From address for email
    /// </summary>
    [JsonPropertyName("fromAddress")]
    public string FromAddress { get; set; } = string.Empty;

    /// <summary>
    /// Credential provider key name for storing/retrieving SMTP password
    /// </summary>
    [JsonPropertyName("credentialKey")]
    public string CredentialKey { get; set; } = "email-smtp";

    /// <summary>
    /// Whether to use OAuth 2.0 with Microsoft Graph API instead of SMTP
    /// </summary>
    [JsonPropertyName("useOAuth")]
    public bool UseOAuth { get; set; } = false;

    /// <summary>
    /// Azure AD/Entra ID Tenant ID for OAuth
    /// </summary>
    [JsonPropertyName("oauthTenantId")]
    public string OAuthTenantId { get; set; } = string.Empty;

    /// <summary>
    /// Azure AD/Entra ID Client ID (Application ID) for OAuth
    /// </summary>
    [JsonPropertyName("oauthClientId")]
    public string OAuthClientId { get; set; } = string.Empty;

    /// <summary>
    /// Azure AD/Entra ID Client Secret for OAuth (stored in credential provider)
    /// </summary>
    [JsonPropertyName("oauthCredentialKey")]
    public string OAuthCredentialKey { get; set; } = "email-oauth";

    /// <summary>
    /// Mailbox email address for OAuth (shared mailbox or service account)
    /// </summary>
    [JsonPropertyName("oauthMailbox")]
    public string OAuthMailbox { get; set; } = string.Empty;
}
