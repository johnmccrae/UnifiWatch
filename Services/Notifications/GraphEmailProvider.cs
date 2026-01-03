using System.Net.Http;
using System.Net.Http.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using UnifiWatch.Models;
using UnifiWatch.Services.Credentials;

namespace UnifiWatch.Services.Notifications;

/// <summary>
/// Microsoft Graph API-based email provider for Office 365/Outlook
/// Uses OAuth 2.0 authentication instead of basic auth
/// </summary>
public class GraphEmailProvider : IEmailProvider
{
    private readonly ILogger<GraphEmailProvider> _logger;
    private readonly EmailNotificationSettings _emailSettings;
    private readonly ICredentialProvider _credentialProvider;
    private readonly HttpClient _httpClient;

    public GraphEmailProvider(
        ILogger<GraphEmailProvider> logger,
        IOptions<EmailNotificationSettings> emailOptions,
        ICredentialProvider credentialProvider,
        HttpClient httpClient)
    {
        _logger = logger;
        _emailSettings = emailOptions.Value;
        _credentialProvider = credentialProvider;
        _httpClient = httpClient;
    }

    public async Task<bool> SendAsync(
        string recipient,
        string subject,
        string plainBody,
        string? htmlBody = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (!IsValidEmailAddress(recipient))
            {
                _logger.LogWarning("Invalid email address format: {Recipient}", recipient);
                return false;
            }

            // Get OAuth access token
            var token = await GetAccessTokenAsync(cancellationToken);
            if (string.IsNullOrEmpty(token))
            {
                _logger.LogError("Failed to obtain OAuth access token");
                return false;
            }

            if (string.IsNullOrWhiteSpace(_emailSettings.OAuthMailbox))
            {
                _logger.LogError("OAuth mailbox is not configured");
                return false;
            }

            // Build email message for Graph API
            var emailMessage = new
            {
                message = new
                {
                    subject,
                    body = new
                    {
                        contentType = string.IsNullOrEmpty(htmlBody) ? "text" : "html",
                        content = htmlBody ?? plainBody
                    },
                    toRecipients = new[]
                    {
                        new
                        {
                            emailAddress = new { address = recipient }
                        }
                    }
                },
                saveToSentItems = true
            };

            // Send via Graph API using the mailbox endpoint
            var mailboxEmail = _emailSettings.OAuthMailbox;
            var graphUrl = $"https://graph.microsoft.com/v1.0/users/{Uri.EscapeDataString(mailboxEmail)}/sendMail";

            var request = new HttpRequestMessage(HttpMethod.Post, graphUrl)
            {
                Content = JsonContent.Create(emailMessage)
            };
            request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

            var response = await _httpClient.SendAsync(request, cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation("Email sent successfully to {Recipient} via Graph API", recipient);
                return true;
            }

            var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
            _logger.LogError("Graph API error sending email to {Recipient}: {StatusCode} {Error}", 
                recipient, response.StatusCode, errorContent);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error sending email to {Recipient}", recipient);
            return false;
        }
    }

    public async Task<Dictionary<string, bool>> SendBatchAsync(
        List<string> recipients,
        string subject,
        string plainBody,
        string? htmlBody = null,
        CancellationToken cancellationToken = default)
    {
        var results = new Dictionary<string, bool>();

        // Acquire token once for the batch
        var token = await GetAccessTokenAsync(cancellationToken);
        if (string.IsNullOrEmpty(token))
        {
            foreach (var recipient in recipients)
            {
                results[recipient] = false;
            }
            return results;
        }

        if (string.IsNullOrWhiteSpace(_emailSettings.OAuthMailbox))
        {
            _logger.LogError("OAuth mailbox is not configured");
            foreach (var recipient in recipients)
            {
                results[recipient] = false;
            }
            return results;
        }

        foreach (var recipient in recipients)
        {
            if (!IsValidEmailAddress(recipient))
            {
                _logger.LogWarning("Invalid email address format: {Recipient}", recipient);
                results[recipient] = false;
                continue;
            }

            try
            {
                var emailMessage = new
                {
                    message = new
                    {
                        subject,
                        body = new
                        {
                            contentType = string.IsNullOrEmpty(htmlBody) ? "text" : "html",
                            content = htmlBody ?? plainBody
                        },
                        toRecipients = new[]
                        {
                            new
                            {
                                emailAddress = new { address = recipient }
                            }
                        }
                    },
                    saveToSentItems = true
                };

                var mailboxEmail = _emailSettings.OAuthMailbox;
                var graphUrl = $"https://graph.microsoft.com/v1.0/users/{Uri.EscapeDataString(mailboxEmail)}/sendMail";

                var request = new HttpRequestMessage(HttpMethod.Post, graphUrl)
                {
                    Content = System.Net.Http.Json.JsonContent.Create(emailMessage)
                };
                request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

                var response = await _httpClient.SendAsync(request, cancellationToken);

                if (response.IsSuccessStatusCode)
                {
                    results[recipient] = true;
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
                    _logger.LogError("Graph API error sending email to {Recipient}: {StatusCode} {Error}", 
                        recipient, response.StatusCode, errorContent);
                    results[recipient] = false;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error sending email to {Recipient}", recipient);
                results[recipient] = false;
            }
        }

        return results;
    }

    private async Task<string?> GetAccessTokenAsync(CancellationToken cancellationToken)
    {
        try
        {
            // Get client secret from credential provider
            var clientSecret = await _credentialProvider.RetrieveAsync(_emailSettings.OAuthCredentialKey, cancellationToken);
            if (string.IsNullOrEmpty(clientSecret))
            {
                _logger.LogError("OAuth client secret not found in credential store");
                return null;
            }

            // Request token from Azure AD
            var tokenUrl = $"https://login.microsoftonline.com/{_emailSettings.OAuthTenantId}/oauth2/v2.0/token";

            var tokenRequest = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("client_id", _emailSettings.OAuthClientId),
                new KeyValuePair<string, string>("client_secret", clientSecret),
                new KeyValuePair<string, string>("scope", "https://graph.microsoft.com/.default"),
                new KeyValuePair<string, string>("grant_type", "client_credentials")
            });

            var response = await _httpClient.PostAsync(tokenUrl, tokenRequest, cancellationToken);
            
            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogError("Failed to get OAuth token: {StatusCode} {Error}", response.StatusCode, errorContent);
                return null;
            }

            var tokenResponse = await response.Content.ReadFromJsonAsync<TokenResponse>(cancellationToken: cancellationToken);
            if (tokenResponse == null)
            {
                _logger.LogError("Failed to parse OAuth token response");
                return null;
            }
            _logger.LogDebug("OAuth token acquired, expires in {ExpiresIn} seconds", tokenResponse.ExpiresIn);
            return tokenResponse.AccessToken;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error acquiring OAuth access token");
            return null;
        }
    }

    private bool IsValidEmailAddress(string email)
    {
        try
        {
            var addr = new System.Net.Mail.MailAddress(email);
            return addr.Address == email;
        }
        catch
        {
            return false;
        }
    }

    private class TokenResponse
    {
        [System.Text.Json.Serialization.JsonPropertyName("access_token")]
        public string AccessToken { get; set; } = string.Empty;

        [System.Text.Json.Serialization.JsonPropertyName("expires_in")]
        public int ExpiresIn { get; set; }
    }
}
