using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Twilio;
using Twilio.Rest.Api.V2010.Account;
using Twilio.Types;
using UnifiWatch.Services.Credentials;

namespace UnifiWatch.Services.Notifications;

/// <summary>
/// SMS provider implementation using Twilio
/// Sends SMS messages via Twilio API with phone number validation
/// </summary>
public class TwilioSmsProvider : ISmsProvider
{
    private readonly SmsNotificationSettings _settings;
    private readonly ICredentialProvider _credentialProvider;
    private readonly ILogger<TwilioSmsProvider> _logger;

    public TwilioSmsProvider(
        IOptions<SmsNotificationSettings> settings,
        ICredentialProvider credentialProvider,
        ILogger<TwilioSmsProvider> logger)
    {
        _settings = settings.Value;
        _credentialProvider = credentialProvider;
        _logger = logger;
    }

    /// <summary>
    /// Sends SMS to a single recipient
    /// Validates phone number, checks message length, initializes Twilio client
    /// </summary>
    public async Task<bool> SendAsync(string recipient, string message, CancellationToken cancellationToken = default)
    {
        try
        {
            // Validate message length
            if (message.Length > _settings.MaxMessageLength)
            {
                _logger.LogError(
                    "SMS message exceeds {MaxLength} character limit: {MessageLength} characters",
                    _settings.MaxMessageLength,
                    message.Length);
                return false;
            }

            // Validate and normalize phone number
            var normalizedNumber = PhoneNumberValidator.NormalizeToE164(recipient);
            if (string.IsNullOrEmpty(normalizedNumber))
            {
                _logger.LogError("Invalid phone number format: {PhoneNumber}", recipient);
                return false;
            }

            // Get auth token from credential provider
            var authToken = await _credentialProvider.RetrieveAsync(_settings.AuthTokenKeyName, cancellationToken);
            if (string.IsNullOrEmpty(authToken))
            {
                _logger.LogError("Twilio auth token not found in credential provider");
                return false;
            }

            // Initialize Twilio client
            if (string.IsNullOrEmpty(_settings.TwilioAccountSid))
            {
                _logger.LogError("Twilio Account SID not configured");
                return false;
            }

            TwilioClient.Init(_settings.TwilioAccountSid, authToken);

            // Send SMS message
            var smsMessage = await MessageResource.CreateAsync(
                body: message,
                from: new PhoneNumber(_settings.FromPhoneNumber),
                to: new PhoneNumber(normalizedNumber)
            );

            _logger.LogInformation(
                "SMS sent successfully to {PhoneNumber}. Twilio SID: {MessageSid}",
                normalizedNumber,
                smsMessage.Sid);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error sending SMS to {PhoneNumber}: {ErrorMessage}",
                recipient,
                ex.Message);
            return false;
        }
    }

    /// <summary>
    /// Sends SMS to multiple recipients in parallel
    /// Returns dictionary mapping each recipient to success/failure
    /// </summary>
    public async Task<Dictionary<string, bool>> SendBatchAsync(
        IList<string> recipients,
        string message,
        CancellationToken cancellationToken = default)
    {
        var results = new Dictionary<string, bool>();

        if (recipients == null || recipients.Count == 0)
            return results;

        // Validate message length once
        if (message.Length > _settings.MaxMessageLength)
        {
            _logger.LogError(
                "SMS message exceeds {MaxLength} character limit: {MessageLength} characters",
                _settings.MaxMessageLength,
                message.Length);

            foreach (var recipient in recipients)
            {
                results[recipient] = false;
            }
            return results;
        }

        // Send to each recipient in parallel
        var sendTasks = recipients.Select(async recipient =>
        {
            var success = await SendAsync(recipient, message, cancellationToken);
            results[recipient] = success;
        });

        await Task.WhenAll(sendTasks);
        return results;
    }
}
