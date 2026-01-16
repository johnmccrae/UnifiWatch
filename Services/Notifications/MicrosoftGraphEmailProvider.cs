using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using Microsoft.Graph;
using Microsoft.Graph.Models;
using Microsoft.Graph.Me.SendMail;
using Microsoft.Kiota.Abstractions;
using Microsoft.Kiota.Abstractions.Authentication;
using UnifiWatch.Configuration;
using UnifiWatch.Services.Credentials;

namespace UnifiWatch.Services.Notifications;

/// <summary>
/// Microsoft Graph API email notification provider
/// Sends emails via Office 365, Outlook.com, and Exchange Online using OAuth2
/// Handles token refresh automatically for seamless operation
/// Uses localization for all user-facing messages
/// </summary>
public class MicrosoftGraphEmailProvider : INotificationProvider
{
    private readonly EmailNotificationConfig _config;
    private readonly ICredentialProvider _credentialProvider;
    private readonly EmailTemplateBuilder _templateBuilder;
    private readonly IStringLocalizer _localizer;
    private readonly ILogger<MicrosoftGraphEmailProvider> _logger;

    public MicrosoftGraphEmailProvider(
        EmailNotificationConfig config,
        ICredentialProvider credentialProvider,
        IStringLocalizer localizer,
        ILogger<MicrosoftGraphEmailProvider> logger)
    {
        _config = config ?? throw new ArgumentNullException(nameof(config));
        _credentialProvider = credentialProvider ?? throw new ArgumentNullException(nameof(credentialProvider));
        _localizer = localizer ?? throw new ArgumentNullException(nameof(localizer));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _templateBuilder = new EmailTemplateBuilder(localizer);
    }

    public string ProviderName => "Microsoft Graph Email";

    public bool IsConfigured =>
        !string.IsNullOrWhiteSpace(_config.TenantId) &&
        !string.IsNullOrWhiteSpace(_config.ClientId) &&
        !string.IsNullOrWhiteSpace(_config.FromAddress);

    public async Task<bool> SendAsync(NotificationMessage message, CancellationToken cancellationToken = default)
    {
        if (!IsConfigured)
        {
            _logger.LogWarning(_localizer["Microsoft Graph provider not configured"]);
            return false;
        }

        if (message.Recipients.Count == 0)
        {
            _logger.LogWarning(_localizer["No recipients specified"]);
            return false;
        }

        var maxAttempts = 3;
        var delays = new[] { 1000, 2000, 4000 };

        for (int attempt = 0; attempt < maxAttempts; attempt++)
        {
            try
            {
                var client = await CreateGraphClientAsync(cancellationToken);

                var sendMailBody = new SendMailPostRequestBody
                {
                    Message = new Message
                    {
                        Subject = message.Subject,
                        Body = new ItemBody
                        {
                            ContentType = BodyType.Html,
                            Content = message.HtmlBody ?? _templateBuilder.BuildStockAlertHtml(message.Products, message.Metadata)
                        },
                        ToRecipients = message.Recipients.Select(r => new Recipient
                        {
                            EmailAddress = new EmailAddress { Address = r }
                        }).ToList()
                    }
                };

                await client.Me.SendMail.PostAsync(sendMailBody, cancellationToken: cancellationToken);

                _logger.LogInformation(_localizer["Email sent successfully via Graph API to {0} recipients"], message.Recipients.Count);
                return true;
            }
            catch (Exception ex)
            {
                var isTransient = IsTransientError(ex);

                if (attempt < maxAttempts - 1 && isTransient)
                {
                    _logger.LogWarning(ex, _localizer["Graph API send failed, retrying in {0}ms"], delays[attempt]);
                    await Task.Delay(delays[attempt], cancellationToken);
                }
                else
                {
                    _logger.LogError(ex, _localizer["Graph API send failed after {0} attempts"], maxAttempts);
                    return false;
                }
            }
        }

        return false;
    }

    public async Task<bool> TestConnectionAsync(CancellationToken cancellationToken = default)
    {
        if (!IsConfigured)
        {
            _logger.LogWarning(_localizer["Microsoft Graph provider not configured"]);
            return false;
        }

        try
        {
            var client = await CreateGraphClientAsync(cancellationToken);
            await client.Me.GetAsync(cancellationToken: cancellationToken);

            _logger.LogInformation(_localizer["Microsoft Graph connection test successful"]);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, _localizer["Microsoft Graph connection test failed"]);
            return false;
        }
    }

    private async Task<GraphServiceClient> CreateGraphClientAsync(CancellationToken cancellationToken)
    {
        var clientSecret = await _credentialProvider.RetrieveAsync("msgraph-client-secret", cancellationToken);
        var refreshToken = await _credentialProvider.RetrieveAsync("msgraph-refresh-token", cancellationToken);

        if (string.IsNullOrEmpty(clientSecret) || string.IsNullOrEmpty(refreshToken))
        {
            throw new InvalidOperationException(_localizer["Microsoft Graph credentials not found"]);
        }

        var authProvider = new RefreshTokenAuthenticationProvider(_config.TenantId, _config.ClientId, clientSecret, refreshToken, _credentialProvider, _logger, _localizer);
        return new GraphServiceClient(authProvider);
    }

    private bool IsTransientError(Exception ex)
    {
        // Handle ODataError from Graph API
        if (ex is HttpRequestException httpEx)
        {
            // 429: Too Many Requests (throttling)
            // 503: Service Unavailable
            // 504: Gateway Timeout
            return httpEx.StatusCode == System.Net.HttpStatusCode.TooManyRequests ||
                   httpEx.StatusCode == System.Net.HttpStatusCode.ServiceUnavailable ||
                   httpEx.StatusCode == System.Net.HttpStatusCode.GatewayTimeout;
        }

        // Network timeouts are transient
        return ex is TimeoutException || ex is OperationCanceledException;
    }

    /// <summary>
    /// Custom authentication provider that handles OAuth2 token refresh
    /// </summary>
    private class RefreshTokenAuthenticationProvider : IAuthenticationProvider
    {
        private readonly string _tenantId;
        private readonly string _clientId;
        private readonly string _clientSecret;
        private readonly string _refreshToken;
        private readonly ICredentialProvider _credentialProvider;
        private readonly ILogger _logger;
        private readonly IStringLocalizer _localizer;
        private string? _cachedAccessToken;
        private DateTime _tokenExpiry = DateTime.MinValue;
        private readonly object _lockObject = new();

        public RefreshTokenAuthenticationProvider(
            string tenantId,
            string clientId,
            string clientSecret,
            string refreshToken,
            ICredentialProvider credentialProvider,
            ILogger logger,
            IStringLocalizer localizer)
        {
            _tenantId = tenantId;
            _clientId = clientId;
            _clientSecret = clientSecret;
            _refreshToken = refreshToken;
            _credentialProvider = credentialProvider;
            _logger = logger;
            _localizer = localizer;
        }

        public async Task AuthenticateRequestAsync(RequestInformation request, Dictionary<string, object>? additionalAuthenticationContext = null, CancellationToken cancellationToken = default)
        {
            var accessToken = await GetAccessTokenAsync(cancellationToken);
            request.Headers.Add("Authorization", $"Bearer {accessToken}");
        }

        private async Task<string> GetAccessTokenAsync(CancellationToken cancellationToken)
        {
            lock (_lockObject)
            {
                // Token buffer: refresh if within 5 minutes of expiry
                if (!string.IsNullOrEmpty(_cachedAccessToken) && DateTime.UtcNow < _tokenExpiry.AddMinutes(-5))
                {
                    return _cachedAccessToken;
                }
            }

            var newToken = await RefreshAccessTokenAsync(cancellationToken);

            lock (_lockObject)
            {
                _cachedAccessToken = newToken;
                _tokenExpiry = DateTime.UtcNow.AddHours(1); // Typically valid for 1 hour
            }

            return newToken;
        }

        private async Task<string> RefreshAccessTokenAsync(CancellationToken cancellationToken)
        {
            using var httpClient = new HttpClient();
            var tokenUrl = $"https://login.microsoftonline.com/{_tenantId}/oauth2/v2.0/token";

            var requestBody = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("grant_type", "refresh_token"),
                new KeyValuePair<string, string>("client_id", _clientId),
                new KeyValuePair<string, string>("client_secret", _clientSecret),
                new KeyValuePair<string, string>("refresh_token", _refreshToken),
                new KeyValuePair<string, string>("scope", "https://graph.microsoft.com/.default")
            });

            try
            {
                var response = await httpClient.PostAsync(tokenUrl, requestBody, cancellationToken);
                response.EnsureSuccessStatusCode();

                var content = await response.Content.ReadAsStringAsync(cancellationToken);
                var tokenData = System.Text.Json.JsonDocument.Parse(content);

                return tokenData.RootElement.GetProperty("access_token").GetString() ?? throw new InvalidOperationException(_localizer["Failed to extract access token"]);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, _localizer["Failed to refresh Microsoft Graph access token"]);
                throw;
            }
        }
    }
}
