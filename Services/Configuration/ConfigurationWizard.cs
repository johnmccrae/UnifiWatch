using UnifiWatch.Configuration;
using UnifiWatch.Services.Credentials;
using UnifiWatch.Services.Localization;

namespace UnifiWatch.Services.Configuration;

/// <summary>
/// Interactive configuration wizard for setting up UnifiWatch
/// </summary>
public class ConfigurationWizard
{
    private readonly UnifiWatch.Configuration.IConfigurationProvider _configProvider;
    private readonly ICredentialProvider _credentialProvider;
    private readonly IResourceLocalizer _localizer;

    public ConfigurationWizard(
        UnifiWatch.Configuration.IConfigurationProvider configProvider,
        ICredentialProvider credentialProvider,
        IResourceLocalizer localizer)
    {
        _configProvider = configProvider;
        _credentialProvider = credentialProvider;
        _localizer = localizer;
    }

    /// <summary>
    /// Run interactive configuration wizard
    /// </summary>
    public async Task<bool> RunAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            Console.WriteLine($"\n{_localizer.CLI("ConfigWizard.Welcome", "Welcome to UnifiWatch Configuration")}\n");

            // Load existing config if available
            var config = await _configProvider.LoadAsync(cancellationToken) 
                         ?? _configProvider.GetDefaultConfiguration();

            // Step 1: Store selection
            var storeOptions = new[] { "USA", "Europe", "UK", "Brazil", "India", "Japan", "Taiwan", "Singapore", "Mexico", "China" };
            var selectedStoreIdx = PromptForChoice(
                _localizer.CLI("ConfigWizard.SelectStore", "Which Ubiquiti store do you want to monitor?"),
                storeOptions,
                Array.IndexOf(storeOptions, config.Monitoring.Store)
            );
            config.Monitoring.Store = storeOptions[selectedStoreIdx];

            // Determine if using modern or legacy API
            var legacyStores = new[] { "Brazil", "India", "Japan", "Taiwan", "Singapore", "Mexico", "China" };
            config.Monitoring.UseModernApi = !legacyStores.Contains(config.Monitoring.Store);

            // Step 2: Product filters
            Console.WriteLine($"\n{_localizer.CLI("ConfigWizard.ProductFilters", "Product Filters (optional - leave empty to monitor all products)")}");
            
            var productNames = PromptForStringArray(
                _localizer.CLI("ConfigWizard.ProductNames", "Product names to monitor (comma-separated, or press Enter to skip)"),
                config.Monitoring.ProductNames
            );
            config.Monitoring.ProductNames = productNames;

            var productSkus = PromptForStringArray(
                _localizer.CLI("ConfigWizard.ProductSkus", "Product SKUs to monitor (e.g., UDM-SE, comma-separated, or press Enter to skip)"),
                config.Monitoring.ProductSkus
            );
            config.Monitoring.ProductSkus = productSkus;

            // Step 3: Check interval
            var checkIntervalSeconds = PromptForInt(
                _localizer.CLI("ConfigWizard.CheckInterval", "How often to check stock (in seconds, 30-3600)"),
                config.Service.CheckIntervalSeconds,
                30,
                3600
            );
            config.Service.CheckIntervalSeconds = checkIntervalSeconds;

            // Step 4: Notification channels
            Console.WriteLine($"\n{_localizer.CLI("ConfigWizard.Notifications", "How should we notify you when stock is available?")}");

            var enableEmail = PromptForYesNo(
                _localizer.CLI("ConfigWizard.EnableEmail", "Send notifications via email?"),
                config.Notifications.Email.Enabled
            );
            config.Notifications.Email.Enabled = enableEmail;

            if (enableEmail)
            {
                if (!await ConfigureEmailAsync(config, cancellationToken))
                {
                    return false;
                }
            }

            var enableSms = PromptForYesNo(
                _localizer.CLI("ConfigWizard.EnableSms", "Send notifications via SMS text?"),
                config.Notifications.Sms.Enabled
            );
            config.Notifications.Sms.Enabled = enableSms;

            if (enableSms)
            {
                if (!await ConfigureSmsAsync(config, cancellationToken))
                {
                    return false;
                }
            }

            // Step 5: Dedupe window
            var dedupeMinutes = PromptForInt(
                _localizer.CLI("ConfigWizard.DedupeWindow", "How long to wait before sending the same notification again (in minutes, 1-60)"),
                config.Notifications.DedupeMinutes,
                1,
                60
            );
            config.Notifications.DedupeMinutes = dedupeMinutes;

            // Step 6: Language/culture
            var cultures = new[] { "en-CA", "fr-CA", "fr-FR", "de-DE", "es-ES", "it-IT", "pt-BR" };
            var cultureLookup = new Dictionary<string, string>
            {
                { "en-CA", "English (Canada)" },
                { "fr-CA", "Français (Canada)" },
                { "fr-FR", "Français (France)" },
                { "de-DE", "Deutsch" },
                { "es-ES", "Español" },
                { "it-IT", "Italiano" },
                { "pt-BR", "Português (Brasil)" }
            };
            var selectedCultureIdx = PromptForChoice(
                _localizer.CLI("ConfigWizard.SelectLanguage", "What language would you like to use?"),
                cultures.Select(c => cultureLookup[c]).ToArray(),
                Array.IndexOf(cultures, config.Service.Language ?? "en-CA")
            );
            config.Service.Language = cultures[selectedCultureIdx];

            // Save configuration
            Console.WriteLine($"\n{_localizer.CLI("ConfigWizard.Saving", "Saving configuration...")}");
            await _configProvider.SaveAsync(config, cancellationToken);
            Console.WriteLine($"✓ {_localizer.CLI("ConfigWizard.Saved", "Configuration saved successfully")}");

            // Show summary
            Console.WriteLine($"\n{_localizer.CLI("ConfigWizard.Summary", "Configuration Summary:")}");
            Console.WriteLine($"  {_localizer.CLI("ConfigWizard.Store", "Store")}: {config.Monitoring.Store}");
            Console.WriteLine($"  {_localizer.CLI("ConfigWizard.CheckIntervalLabel", "Check Interval")}: {config.Service.CheckIntervalSeconds}s");
            Console.WriteLine($"  {_localizer.CLI("ConfigWizard.EmailEnabled", "Email Enabled")}: {(config.Notifications.Email.Enabled ? "Yes" : "No")}");
            Console.WriteLine($"  {_localizer.CLI("ConfigWizard.SmsEnabled", "SMS Enabled")}: {(config.Notifications.Sms.Enabled ? "Yes" : "No")}");

            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"✗ {_localizer.CLI("ConfigWizard.Error", "Configuration error: {0}", ex.Message)}");
            return false;
        }
    }

    /// <summary>
    /// Configure email settings interactively
    /// </summary>
    private async Task<bool> ConfigureEmailAsync(ServiceConfiguration config, CancellationToken cancellationToken)
    {
        Console.WriteLine($"\n{_localizer.CLI("ConfigWizard.EmailConfig", "Email Settings")}");

        // Allow environment override for auth method
        var useOAuthEnv = Environment.GetEnvironmentVariable("UNIFIWATCH_EMAIL_USE_OAUTH");
        var useOAuthDefault = !string.IsNullOrEmpty(useOAuthEnv) && (useOAuthEnv.Equals("true", StringComparison.OrdinalIgnoreCase) || useOAuthEnv == "1");

        var authMethods = new[]
        {
            _localizer.CLI("ConfigWizard.AuthMethod.Smtp", "SMTP (username/password or app password)"),
            _localizer.CLI("ConfigWizard.AuthMethod.OAuth", "OAuth 2.0 (Microsoft Graph)")
        };

        var selectedAuthIdx = PromptForChoice(
            _localizer.CLI("ConfigWizard.AuthMethod", "Which email authentication method do you use?"),
            authMethods,
            useOAuthDefault ? 1 : 0
        );

        config.Notifications.Email.UseOAuth = selectedAuthIdx == 1;

        // Configure based on selection
        var emailConfigured = config.Notifications.Email.UseOAuth
            ? await ConfigureOAuthEmailAsync(config, cancellationToken)
            : await ConfigureSmtpEmailAsync(config, cancellationToken);

        if (!emailConfigured)
        {
            return false;
        }

        // Recipients (common)
        var recipients = PromptForStringArray(
            _localizer.CLI("ConfigWizard.Recipients", "Email addresses to send notifications to (comma-separated)"),
            config.Notifications.Email.Recipients
        );
        config.Notifications.Email.Recipients = recipients;

        return true;
    }

    private async Task<bool> ConfigureSmtpEmailAsync(ServiceConfiguration config, CancellationToken cancellationToken)
    {
        Console.WriteLine($"\n{_localizer.CLI("ConfigWizard.SmtpSettings", "SMTP Email Settings")}");

        // Env overrides
        var smtpServerEnv = Environment.GetEnvironmentVariable("UNIFIWATCH_EMAIL_SMTP_SERVER");
        var smtpPortEnv = Environment.GetEnvironmentVariable("UNIFIWATCH_EMAIL_SMTP_PORT");
        var useTlsEnv = Environment.GetEnvironmentVariable("UNIFIWATCH_EMAIL_USE_TLS");
        var fromEnv = Environment.GetEnvironmentVariable("UNIFIWATCH_EMAIL_FROM_ADDRESS");
        var credentialKeyEnv = Environment.GetEnvironmentVariable("UNIFIWATCH_EMAIL_CREDENTIAL_KEY");

        config.Notifications.Email.SmtpServer = PromptForString(
            _localizer.CLI("ConfigWizard.SmtpServer", "SMTP server address (e.g., smtp.gmail.com)"),
            smtpServerEnv ?? config.Notifications.Email.SmtpServer
        );

        var defaultPort = config.Notifications.Email.SmtpPort;
        if (!string.IsNullOrEmpty(smtpPortEnv) && int.TryParse(smtpPortEnv, out var envPort))
        {
            defaultPort = envPort;
        }

        config.Notifications.Email.SmtpPort = PromptForInt(
            _localizer.CLI("ConfigWizard.SmtpPort", "SMTP port number (usually 587 for TLS, 465 for SSL)"),
            defaultPort,
            1,
            65535
        );

        var defaultTls = config.Notifications.Email.UseTls;
        if (!string.IsNullOrEmpty(useTlsEnv))
        {
            defaultTls = useTlsEnv.Equals("true", StringComparison.OrdinalIgnoreCase) || useTlsEnv == "1";
        }

        config.Notifications.Email.UseTls = PromptForYesNo(
            _localizer.CLI("ConfigWizard.UseTls", "Use secure TLS encryption for email?"),
            defaultTls
        );

        config.Notifications.Email.FromAddress = PromptForString(
            _localizer.CLI("ConfigWizard.FromAddress", "Email address to send from (usually your email address)"),
            fromEnv ?? config.Notifications.Email.FromAddress
        );

        // Credentials
        var password = PromptForPassword(
            _localizer.CLI("ConfigWizard.EmailPassword", "Email password or app-specific password (input is hidden)")
        );

        if (!string.IsNullOrWhiteSpace(password))
        {
            var credentialKey = credentialKeyEnv ?? config.Notifications.Email.CredentialKey ?? "email-smtp";
            config.Notifications.Email.CredentialKey = credentialKey;
            await _credentialProvider.StoreAsync(credentialKey, password);
            Console.WriteLine($"✓ {_localizer.CLI("ConfigWizard.CredentialStored", "Credentials stored")}");
        }

        // Ensure UseOAuth flag is false for SMTP
        config.Notifications.Email.UseOAuth = false;
        return true;
    }

    private async Task<bool> ConfigureOAuthEmailAsync(ServiceConfiguration config, CancellationToken cancellationToken)
    {
        Console.WriteLine($"\n{_localizer.CLI("ConfigWizard.OAuthSettings", "OAuth 2.0 Email Settings (Microsoft Graph)")}");
        Console.WriteLine(_localizer.CLI("ConfigWizard.OAuthInfo", "Requires Azure AD app with Mail.Send application permission (consent granted)."));

        var tenantEnv = Environment.GetEnvironmentVariable("UNIFIWATCH_EMAIL_OAUTH_TENANT_ID");
        var clientEnv = Environment.GetEnvironmentVariable("UNIFIWATCH_EMAIL_OAUTH_CLIENT_ID");
        var mailboxEnv = Environment.GetEnvironmentVariable("UNIFIWATCH_EMAIL_OAUTH_MAILBOX");
        var credentialKeyEnv = Environment.GetEnvironmentVariable("UNIFIWATCH_EMAIL_OAUTH_CREDENTIAL_KEY");

        config.Notifications.Email.OAuthTenantId = PromptForString(
            _localizer.CLI("ConfigWizard.OAuthTenantId", "Azure AD Tenant ID (GUID)"),
            tenantEnv ?? config.Notifications.Email.OAuthTenantId
        );

        config.Notifications.Email.OAuthClientId = PromptForString(
            _localizer.CLI("ConfigWizard.OAuthClientId", "Application (Client) ID"),
            clientEnv ?? config.Notifications.Email.OAuthClientId
        );

        config.Notifications.Email.OAuthMailbox = PromptForString(
            _localizer.CLI("ConfigWizard.OAuthMailbox", "Mailbox email address (shared mailbox or service account)"),
            mailboxEnv ?? config.Notifications.Email.OAuthMailbox
        );

        var credentialKey = credentialKeyEnv ?? config.Notifications.Email.OAuthCredentialKey ?? "email-oauth";
        config.Notifications.Email.OAuthCredentialKey = credentialKey;

        // Store client secret
        var clientSecret = PromptForPassword(
            _localizer.CLI("ConfigWizard.OAuthClientSecret", "Client secret (input is hidden)")
        );

        if (!string.IsNullOrWhiteSpace(clientSecret))
        {
            await _credentialProvider.StoreAsync(credentialKey, clientSecret);
            Console.WriteLine($"✓ {_localizer.CLI("ConfigWizard.OAuthSecretStored", "OAuth client secret stored")}");
        }

        // Ensure FromAddress matches mailbox for Graph send
        config.Notifications.Email.FromAddress = config.Notifications.Email.OAuthMailbox;
        config.Notifications.Email.UseOAuth = true;
        return true;
    }

    /// <summary>
    /// Configure SMS settings interactively
    /// </summary>
    private async Task<bool> ConfigureSmsAsync(ServiceConfiguration config, CancellationToken cancellationToken)
    {
        Console.WriteLine($"\n{_localizer.CLI("ConfigWizard.SmsConfig", "SMS Text Message Settings")}");

        var providers = new[] { "Twilio" };
        var selectedProviderIdx = PromptForChoice(
            _localizer.CLI("ConfigWizard.SmsProvider", "Which SMS provider do you use?"),
            providers,
            0
        );
        config.Notifications.Sms.Provider = providers[selectedProviderIdx];

        var recipients = PromptForStringArray(
            _localizer.CLI("ConfigWizard.PhoneNumbers", "Phone numbers to send SMS to (comma-separated, include country code like +1)"),
            config.Notifications.Sms.Recipients
        );
        config.Notifications.Sms.Recipients = recipients;

        // Store credentials
        var authToken = PromptForPassword(
            _localizer.CLI("ConfigWizard.AuthToken", "Twilio Account SID or API authentication token (input is hidden)")
        );

        if (!string.IsNullOrWhiteSpace(authToken))
        {
            var credentialKey = config.Notifications.Sms.CredentialKey ?? "sms:auth-token";
            await _credentialProvider.StoreAsync(credentialKey, authToken);
            Console.WriteLine($"✓ {_localizer.CLI("ConfigWizard.CredentialStored", "Credentials stored")}");
        }

        return true;
    }

    private int PromptForChoice(string prompt, string[] options, int defaultIdx = 0)
    {
        while (true)
        {
            Console.WriteLine($"\n{prompt}:");
            for (int i = 0; i < options.Length; i++)
            {
                var marker = i == defaultIdx ? " [default]" : "";
                Console.WriteLine($"  {i + 1}. {options[i]}{marker}");
            }

            Console.Write($"Choose (1-{options.Length}, press Enter for default): ");
            var input = Console.ReadLine()?.Trim();

            if (string.IsNullOrEmpty(input))
            {
                return defaultIdx;
            }

            if (int.TryParse(input, out var choice) && choice > 0 && choice <= options.Length)
            {
                return choice - 1;
            }

            Console.WriteLine(_localizer.CLI("ConfigWizard.InvalidChoice", "Invalid choice, please try again"));
        }
    }

    private string PromptForString(string prompt, string defaultValue = "")
    {
        var defaultDisplay = string.IsNullOrWhiteSpace(defaultValue) ? "" : $" [{defaultValue}]";
        Console.Write($"{prompt}{defaultDisplay}: ");
        var input = Console.ReadLine()?.Trim();
        return string.IsNullOrWhiteSpace(input) ? defaultValue : input;
    }

    private string PromptForPassword(string prompt)
    {
        Console.Write($"{prompt}: ");
        var password = new System.Text.StringBuilder();
        while (true)
        {
            var key = Console.ReadKey(intercept: true);
            if (key.Key == ConsoleKey.Enter)
            {
                Console.WriteLine();
                break;
            }
            if (key.Key == ConsoleKey.Backspace)
            {
                if (password.Length > 0)
                {
                    password.Length--;
                }
            }
            else
            {
                password.Append(key.KeyChar);
            }
        }
        return password.ToString();
    }

    private int PromptForInt(string prompt, int defaultValue, int min, int max)
    {
        while (true)
        {
            Console.Write($"{prompt} [{defaultValue}]: ");
            var input = Console.ReadLine()?.Trim();

            if (string.IsNullOrEmpty(input))
            {
                return defaultValue;
            }

            if (int.TryParse(input, out var value) && value >= min && value <= max)
            {
                return value;
            }

            Console.WriteLine(_localizer.CLI("ConfigWizard.InvalidNumber", "Please enter a number between {0} and {1}", min, max));
        }
    }

    private bool PromptForYesNo(string prompt, bool defaultValue = false)
    {
        var defaultDisplay = defaultValue ? "(Y/n)" : "(y/N)";
        Console.Write($"{prompt} {defaultDisplay}: ");
        var input = Console.ReadLine()?.Trim().ToLowerInvariant();

        if (string.IsNullOrEmpty(input))
        {
            return defaultValue;
        }

        return input == "y" || input == "yes";
    }

    private List<string> PromptForStringArray(string prompt, List<string> defaultValues)
    {
        var defaultDisplay = defaultValues?.Count > 0 ? $" [{string.Join(", ", defaultValues)}]" : "";
        Console.Write($"{prompt}{defaultDisplay}: ");
        var input = Console.ReadLine()?.Trim();

        if (string.IsNullOrEmpty(input))
        {
            return defaultValues ?? new List<string>();
        }

        return input.Split(',')
            .Select(s => s.Trim())
            .Where(s => !string.IsNullOrEmpty(s))
            .ToList();
    }
}
