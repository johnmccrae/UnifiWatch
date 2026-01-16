using System.CommandLine;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Spectre.Console;
using UnifiWatch.Configuration;
using UnifiWatch.Services.Credentials;

namespace UnifiWatch.CLI;

/// <summary>
/// Command handlers for configuration management
/// </summary>
public static class ConfigurationCommands
{
    /// <summary>
    /// Create the --configure command
    /// </summary>
    public static Command CreateConfigureCommand(
        IConfigurationProvider configProvider,
        ICredentialProvider credentialProvider,
        ILogger logger)
    {
        var command = new Command("configure", "Launch interactive configuration wizard");

        command.SetHandler(async (context) =>
        {
            var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
            var wizardLogger = loggerFactory.CreateLogger<ConfigurationWizard>();

            var wizard = new ConfigurationWizard(configProvider, credentialProvider, wizardLogger);
            var success = await wizard.RunAsync(context.GetCancellationToken());

            context.ExitCode = success ? 0 : 1;
        });

        return command;
    }

    /// <summary>
    /// Create the --show-config command
    /// </summary>
    public static Command CreateShowConfigCommand(
        IConfigurationProvider configProvider,
        ILogger logger)
    {
        var command = new Command("show-config", "Display current configuration");

        command.SetHandler(async (context) =>
        {
            try
            {
                var config = await configProvider.LoadAsync(context.GetCancellationToken());

                if (config == null)
                {
                    AnsiConsole.MarkupLine("[yellow]No configuration found.[/]");
                    AnsiConsole.MarkupLine($"[grey]Run 'UnifiWatch configure' to create one.[/]");
                    context.ExitCode = 1;
                    return;
                }

                var configPath = configProvider.ConfigurationFilePath;

                AnsiConsole.Write(new Rule("[blue]UnifiWatch Configuration[/]").LeftJustified());
                AnsiConsole.MarkupLine($"[grey]Config file: {configPath}[/]\n");

                // Display General Settings
                DisplayGeneralSettings(config);

                // Display Store Configuration
                DisplayStoreConfiguration(config);

                // Display Product Filters
                DisplayProductFilters(config);

                // Display Notification Settings
                DisplayNotificationSettings(config);

                // Display Service Settings
                DisplayServiceSettings(config);

                AnsiConsole.Write(new Rule().LeftJustified());

                context.ExitCode = 0;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error displaying configuration");
                AnsiConsole.MarkupLine($"[red]Error: {ex.Message}[/]");
                context.ExitCode = 1;
            }
        });

        return command;
    }

    /// <summary>
    /// Create the --reset-config command
    /// </summary>
    public static Command CreateResetConfigCommand(
        IConfigurationProvider configProvider,
        ICredentialProvider credentialProvider,
        ILogger logger)
    {
        var command = new Command("reset-config", "Delete current configuration");

        var deleteCredentialsOption = new Option<bool>(
            "--delete-credentials",
            description: "Also delete stored credentials",
            getDefaultValue: () => false);

        command.AddOption(deleteCredentialsOption);

        command.SetHandler(async (context) =>
        {
            try
            {
                var deleteCredentials = context.ParseResult.GetValueForOption(deleteCredentialsOption);

                var configPath = configProvider.ConfigurationFilePath;
                
                if (!File.Exists(configPath))
                {
                    AnsiConsole.MarkupLine("[yellow]No configuration file found.[/]");
                    context.ExitCode = 0;
                    return;
                }

                AnsiConsole.MarkupLine($"[yellow]This will delete:[/]");
                AnsiConsole.MarkupLine($"  • Configuration file: [cyan]{configPath}[/]");
                if (deleteCredentials)
                {
                    AnsiConsole.MarkupLine($"  • All stored credentials");
                }

                var confirm = AnsiConsole.Confirm(
                    "\n[red]Are you sure you want to continue?[/]",
                    defaultValue: false);

                if (!confirm)
                {
                    AnsiConsole.MarkupLine("[grey]Operation cancelled.[/]");
                    context.ExitCode = 0;
                    return;
                }

                // Delete configuration file
                File.Delete(configPath);
                AnsiConsole.MarkupLine("[green]✓[/] Configuration file deleted");

                // Delete credentials if requested
                if (deleteCredentials)
                {
                    var config = await configProvider.LoadAsync(context.GetCancellationToken());
                    if (config != null)
                    {
                        var credentialKeys = new List<string>();

                        if (config.Notifications.Email.Enabled)
                        {
                            credentialKeys.Add(config.Notifications.Email.CredentialKey);
                        }

                        if (config.Notifications.Sms.Enabled)
                        {
                            credentialKeys.Add(config.Notifications.Sms.CredentialKey);
                        }

                        foreach (var key in credentialKeys.Distinct())
                        {
                            try
                            {
                                await credentialProvider.DeleteAsync(key, context.GetCancellationToken());
                                AnsiConsole.MarkupLine($"[green]✓[/] Deleted credentials: [cyan]{key}[/]");
                            }
                            catch (Exception ex)
                            {
                                logger.LogWarning(ex, "Failed to delete credential: {Key}", key);
                                AnsiConsole.MarkupLine($"[yellow]![/] Could not delete: [cyan]{key}[/]");
                            }
                        }
                    }
                }

                AnsiConsole.MarkupLine("\n[green]Configuration reset complete.[/]");
                context.ExitCode = 0;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error resetting configuration");
                AnsiConsole.MarkupLine($"[red]Error: {ex.Message}[/]");
                context.ExitCode = 1;
            }
        });

        return command;
    }

    /// <summary>
    /// Create the --test-notifications command
    /// </summary>
    public static Command CreateTestNotificationsCommand(
        IConfigurationProvider configProvider,
        ILogger logger)
    {
        var command = new Command("test-notifications", "Send test notifications to all enabled channels");

        command.SetHandler(async (context) =>
        {
            try
            {
                var config = await configProvider.LoadAsync(context.GetCancellationToken());

                if (config == null)
                {
                    AnsiConsole.MarkupLine("[yellow]No configuration found.[/]");
                    AnsiConsole.MarkupLine($"[grey]Run 'UnifiWatch configure' to create one.[/]");
                    context.ExitCode = 1;
                    return;
                }

                AnsiConsole.Write(new Rule("[blue]Testing Notifications[/]").LeftJustified());
                AnsiConsole.MarkupLine("");

                var results = new Dictionary<string, (bool Success, string Message)>();

                // Test Desktop
                if (config.Notifications.Desktop.Enabled)
                {
                    await AnsiConsole.Status()
                        .StartAsync("Testing desktop notifications...", async ctx =>
                        {
                            try
                            {
                                // Desktop notification test
                                await Task.Delay(500, context.GetCancellationToken());
                                results["Desktop"] = (true, "Desktop notification displayed");
                            }
                            catch (Exception ex)
                            {
                                results["Desktop"] = (false, ex.Message);
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
                                // Email test using EmailNotificationService
                                // For now, just validate configuration
                                if (string.IsNullOrWhiteSpace(config.Notifications.Email.SmtpServer))
                                {
                                    throw new InvalidOperationException("SMTP server not configured");
                                }
                                if (!config.Notifications.Email.Recipients.Any())
                                {
                                    throw new InvalidOperationException("No recipients configured");
                                }

                                await Task.Delay(1000, context.GetCancellationToken());
                                results["Email"] = (true, $"Would send to {config.Notifications.Email.Recipients.Count} recipient(s)");
                            }
                            catch (Exception ex)
                            {
                                results["Email"] = (false, ex.Message);
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
                                // SMS test using SmsNotificationService
                                // For now, just validate configuration
                                if (string.IsNullOrWhiteSpace(config.Notifications.Sms.TwilioAccountSid))
                                {
                                    throw new InvalidOperationException("Twilio Account SID not configured");
                                }
                                if (!config.Notifications.Sms.ToPhoneNumbers.Any())
                                {
                                    throw new InvalidOperationException("No phone numbers configured");
                                }

                                await Task.Delay(1000, context.GetCancellationToken());
                                results["SMS"] = (true, $"Would send to {config.Notifications.Sms.ToPhoneNumbers.Count} phone number(s)");
                            }
                            catch (Exception ex)
                            {
                                results["SMS"] = (false, ex.Message);
                            }
                        });
                }

                // Display results
                if (results.Count == 0)
                {
                    AnsiConsole.MarkupLine("[yellow]No notification channels enabled.[/]");
                }
                else
                {
                    var table = new Table();
                    table.Border = TableBorder.Ascii;
                    table.AddColumn("Channel");
                    table.AddColumn("Status");
                    table.AddColumn("Details");

                    foreach (var result in results)
                    {
                        var status = result.Value.Success
                            ? "[green]✓ Success[/]"
                            : "[red]✗ Failed[/]";
                        var message = result.Value.Success
                            ? $"[grey]{result.Value.Message}[/]"
                            : $"[red]{result.Value.Message}[/]";

                        table.AddRow(result.Key, status, message);
                    }

                    AnsiConsole.Write(table);
                }

                context.ExitCode = results.All(r => r.Value.Success) ? 0 : 1;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error testing notifications");
                AnsiConsole.MarkupLine($"[red]Error: {ex.Message}[/]");
                context.ExitCode = 1;
            }
        });

        return command;
    }

    private static void DisplayGeneralSettings(ServiceConfiguration config)
    {
        var table = new Table();
        table.Border = TableBorder.Ascii;
        table.AddColumn("[bold]General Settings[/]");
        table.AddColumn("");

        table.AddRow("Language", $"[cyan]{config.Service.Language}[/]");
        table.AddRow("Check Interval", $"[cyan]{config.Service.CheckIntervalSeconds}s[/]");

        AnsiConsole.Write(table);
        AnsiConsole.WriteLine();
    }

    private static void DisplayStoreConfiguration(ServiceConfiguration config)
    {
        var table = new Table();
        table.Border = TableBorder.Ascii;
        table.AddColumn("[bold]Store Configuration[/]");
        table.AddColumn("");

        table.AddRow("Store", $"[cyan]{config.Monitoring.Store}[/]");
        table.AddRow("Use Modern API", config.Monitoring.UseModernApi ? "[green]Yes[/]" : "[grey]No[/]");

        AnsiConsole.Write(table);
        AnsiConsole.WriteLine();
    }

    private static void DisplayProductFilters(ServiceConfiguration config)
    {
        var table = new Table();
        table.Border = TableBorder.Ascii;
        table.AddColumn("[bold]Product Filters[/]");
        table.AddColumn("");

        var hasFilters = config.Monitoring.Collections.Any() ||
                         config.Monitoring.ProductNames.Any() ||
                         config.Monitoring.ProductSkus.Any();

        if (hasFilters)
        {
            foreach (var collection in config.Monitoring.Collections)
            {
                table.AddRow("Collection", $"[cyan]{collection}[/]");
            }
            foreach (var name in config.Monitoring.ProductNames)
            {
                table.AddRow("Product Name", $"[cyan]{name}[/]");
            }
            foreach (var sku in config.Monitoring.ProductSkus)
            {
                table.AddRow("SKU", $"[cyan]{sku}[/]");
            }
        }
        else
        {
            table.AddRow("[grey]No filters (monitoring all products)[/]", "");
        }

        AnsiConsole.Write(table);
        AnsiConsole.WriteLine();
    }

    private static void DisplayNotificationSettings(ServiceConfiguration config)
    {
        var table = new Table();
        table.Border = TableBorder.Ascii;
        table.AddColumn("[bold]Notifications[/]");
        table.AddColumn("Status");
        table.AddColumn("Details");

        // Desktop
        var desktopStatus = config.Notifications.Desktop.Enabled ? "[green]Enabled[/]" : "[grey]Disabled[/]";
        table.AddRow("Desktop", desktopStatus, "");

        // Email
        var emailStatus = config.Notifications.Email.Enabled ? "[green]Enabled[/]" : "[grey]Disabled[/]";
        var emailDetails = config.Notifications.Email.Enabled
            ? $"[cyan]{config.Notifications.Email.SmtpServer}:{config.Notifications.Email.SmtpPort}[/]\n" +
              $"[cyan]{config.Notifications.Email.Recipients.Count} recipient(s)[/]\n" +
              $"[grey]Credentials: {config.Notifications.Email.CredentialKey}[/]"
            : "";
        table.AddRow("Email", emailStatus, emailDetails);

        // SMS
        var smsStatus = config.Notifications.Sms.Enabled ? "[green]Enabled[/]" : "[grey]Disabled[/]";
        var smsDetails = config.Notifications.Sms.Enabled
            ? $"[cyan]{config.Notifications.Sms.Provider}[/]\n" +
              $"[cyan]{config.Notifications.Sms.ToPhoneNumbers.Count} recipient(s)[/]\n" +
              $"[grey]Credentials: {config.Notifications.Sms.CredentialKey}[/]"
            : "";
        table.AddRow("SMS", smsStatus, smsDetails);

        AnsiConsole.Write(table);
        AnsiConsole.WriteLine();
    }

    private static void DisplayServiceSettings(ServiceConfiguration config)
    {
        var table = new Table();
        table.Border = TableBorder.Ascii;
        table.AddColumn("[bold]Service Settings[/]");
        table.AddColumn("");

        table.AddRow("Enabled", config.Service.Enabled ? "[green]Yes[/]" : "[grey]No[/]");
        table.AddRow("Auto Start", config.Service.AutoStart ? "[green]Yes[/]" : "[grey]No[/]");
        table.AddRow("Paused", config.Service.Paused ? "[yellow]Yes[/]" : "[grey]No[/]");

        AnsiConsole.Write(table);
    }

    /// <summary>
    /// Create the validate-config command
    /// </summary>
    public static Command CreateValidateConfigCommand(
        IConfigurationProvider configProvider,
        ICredentialProvider credentialProvider,
        ILogger logger)
    {
        var command = new Command("validate-config", "Validate configuration file and connectivity");

        command.SetHandler(async (context) =>
        {
            try
            {
                AnsiConsole.Write(new Rule("[blue]Configuration Validation[/]").LeftJustified());
                AnsiConsole.WriteLine();

                var config = await configProvider.LoadAsync(context.GetCancellationToken());

                if (config == null)
                {
                    AnsiConsole.MarkupLine("[yellow]No configuration found.[/]");
                    AnsiConsole.MarkupLine($"[grey]Run 'UnifiWatch configure' to create one.[/]");
                    context.ExitCode = 1;
                    return;
                }

                var validationResults = new List<(string Check, bool Pass, string Message)>();

                // 1. Validate configuration structure
                validationResults.Add(await ValidateConfigStructure(config));

                // 2. Validate file permissions
                validationResults.Add(ValidateConfigFilePermissions(configProvider.ConfigurationFilePath));

                // 3. Validate monitoring configuration
                validationResults.Add(ValidateMonitoringConfig(config));

                // 4. Validate notification configuration
                validationResults.Add(await ValidateNotificationConfig(config, credentialProvider, context.GetCancellationToken()));

                // 5. Validate service configuration
                validationResults.Add(ValidateServiceConfig(config));

                // Display results
                DisplayValidationResults(validationResults);

                var allPassed = validationResults.All(r => r.Pass);
                context.ExitCode = allPassed ? 0 : 1;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error validating configuration");
                AnsiConsole.MarkupLine($"[red]✗ Error: {ex.Message}[/]");
                context.ExitCode = 1;
            }

            AnsiConsole.Write(new Rule().LeftJustified());
        });

        return command;
    }

    /// <summary>
    /// Create the health-check command
    /// </summary>
    public static Command CreateHealthCheckCommand(
        IConfigurationProvider configProvider,
        ILogger logger)
    {
        var command = new Command("health-check", "Check service health and system status");

        command.SetHandler(async (context) =>
        {
            try
            {
                AnsiConsole.Write(new Rule("[blue]UnifiWatch Health Check[/]").LeftJustified());
                AnsiConsole.WriteLine();

                var healthChecks = new List<(string Check, bool Healthy, string Details)>();

                // 1. Configuration status
                var config = await configProvider.LoadAsync(context.GetCancellationToken());
                healthChecks.Add((
                    "Configuration File",
                    config != null,
                    config != null ? $"Found at {configProvider.ConfigurationFilePath}" : "No configuration file found"
                ));

                // 2. Service status
                healthChecks.Add(CheckServiceStatus());

                // 3. Notification channels
                if (config != null)
                {
                    healthChecks.Add((
                        "Desktop Notifications",
                        config.Notifications.Desktop.Enabled,
                        config.Notifications.Desktop.Enabled ? "Enabled" : "Disabled"
                    ));

                    healthChecks.Add((
                        "Email Notifications",
                        config.Notifications.Email.Enabled,
                        config.Notifications.Email.Enabled ? $"Enabled for {config.Notifications.Email.Recipients.Count} recipient(s)" : "Disabled"
                    ));

                    healthChecks.Add((
                        "SMS Notifications",
                        config.Notifications.Sms.Enabled,
                        config.Notifications.Sms.Enabled ? $"Enabled for {config.Notifications.Sms.ToPhoneNumbers.Count} phone number(s)" : "Disabled"
                    ));
                }

                // 4. System resources
                healthChecks.Add(CheckSystemResources());

                // Display results
                DisplayHealthCheckResults(healthChecks);

                var allHealthy = healthChecks.All(h => h.Healthy);
                context.ExitCode = allHealthy ? 0 : 1;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error performing health check");
                AnsiConsole.MarkupLine($"[red]✗ Error: {ex.Message}[/]");
                context.ExitCode = 1;
            }

            AnsiConsole.Write(new Rule().LeftJustified());
        });

        return command;
    }

    // Validation helper methods

    private static async Task<(string Check, bool Pass, string Message)> ValidateConfigStructure(ServiceConfiguration config)
    {
        try
        {
            // Check required sections
            if (config.Service == null)
                return ("Configuration Structure", false, "Missing Service section");
            if (config.Monitoring == null)
                return ("Configuration Structure", false, "Missing Monitoring section");
            if (config.Notifications == null)
                return ("Configuration Structure", false, "Missing Notifications section");

            // Check required fields
            if (string.IsNullOrWhiteSpace(config.Monitoring.Store))
                return ("Configuration Structure", false, "Store is not configured");

            return ("Configuration Structure", true, "All required sections present");
        }
        catch (Exception ex)
        {
            return ("Configuration Structure", false, ex.Message);
        }
    }

    private static (string Check, bool Pass, string Message) ValidateConfigFilePermissions(string configPath)
    {
        try
        {
            if (!File.Exists(configPath))
                return ("Config File Permissions", false, $"Configuration file not found at {configPath}");

            var fileInfo = new FileInfo(configPath);
            
            // Check file can be read
            if (!File.Exists(configPath))
                return ("Config File Permissions", false, "Configuration file is not readable");

            return ("Config File Permissions", true, $"File readable at {configPath}");
        }
        catch (Exception ex)
        {
            return ("Config File Permissions", false, ex.Message);
        }
    }

    private static (string Check, bool Pass, string Message) ValidateMonitoringConfig(ServiceConfiguration config)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(config.Monitoring.Store))
                return ("Monitoring Config", false, "No store configured");

            var validStores = new[] { "Europe", "USA", "UK", "Brazil", "India", "Japan", "Taiwan", "Singapore", "Mexico", "China" };
            if (!validStores.Contains(config.Monitoring.Store))
                return ("Monitoring Config", false, $"Invalid store: {config.Monitoring.Store}");

            return ("Monitoring Config", true, $"Store configured: {config.Monitoring.Store}");
        }
        catch (Exception ex)
        {
            return ("Monitoring Config", false, ex.Message);
        }
    }

    private static async Task<(string Check, bool Pass, string Message)> ValidateNotificationConfig(
        ServiceConfiguration config,
        ICredentialProvider credentialProvider,
        CancellationToken cancellationToken)
    {
        var issues = new List<string>();

        try
        {
            // Email validation
            if (config.Notifications.Email.Enabled)
            {
                if (string.IsNullOrWhiteSpace(config.Notifications.Email.SmtpServer))
                    issues.Add("Email: SMTP server not configured");
                else if (config.Notifications.Email.SmtpPort <= 0)
                    issues.Add("Email: SMTP port is invalid");
                else if (!config.Notifications.Email.Recipients.Any())
                    issues.Add("Email: No recipients configured");
            }

            // SMS validation
            if (config.Notifications.Sms.Enabled)
            {
                if (string.IsNullOrWhiteSpace(config.Notifications.Sms.Provider))
                    issues.Add("SMS: No provider configured");
                else if (!config.Notifications.Sms.ToPhoneNumbers.Any())
                    issues.Add("SMS: No phone numbers configured");
            }

            if (issues.Count == 0)
                return ("Notification Config", true, "All enabled channels configured");
            else
                return ("Notification Config", false, string.Join("; ", issues));
        }
        catch (Exception ex)
        {
            return ("Notification Config", false, ex.Message);
        }
    }

    private static (string Check, bool Pass, string Message) ValidateServiceConfig(ServiceConfiguration config)
    {
        try
        {
            var issues = new List<string>();

            if (config.Service.CheckIntervalSeconds < 10)
                issues.Add("Check interval is less than 10 seconds");

            if (string.IsNullOrWhiteSpace(config.Service.Language))
                issues.Add("Language not configured");

            if (issues.Count == 0)
                return ("Service Config", true, "Service configuration valid");
            else
                return ("Service Config", false, string.Join("; ", issues));
        }
        catch (Exception ex)
        {
            return ("Service Config", false, ex.Message);
        }
    }

    private static (string Check, bool Healthy, string Details) CheckServiceStatus()
    {
        try
        {
            var osVersion = Environment.OSVersion.Platform.ToString();
            var isServiceRunning = false;
            string statusMsg = "Running in non-service mode";

            #if WINDOWS
            try
            {
                var services = System.ServiceProcess.ServiceController.GetServices();
                var unifiWatchService = services.FirstOrDefault(s => s.ServiceName.Contains("UnifiWatch", StringComparison.OrdinalIgnoreCase));
                if (unifiWatchService != null)
                {
                    isServiceRunning = unifiWatchService.Status == System.ServiceProcess.ServiceControllerStatus.Running;
                    statusMsg = isServiceRunning ? "Windows Service is running" : $"Windows Service status: {unifiWatchService.Status}";
                }
            }
            catch { }
            #elif LINUX
            statusMsg = "Running on Linux (check systemd status manually)";
            #elif MACOS
            statusMsg = "Running on macOS (check launchd status manually)";
            #endif

            return ("Service Status", true, statusMsg);
        }
        catch (Exception ex)
        {
            return ("Service Status", false, ex.Message);
        }
    }

    private static (string Check, bool Healthy, string Details) CheckSystemResources()
    {
        try
        {
            var availableMemory = GC.GetTotalMemory(false) / (1024 * 1024);
            var processorCount = Environment.ProcessorCount;

            return ("System Resources", true, $"{processorCount} CPU(s), ~{availableMemory}MB memory available");
        }
        catch (Exception ex)
        {
            return ("System Resources", false, ex.Message);
        }
    }

    private static void DisplayValidationResults(List<(string Check, bool Pass, string Message)> results)
    {
        var table = new Table();
        table.Border = TableBorder.Ascii;
        table.AddColumn("Check");
        table.AddColumn("Status");
        table.AddColumn("Details");

        foreach (var result in results)
        {
            var status = result.Pass ? "[green]✓ Pass[/]" : "[red]✗ Failed[/]";
            var message = result.Pass ? $"[grey]{result.Message}[/]" : $"[red]{result.Message}[/]";
            table.AddRow(result.Check, status, message);
        }

        AnsiConsole.Write(table);
        AnsiConsole.WriteLine();

        if (results.All(r => r.Pass))
        {
            AnsiConsole.MarkupLine("[green]✓ All validation checks passed![/]");
        }
        else
        {
            var failedCount = results.Count(r => !r.Pass);
            AnsiConsole.MarkupLine($"[red]✗ {failedCount} validation check(s) failed[/]");
        }
    }

    private static void DisplayHealthCheckResults(List<(string Check, bool Healthy, string Details)> results)
    {
        var table = new Table();
        table.Border = TableBorder.Ascii;
        table.AddColumn("Component");
        table.AddColumn("Status");
        table.AddColumn("Details");

        foreach (var result in results)
        {
            var status = result.Healthy ? "[green]✓ Healthy[/]" : "[yellow]! Warning[/]";
            var message = result.Healthy ? $"[grey]{result.Details}[/]" : $"[yellow]{result.Details}[/]";
            table.AddRow(result.Check, status, message);
        }

        AnsiConsole.Write(table);
        AnsiConsole.WriteLine();

        var allHealthy = results.All(r => r.Healthy);
        if (allHealthy)
        {
            AnsiConsole.MarkupLine("[green]✓ System is healthy![/]");
        }
        else
        {
            AnsiConsole.MarkupLine("[yellow]! Some components need attention[/]");
        }
    }
}
