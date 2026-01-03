using System.Net.Http.Json;
using Microsoft.Extensions.Logging;

namespace UnifiWatch.Services.Notifications;

/// <summary>
/// OAuth 2.0 token provider for Office 365/Outlook
/// Handles token acquisition and refresh
/// </summary>
public class Office365OAuthProvider
{
    private readonly ILogger<Office365OAuthProvider> _logger;
    private readonly HttpClient _httpClient;
    private readonly string _clientId;
    private readonly string _clientSecret;
    private readonly string _tenantId;
    private readonly string _refreshToken;

    public Office365OAuthProvider(
        ILogger<Office365OAuthProvider> logger,
        HttpClient httpClient,
        string clientId,
        string clientSecret,
        string tenantId,
        string refreshToken)
    {
        _logger = logger;
        _httpClient = httpClient;
        _clientId = clientId;
        _clientSecret = clientSecret;
        _tenantId = tenantId;
        _refreshToken = refreshToken;
    }

    public async Task<string?> GetAccessTokenAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var request = new HttpRequestMessage(HttpMethod.Post, 
                $"https://login.microsoftonline.com/{_tenantId}/oauth2/v2.0/token");

            var content = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("client_id", _clientId),
                new KeyValuePair<string, string>("client_secret", _clientSecret),
                new KeyValuePair<string, string>("refresh_token", _refreshToken),
                new KeyValuePair<string, string>("grant_type", "refresh_token"),
                new KeyValuePair<string, string>("scope", "https://graph.microsoft.com/.default")
            });

            request.Content = content;

            var response = await _httpClient.SendAsync(request, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogError("OAuth token refresh failed: {StatusCode} {Error}", response.StatusCode, error);
                return null;
            }

            var result = await response.Content.ReadFromJsonAsync<TokenResponse>(cancellationToken: cancellationToken);
            _logger.LogInformation("OAuth token refreshed successfully");
            return result?.AccessToken;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error acquiring OAuth token");
            return null;
        }
    }

    private class TokenResponse
    {
        public string? AccessToken { get; set; }
        public string? TokenType { get; set; }
        public int ExpiresIn { get; set; }
        public string? RefreshToken { get; set; }
    }
}
