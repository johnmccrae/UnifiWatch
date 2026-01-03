namespace UnifiWatch.Services.Notifications;

/// <summary>
/// Configuration for SMS notification provider (Twilio, SNS, Vonage, etc.)
/// </summary>
public class SmsNotificationSettings
{
    /// <summary>
    /// Whether SMS notifications are enabled
    /// </summary>
    public bool Enabled { get; set; } = false;

    /// <summary>
    /// Recipients for SMS notifications (E.164 format)
    /// </summary>
    public List<string> Recipients { get; set; } = new();

    /// <summary>
    /// SMS provider type: "twilio", "sns", "vonage"
    /// </summary>
    public string ServiceType { get; set; } = "twilio";

    /// <summary>
    /// Twilio Account SID (for Twilio provider)
    /// </summary>
    public string? TwilioAccountSid { get; set; }

    /// <summary>
    /// Phone number to send SMS from (E.164 format: +1234567890)
    /// </summary>
    public string FromPhoneNumber { get; set; } = string.Empty;

    /// <summary>
    /// Credential provider key name for storing/retrieving auth token
    /// For Twilio: auth token
    /// For SNS: AWS access key ID and secret
    /// For Vonage: API key and secret
    /// </summary>
    public string AuthTokenKeyName { get; set; } = "sms:auth-token";

    /// <summary>
    /// Maximum SMS length (default 160 characters)
    /// </summary>
    public int MaxMessageLength { get; set; } = 160;

    /// <summary>
    /// Whether to allow message shortening to fit SMS limit
    /// </summary>
    public bool AllowMessageShortening { get; set; } = true;
}
