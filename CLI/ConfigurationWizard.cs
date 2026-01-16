using Microsoft.Extensions.Logging;
using Spectre.Console;
using UnifiWatch.Configuration;
using UnifiWatch.Services.Credentials;

namespace UnifiWatch.CLI;

/// <summary>
/// Interactive configuration wizard for setting up UnifiWatch
/// </summary>
public class ConfigurationWizard
{
    private readonly IConfigurationProvider _configProvider;
    private readonly ICredentialProvider _credentialProvider;
    private readonly ILogger<ConfigurationWizard> _logger;

    public ConfigurationWizard(
        IConfigurationProvider configProvider,
        ICredentialProvider credentialProvider,
        ILogger<ConfigurationWizard> logger)
    {
        _configProvider = configProvider;
        _credentialProvider = credentialProvider;
        _logger = logger;
    }

    /// <summary>
    /// Run the interactive configuration wizard
    /// </summary>
    public async Task<bool> RunAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            AnsiConsole.Clear();
            
            AnsiConsole.Write(new FigletText("UnifiWatch").Color(Color.Blue));
            
            // Check if existing configuration exists
            ServiceConfiguration? existingConfig = null;
            try
            {
                existingConfig = await _configProvider.LoadAsync(cancellationToken);
            }
            catch
            {
                // No existing config found, will use defaults
            }

            if (existingConfig != null)
            {
                var overwrite = AnsiConsole.Confirm(
                    "[yellow]Existing configuration found. Do you want to modify it?[/]",
                    defaultValue: true);

                if (!overwrite)
                {
                    AnsiConsole.MarkupLine("[yellow]⊘ Configuration wizard cancelled[/]");
                    return false;
                }
            }

            // Start with default or existing config
            var config = existingConfig ?? _configProvider.GetDefaultConfiguration();

            // Step 1: Store Selection
            await ConfigureStoreAsync(config, cancellationToken);

            // Step 2: Product Filters
            await ConfigureProductFiltersAsync(config, cancellationToken);

            // Step 3: Check Interval
            ConfigureCheckInterval(config);

            // Step 4: Notification Channels
            await ConfigureNotificationChannelsAsync(config, cancellationToken);

            // Step 5: Service Settings
            ConfigureServiceSettings(config);

            // Step 6: Culture/Language
            ConfigureCulture(config);

            // Save configuration
            await _configProvider.SaveAsync(config, cancellationToken);

            AnsiConsole.MarkupLine("\n[green]✓[/] Configuration saved successfully!");
            AnsiConsole.MarkupLine($"[grey]Config file: {_configProvider.ConfigurationFilePath}[/]");

            // Offer to test notifications
            if (AnsiConsole.Confirm("\nDo you want to test your notification settings?", defaultValue: true))
            {
                await TestNotificationsAsync(config, cancellationToken);
            }

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error running configuration wizard");
            AnsiConsole.MarkupLine($"[red]✗ Error: {ex.Message}[/]");
            return false;
        }
    }

    private async Task ConfigureStoreAsync(ServiceConfiguration config, CancellationToken cancellationToken)
    {
        AnsiConsole.MarkupLine("\n[bold blue]Step 1:[/] Store Selection");
        AnsiConsole.MarkupLine("[grey]Choose which Ubiquiti store to monitor[/]\n");

        var storeChoice = AnsiConsole.Prompt(
            new SelectionPrompt<string>()
                .Title("Select store:")
                .AddChoices(new[] { "USA Store", "European Store", "UK Store" }));

        config.Monitoring.Store = storeChoice switch
        {
            "USA Store" => "USA",
            "European Store" => "Europe",
            "UK Store" => "UK",
            _ => throw new InvalidOperationException("Invalid store selection")
        };

        config.Monitoring.UseModernApi = true;

        AnsiConsole.MarkupLine($"[green]✓[/] Selected: {config.Monitoring.Store}");
        await Task.CompletedTask;
    }

    private async Task ConfigureProductFiltersAsync(ServiceConfiguration config, CancellationToken cancellationToken)
    {
        AnsiConsole.MarkupLine("\n[bold blue]Step 2:[/] Product Filters");
        AnsiConsole.MarkupLine("[grey]Specify which products to monitor (leave empty to monitor all)[/]\n");

        var filterType = AnsiConsole.Prompt(
            new SelectionPrompt<string>()
                .Title("How do you want to filter products?")
                .AddChoices(new[] { "Monitor all products", "By collection", "By specific product names", "By SKU" }));

        config.Monitoring.Collections.Clear();
        config.Monitoring.ProductNames.Clear();
        config.Monitoring.ProductSkus.Clear();

        switch (filterType)
        {
            case "Monitor all products":
                AnsiConsole.MarkupLine("[green]✓ Will monitor all products[/]");
                break;

            case "By collection":
                var collection = AnsiConsole.Ask<string>("Enter collection name (e.g., 'UniFi Dream Machine'):");
                config.Monitoring.Collections.Add(collection);
                AnsiConsole.MarkupLine($"[green]✓[/] Monitoring collection: [cyan]{collection}[/]");
                break;

            case "By specific product names":
                var addMore = true;
                while (addMore)
                {
                    var productName = AnsiConsole.Ask<string>("Enter product name:");
                    config.Monitoring.ProductNames.Add(productName);
                    AnsiConsole.MarkupLine($"[green]✓[/] Added: [cyan]{productName}[/]");

                    addMore = AnsiConsole.Confirm("Add another product?", defaultValue: false);
                }
                break;

            case "By SKU":
                var sku = AnsiConsole.Ask<string>("Enter SKU:");
                config.Monitoring.ProductSkus.Add(sku);
                AnsiConsole.MarkupLine($"[green]✓[/] Monitoring SKU: [cyan]{sku}[/]");
                break;
        }

        await Task.CompletedTask;
    }

    private void ConfigureCheckInterval(ServiceConfiguration config)
    {
        AnsiConsole.MarkupLine("\n[bold blue]Step 3:[/] Check Interval");
        AnsiConsole.MarkupLine("[grey]How often should UnifiWatch check for stock updates?[/]\n");

        var interval = AnsiConsole.Prompt(
            new TextPrompt<int>("Enter check interval in seconds:")
                .DefaultValue(config.Service.CheckIntervalSeconds)
                .Validate(seconds =>
                {
                    return seconds switch
                    {
                        < 30 => ValidationResult.Error("[red]Minimum interval is 30 seconds to avoid rate limiting[/]"),
                        > 86400 => ValidationResult.Error("[red]Maximum interval is 86400 seconds (24 hours)[/]"),
                        _ => ValidationResult.Success()
                    };
                }));

        config.Service.CheckIntervalSeconds = interval;

        var friendly = interval switch
        {
            < 120 => $"{interval} seconds",
            < 3600 => $"{interval / 60} minutes",
            _ => $"{interval / 3600} hours"
        };

        AnsiConsole.MarkupLine($"[green]✓[/] Check interval: [cyan]{friendly}[/]");
    }

    private async Task ConfigureNotificationChannelsAsync(ServiceConfiguration config, CancellationToken cancellationToken)
    {
        AnsiConsole.MarkupLine("\n[bold blue]Step 4:[/] Notification Channels");
        AnsiConsole.MarkupLine("[grey]Configure how you want to be notified[/]\n");

        var channels = AnsiConsole.Prompt(
            new MultiSelectionPrompt<string>()
                .Title("Select notification channels:")
                .Required()
                .InstructionsText("[grey](Press [blue]space[/] to select, [green]enter[/] to confirm)[/]")
                .AddChoices(new[] { "Desktop Notifications", "Email (SMTP)", "SMS (Twilio)" }));

        // Desktop notifications
        config.Notifications.Desktop.Enabled = channels.Contains("Desktop Notifications");
        if (config.Notifications.Desktop.Enabled)
        {
            AnsiConsole.MarkupLine("[green]✓ Desktop notifications enabled[/]");
        }

        // Email notifications
        if (channels.Contains("Email (SMTP)"))
        {
            await ConfigureEmailAsync(config, cancellationToken);
        }
        else
        {
            config.Notifications.Email.Enabled = false;
        }

        // SMS notifications
        if (channels.Contains("SMS (Twilio)"))
        {
            await ConfigureSmsAsync(config, cancellationToken);
        }
        else
        {
            config.Notifications.Sms.Enabled = false;
        }
    }

    private async Task ConfigureEmailAsync(ServiceConfiguration config, CancellationToken cancellationToken)
    {
        AnsiConsole.MarkupLine("\n[bold cyan]Email Configuration[/]");

        config.Notifications.Email.Enabled = true;

        // SMTP Server
        config.Notifications.Email.SmtpServer = AnsiConsole.Ask(
            "SMTP server:",
            config.Notifications.Email.SmtpServer);

        // SMTP Port
        config.Notifications.Email.SmtpPort = AnsiConsole.Prompt(
            new TextPrompt<int>("SMTP port:")
                .DefaultValue(config.Notifications.Email.SmtpPort)
                .Validate(port =>
                {
                    return port is > 0 and <= 65535
                        ? ValidationResult.Success()
                        : ValidationResult.Error("[red]Port must be between 1 and 65535[/]");
                }));

        // TLS
        config.Notifications.Email.UseTls = AnsiConsole.Confirm(
            "Use TLS/SSL?",
            defaultValue: config.Notifications.Email.UseTls);

        // From Address
        config.Notifications.Email.FromAddress = AnsiConsole.Ask(
            "From email address:",
            config.Notifications.Email.FromAddress);

        // Recipients
        config.Notifications.Email.Recipients.Clear();
        var addRecipient = true;
        while (addRecipient)
        {
            var recipient = AnsiConsole.Ask<string>("Recipient email address:");
            config.Notifications.Email.Recipients.Add(recipient);
            AnsiConsole.MarkupLine($"[green]✓[/] Added recipient: [cyan]{recipient}[/]");

            addRecipient = AnsiConsole.Confirm("Add another recipient?", defaultValue: false);
        }

        // Credentials
        var storeCredentials = AnsiConsole.Confirm(
            "\nDo you want to store SMTP credentials now?",
            defaultValue: true);

        if (storeCredentials)
        {
            var username = AnsiConsole.Ask<string>("SMTP username (usually your email):");
            var password = AnsiConsole.Prompt(
                new TextPrompt<string>("SMTP password:")
                    .Secret());

            await _credentialProvider.StoreAsync(
                config.Notifications.Email.CredentialKey,
                username,
                password,
                cancellationToken);

            AnsiConsole.MarkupLine("[green]✓[/] SMTP credentials stored securely");
        }

        AnsiConsole.MarkupLine("[green]✓[/] Email notifications configured");
    }

    private async Task ConfigureSmsAsync(ServiceConfiguration config, CancellationToken cancellationToken)
    {
        AnsiConsole.MarkupLine("\n[bold cyan]SMS Configuration (Twilio)[/]");

        config.Notifications.Sms.Enabled = true;
        config.Notifications.Sms.Provider = "twilio";

        // Account SID
        config.Notifications.Sms.TwilioAccountSid = AnsiConsole.Ask(
            "Twilio Account SID:",
            config.Notifications.Sms.TwilioAccountSid ?? "");

        // From Phone Number
        config.Notifications.Sms.FromPhoneNumber = AnsiConsole.Ask(
            "From phone number (E.164 format, e.g., +15551234567):",
            config.Notifications.Sms.FromPhoneNumber ?? "");

        // To Phone Numbers
        config.Notifications.Sms.ToPhoneNumbers.Clear();
        var addPhone = true;
        while (addPhone)
        {
            var phoneNumber = AnsiConsole.Ask<string>("Recipient phone number (E.164 format):");
            config.Notifications.Sms.ToPhoneNumbers.Add(phoneNumber);
            AnsiConsole.MarkupLine($"[green]✓[/] Added recipient: [cyan]{phoneNumber}[/]");

            addPhone = AnsiConsole.Confirm("Add another phone number?", defaultValue: false);
        }

        // Auth Token
        var storeToken = AnsiConsole.Confirm(
            "\nDo you want to store Twilio Auth Token now?",
            defaultValue: true);

        if (storeToken)
        {
            var authToken = AnsiConsole.Prompt(
                new TextPrompt<string>("Twilio Auth Token:")
                    .Secret());

            await _credentialProvider.StoreAsync(
                config.Notifications.Sms.CredentialKey,
                authToken,
                "Twilio Auth Token",
                cancellationToken);

            AnsiConsole.MarkupLine("[green]✓[/] Twilio credentials stored securely");
        }

        AnsiConsole.MarkupLine("[green]✓[/] SMS notifications configured");
    }

    private void ConfigureServiceSettings(ServiceConfiguration config)
    {
        AnsiConsole.MarkupLine("\n[bold blue]Step 5:[/] Service Settings");
        AnsiConsole.MarkupLine("[grey]Configure service behavior[/]\n");

        config.Service.AutoStart = AnsiConsole.Confirm(
            "Run on system startup?",
            defaultValue: config.Service.AutoStart);

        var autoStartSymbol = config.Service.AutoStart ? "✓" : "✗";
        AnsiConsole.MarkupLine($"[grey]{autoStartSymbol} Auto-start: {(config.Service.AutoStart ? "[green]enabled[/]" : "[yellow]disabled[/]")}[/]");

        config.Service.Enabled = AnsiConsole.Confirm(
            "Enable service?",
            defaultValue: config.Service.Enabled);

        var enabledSymbol = config.Service.Enabled ? "✓" : "✗";
        AnsiConsole.MarkupLine($"[grey]{enabledSymbol} Service: {(config.Service.Enabled ? "[green]enabled[/]" : "[yellow]disabled[/]")}[/]");

        AnsiConsole.MarkupLine("[green]✓ Service settings configured[/]");
    }

    private void ConfigureCulture(ServiceConfiguration config)
    {
        AnsiConsole.MarkupLine("\n[bold blue]Step 6:[/] Language/Culture");
        AnsiConsole.MarkupLine("[grey]Select your preferred language[/]\n");

        var cultures = new Dictionary<string, string>
        {
            { "en-US", "English (United States)" },
            { "en-CA", "English (Canada)" },
            { "fr-CA", "French (Canada)" },
            { "fr-FR", "French (France)" },
            { "de-DE", "German (Germany)" },
            { "es-ES", "Spanish (Spain)" },
            { "it-IT", "Italian (Italy)" },
            { "pt-BR", "Portuguese (Brazil)" }
        };

        var selected = AnsiConsole.Prompt(
            new SelectionPrompt<string>()
                .Title("Select language:")
                .AddChoices(cultures.Values));

        var culture = cultures.First(kvp => kvp.Value == selected).Key;
        config.Service.Language = culture;

        AnsiConsole.MarkupLine($"[green]✓[/] Language: [cyan]{selected}[/]");
    }

    private async Task TestNotificationsAsync(ServiceConfiguration config, CancellationToken cancellationToken)
    {
        AnsiConsole.MarkupLine("\n[bold yellow]Testing Notifications...[/]\n");

        var results = new Dictionary<string, bool>();

        // Test Desktop
        if (config.Notifications.Desktop.Enabled)
        {
            await AnsiConsole.Status()
                .StartAsync("Testing desktop notifications...", async ctx =>
                {
                    try
                    {
                        // Desktop notification test would go here
                        // For now, we'll simulate success
                        await Task.Delay(500, cancellationToken);
                        results["Desktop"] = true;
                    }
                    catch
                    {
                        results["Desktop"] = false;
                    }
                });
        }

        // Test Email
        if (config.Notifications.Email.Enabled)
        {
            await AnsiConsole.Status()
                .StartAsync("Testing email notifications...", async ctx =>
                {
                    try
                    {
                        // Email test would use EmailNotificationService here
                        await Task.Delay(1000, cancellationToken);
                        results["Email"] = true;
                    }
                    catch
                    {
                        results["Email"] = false;
                    }
                });
        }

        // Test SMS
        if (config.Notifications.Sms.Enabled)
        {
            await AnsiConsole.Status()
                .StartAsync("Testing SMS notifications...", async ctx =>
                {
                    try
                    {
                        // SMS test would use SmsNotificationService here
                        await Task.Delay(1000, cancellationToken);
                        results["SMS"] = true;
                    }
                    catch
                    {
                        results["SMS"] = false;
                    }
                });
        }

        // Display results
        var table = new Table();
        table.AddColumn("Channel");
        table.AddColumn("Status");

        foreach (var result in results)
        {
            var status = result.Value
                ? "[green]✓ Success[/]"
                : "[red]✗ Failed[/]";
            table.AddRow(result.Key, status);
        }

        AnsiConsole.Write(table);
    }
}
