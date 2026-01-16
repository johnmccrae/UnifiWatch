using Microsoft.Extensions.Logging;
using UnifiWatch.Configuration;
using UnifiWatch.Services.Credentials;
using System.Net.Http.Json;
using System.Text;

namespace UnifiWatch.Services.Notifications.Sms;

/// <summary>
/// Twilio SMS notification provider
/// Sends SMS messages via Twilio API with automatic retry logic
/// Handles phone number validation, message length management
/// </summary>
public class TwilioSmsProvider : ISmsProvider
{
    private const string TwilioApiBaseUrl = "https://api.twilio.com/2010-04-01";
    private readonly SmsNotificationConfig _config;
    private readonly ICredentialProvider _credentialProvider;
    private readonly ILogger<TwilioSmsProvider> _logger;
    private readonly HttpClient _httpClient;

    public TwilioSmsProvider(
        SmsNotificationConfig config,
        ICredentialProvider credentialProvider,
        ILogger<TwilioSmsProvider> logger,
        HttpClient httpClient)
    {
        _config = config ?? throw new ArgumentNullException(nameof(config));
        _credentialProvider = credentialProvider ?? throw new ArgumentNullException(nameof(credentialProvider));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
    }

    public string ProviderName => "Twilio SMS";

    public int MaxMessageLength => _config.MaxMessageLength;

    public bool IsConfigured =>
        !string.IsNullOrWhiteSpace(_config.TwilioAccountSid) &&
        !string.IsNullOrWhiteSpace(_config.FromPhoneNumber) &&
        !string.IsNullOrWhiteSpace(_config.AuthTokenKeyName);

    public async Task<bool> SendAsync(string message, List<string> toPhoneNumbers, CancellationToken cancellationToken = default)
    {
        if (!IsConfigured)
        {
            _logger.LogWarning("Twilio provider not configured");
            return false;
        }

        if (string.IsNullOrWhiteSpace(message))
        {
            _logger.LogWarning("SMS message is empty");
            return false;
        }

        if (toPhoneNumbers == null || toPhoneNumbers.Count == 0)
        {
            _logger.LogWarning("No phone numbers specified for SMS");
            return false;
        }

        // Prepare message (sanitize, shorten if needed)
        var preparedMessage = SmsMessageFormatter.PrepareForSms(message, _config.AllowMessageShortening);

        if (string.IsNullOrWhiteSpace(preparedMessage))
        {
            _logger.LogWarning("SMS message became empty after sanitization");
            return false;
        }

        // Normalize phone numbers to E.164 format
        var normalizedNumbers = PhoneNumberValidator.NormalizePhoneNumbers(toPhoneNumbers);

        if (normalizedNumbers.Count == 0)
        {
            _logger.LogWarning("No valid phone numbers after normalization");
            return false;
        }

        // Log any failed normalizations
        if (normalizedNumbers.Count < toPhoneNumbers.Count)
        {
            _logger.LogWarning("Failed to normalize {0} phone number(s)", toPhoneNumbers.Count - normalizedNumbers.Count);
        }

        var maxAttempts = 3;
        var delays = new[] { 1000, 2000, 4000 };

        for (int attempt = 0; attempt < maxAttempts; attempt++)
        {
            try
            {
                var authToken = await _credentialProvider.RetrieveAsync(_config.AuthTokenKeyName, cancellationToken);
                if (string.IsNullOrEmpty(authToken))
                {
                    _logger.LogError("Twilio auth token not found in credential store");
                    return false;
                }

                var successCount = 0;
                var failureCount = 0;

                foreach (var toNumber in normalizedNumbers)
                {
                    try
                    {
                        var sent = await SendSingleSmsAsync(
                            preparedMessage,
                            toNumber,
                            _config.TwilioAccountSid,
                            authToken,
                            cancellationToken);

                        if (sent)
                            successCount++;
                        else
                            failureCount++;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to send SMS to {0}", toNumber);
                        failureCount++;
                    }
                }

                if (successCount > 0)
                {
                    _logger.LogInformation("SMS sent successfully to {0} of {1} recipients", successCount, normalizedNumbers.Count);

                    if (failureCount > 0)
                    {
                        _logger.LogWarning("SMS failed for {0} of {1} recipients", failureCount, normalizedNumbers.Count);
                    }

                    return failureCount == 0; // Only return true if all succeeded
                }

                // All failed, check if transient error
                if (attempt < maxAttempts - 1)
                {
                    _logger.LogWarning("SMS send failed, retrying in {0}ms", delays[attempt]);
                    await Task.Delay(delays[attempt], cancellationToken);
                }
            }
            catch (Exception ex)
            {
                if (attempt < maxAttempts - 1)
                {
                    _logger.LogWarning(ex, "Twilio SMS send error, retrying in {0}ms", delays[attempt]);
                    await Task.Delay(delays[attempt], cancellationToken);
                }
                else
                {
                    _logger.LogError(ex, "Twilio SMS send failed after {0} attempts", maxAttempts);
                }
            }
        }

        return false;
    }

    public async Task<bool> TestConnectionAsync(CancellationToken cancellationToken = default)
    {
        if (!IsConfigured)
        {
            _logger.LogWarning("Twilio provider not configured");
            return false;
        }

        try
        {
            var authToken = await _credentialProvider.RetrieveAsync(_config.AuthTokenKeyName, cancellationToken);
            if (string.IsNullOrEmpty(authToken))
            {
                _logger.LogError("Twilio auth token not found");
                return false;
            }

            // Test by fetching account info
            var accountUrl = $"{TwilioApiBaseUrl}/Accounts/{_config.TwilioAccountSid}";
            var request = new HttpRequestMessage(HttpMethod.Get, accountUrl);
            AddAuthHeader(request, _config.TwilioAccountSid, authToken);

            var response = await _httpClient.SendAsync(request, cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation("Twilio connection test successful");
                return true;
            }
            else
            {
                _logger.LogError("Twilio connection test failed with status {0}: {1}", response.StatusCode, await response.Content.ReadAsStringAsync(cancellationToken));
                return false;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Twilio connection test error");
            return false;
        }
    }

    private async Task<bool> SendSingleSmsAsync(
        string message,
        string toPhoneNumber,
        string accountSid,
        string authToken,
        CancellationToken cancellationToken)
    {
        var messagesUrl = $"{TwilioApiBaseUrl}/Accounts/{accountSid}/Messages.json";

        var requestContent = new FormUrlEncodedContent(new[]
        {
            new KeyValuePair<string, string>("To", toPhoneNumber),
            new KeyValuePair<string, string>("From", _config.FromPhoneNumber),
            new KeyValuePair<string, string>("Body", message)
        });

        var request = new HttpRequestMessage(HttpMethod.Post, messagesUrl)
        {
            Content = requestContent
        };

        AddAuthHeader(request, accountSid, authToken);

        var response = await _httpClient.SendAsync(request, cancellationToken);

        if (response.IsSuccessStatusCode)
        {
            _logger.LogInformation("SMS sent successfully to {0}", toPhoneNumber);
            return true;
        }
        else if (response.StatusCode == System.Net.HttpStatusCode.BadRequest)
        {
            // 400 errors are not retryable (e.g., invalid number format)
            var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
            _logger.LogError("SMS send failed (non-retryable): {0}", errorContent);
            return false;
        }
        else
        {
            var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
            _logger.LogError("SMS send failed with status {0}: {1}", response.StatusCode, errorContent);
            throw new HttpRequestException($"Twilio API error: {response.StatusCode}");
        }
    }

    private void AddAuthHeader(HttpRequestMessage request, string accountSid, string authToken)
    {
        var credentials = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{accountSid}:{authToken}"));
        request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", credentials);
    }
}
