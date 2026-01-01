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
                _localizer.CLI("ConfigWizard.SelectStore", "Select store to monitor"),
                storeOptions,
                Array.IndexOf(storeOptions, config.Monitoring.Store)
            );
            config.Monitoring.Store = storeOptions[selectedStoreIdx];

            // Determine if using modern or legacy API
            var legacyStores = new[] { "Brazil", "India", "Japan", "Taiwan", "Singapore", "Mexico", "China" };
            config.Monitoring.UseModernApi = !legacyStores.Contains(config.Monitoring.Store);

            // Step 2: Product filters
            Console.WriteLine($"\n{_localizer.CLI("ConfigWizard.ProductFilters", "Product Filters (leave empty to monitor all)")}");
            
            var productNames = PromptForStringArray(
                _localizer.CLI("ConfigWizard.ProductNames", "Product names to monitor (comma-separated, or press Enter to skip)"),
                config.Monitoring.ProductNames
            );
            config.Monitoring.ProductNames = productNames;

            var productSkus = PromptForStringArray(
                _localizer.CLI("ConfigWizard.ProductSkus", "Product SKUs to monitor (comma-separated, or press Enter to skip)"),
                config.Monitoring.ProductSkus
            );
            config.Monitoring.ProductSkus = productSkus;

            // Step 3: Check interval
            var checkIntervalSeconds = PromptForInt(
                _localizer.CLI("ConfigWizard.CheckInterval", "Check interval (seconds, 30-3600)"),
                config.Service.CheckIntervalSeconds,
                30,
                3600
            );
            config.Service.CheckIntervalSeconds = checkIntervalSeconds;

            // Step 4: Notification channels
            Console.WriteLine($"\n{_localizer.CLI("ConfigWizard.Notifications", "Notification Configuration")}");

            var enableEmail = PromptForYesNo(
                _localizer.CLI("ConfigWizard.EnableEmail", "Enable email notifications?"),
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
                _localizer.CLI("ConfigWizard.EnableSms", "Enable SMS notifications?"),
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
                _localizer.CLI("ConfigWizard.DedupeWindow", "Notification dedupe window (minutes, 1-60)"),
                config.Notifications.DedupeMinutes,
                1,
                60
            );
            config.Notifications.DedupeMinutes = dedupeMinutes;

            // Step 6: Language/culture
            var cultures = new[] { "en-CA", "fr-CA", "fr-FR", "de-DE", "es-ES", "it-IT" };
            var cultureLookup = new Dictionary<string, string>
            {
                { "en-CA", "English (Canada)" },
                { "fr-CA", "Français (Canada)" },
                { "fr-FR", "Français (France)" },
                { "de-DE", "Deutsch" },
                { "es-ES", "Español" },
                { "it-IT", "Italiano" }
            };
            var selectedCultureIdx = PromptForChoice(
                _localizer.CLI("ConfigWizard.SelectLanguage", "Select language"),
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
        Console.WriteLine($"\n{_localizer.CLI("ConfigWizard.EmailConfig", "Email Configuration")}");

        config.Notifications.Email.SmtpServer = PromptForString(
            _localizer.CLI("ConfigWizard.SmtpServer", "SMTP server (e.g., smtp.gmail.com)"),
            config.Notifications.Email.SmtpServer
        );

        config.Notifications.Email.SmtpPort = PromptForInt(
            _localizer.CLI("ConfigWizard.SmtpPort", "SMTP port (default 587)"),
            config.Notifications.Email.SmtpPort,
            1,
            65535
        );

        config.Notifications.Email.UseTls = PromptForYesNo(
            _localizer.CLI("ConfigWizard.UseTls", "Use TLS?"),
            config.Notifications.Email.UseTls
        );

        config.Notifications.Email.FromAddress = PromptForString(
            _localizer.CLI("ConfigWizard.FromAddress", "From address (your email)"),
            config.Notifications.Email.FromAddress
        );

        var recipients = PromptForStringArray(
            _localizer.CLI("ConfigWizard.Recipients", "Recipient emails (comma-separated)"),
            config.Notifications.Email.Recipients
        );
        config.Notifications.Email.Recipients = recipients;

        // Store credentials
        var password = PromptForPassword(
            _localizer.CLI("ConfigWizard.EmailPassword", "Email password or app password")
        );

        if (!string.IsNullOrWhiteSpace(password))
        {
            var credentialKey = config.Notifications.Email.CredentialKey ?? "email-smtp";
            await _credentialProvider.StoreAsync(credentialKey, password);
            Console.WriteLine($"✓ {_localizer.CLI("ConfigWizard.CredentialStored", "Credentials stored")}");
        }

        return true;
    }

    /// <summary>
    /// Configure SMS settings interactively
    /// </summary>
    private async Task<bool> ConfigureSmsAsync(ServiceConfiguration config, CancellationToken cancellationToken)
    {
        Console.WriteLine($"\n{_localizer.CLI("ConfigWizard.SmsConfig", "SMS Configuration")}");

        var providers = new[] { "Twilio" };
        var selectedProviderIdx = PromptForChoice(
            _localizer.CLI("ConfigWizard.SmsProvider", "SMS provider"),
            providers,
            0
        );
        config.Notifications.Sms.Provider = providers[selectedProviderIdx];

        var recipients = PromptForStringArray(
            _localizer.CLI("ConfigWizard.PhoneNumbers", "Recipient phone numbers (comma-separated, E.164 format)"),
            config.Notifications.Sms.Recipients
        );
        config.Notifications.Sms.Recipients = recipients;

        // Store credentials
        var authToken = PromptForPassword(
            _localizer.CLI("ConfigWizard.AuthToken", "Auth token (Twilio Account SID or API key)")
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
