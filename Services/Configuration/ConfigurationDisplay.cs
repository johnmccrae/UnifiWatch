using System.Text.Json;
using UnifiWatch.Configuration;
using UnifiWatch.Services.Localization;

namespace UnifiWatch.Services.Configuration;

/// <summary>
/// Utilities for displaying and validating configuration
/// </summary>
public static class ConfigurationDisplay
{
    /// <summary>
    /// Display configuration with credentials redacted
    /// </summary>
    public static void DisplayConfiguration(ServiceConfiguration config, IResourceLocalizer localizer, string? configPath = null)
    {
        Console.WriteLine($"\n{localizer.CLI("Config.Display", "=== UnifiWatch Configuration ===")}");

        if (!string.IsNullOrEmpty(configPath))
        {
            Console.WriteLine($"{localizer.CLI("Config.Path", "Configuration Path")}: {configPath}");
        }

        Console.WriteLine($"\n{localizer.CLI("Config.Service", "Service Settings:")}");
        Console.WriteLine($"  {localizer.CLI("Config.Enabled", "Enabled")}: {config.Service.Enabled}");
        Console.WriteLine($"  {localizer.CLI("Config.AutoStart", "Auto-Start")}: {config.Service.AutoStart}");
        Console.WriteLine($"  {localizer.CLI("Config.Paused", "Paused")}: {config.Service.Paused}");
        Console.WriteLine($"  {localizer.CLI("Config.CheckInterval", "Check Interval")}: {config.Service.CheckIntervalSeconds}s");
        Console.WriteLine($"  {localizer.CLI("Config.Language", "Language")}: {config.Service.Language}");
        Console.WriteLine($"  {localizer.CLI("Config.TimeZone", "Time Zone")}: {config.Service.TimeZone}");

        Console.WriteLine($"\n{localizer.CLI("Config.Monitoring", "Monitoring Settings:")}");
        Console.WriteLine($"  {localizer.CLI("Config.Store", "Store")}: {config.Monitoring.Store}");
        Console.WriteLine($"  {localizer.CLI("Config.UseModernApi", "Use Modern API")}: {config.Monitoring.UseModernApi}");
        
        if (config.Monitoring.Collections?.Count > 0)
        {
            Console.WriteLine($"  {localizer.CLI("Config.Collections", "Collections")}: {string.Join(", ", config.Monitoring.Collections)}");
        }
        
        if (config.Monitoring.ProductNames?.Count > 0)
        {
            Console.WriteLine($"  {localizer.CLI("Config.ProductNames", "Product Names")}: {string.Join(", ", config.Monitoring.ProductNames)}");
        }
        
        if (config.Monitoring.ProductSkus?.Count > 0)
        {
            Console.WriteLine($"  {localizer.CLI("Config.ProductSkus", "Product SKUs")}: {string.Join(", ", config.Monitoring.ProductSkus)}");
        }

        Console.WriteLine($"\n{localizer.CLI("Config.Notifications", "Notification Settings:")}");
        Console.WriteLine($"  {localizer.CLI("Config.DedupeWindow", "Dedupe Window")}: {config.Notifications.DedupeMinutes} minutes");

        Console.WriteLine($"\n  {localizer.CLI("Config.Email", "Email:")}");
        Console.WriteLine($"    {localizer.CLI("Config.Enabled", "Enabled")}: {config.Notifications.Email.Enabled}");
        if (config.Notifications.Email.Enabled)
        {
            Console.WriteLine($"    {localizer.CLI("Config.SmtpServer", "SMTP Server")}: {config.Notifications.Email.SmtpServer}");
            Console.WriteLine($"    {localizer.CLI("Config.SmtpPort", "SMTP Port")}: {config.Notifications.Email.SmtpPort}");
            Console.WriteLine($"    {localizer.CLI("Config.UseTls", "Use TLS")}: {config.Notifications.Email.UseTls}");
            Console.WriteLine($"    {localizer.CLI("Config.FromAddress", "From Address")}: {config.Notifications.Email.FromAddress}");
            Console.WriteLine($"    {localizer.CLI("Config.Recipients", "Recipients")}: {string.Join(", ", config.Notifications.Email.Recipients ?? new List<string>())}");
            Console.WriteLine($"    {localizer.CLI("Config.Password", "Password")}: ***");
        }

        Console.WriteLine($"\n  {localizer.CLI("Config.Sms", "SMS:")}");
        Console.WriteLine($"    {localizer.CLI("Config.Enabled", "Enabled")}: {config.Notifications.Sms.Enabled}");
        if (config.Notifications.Sms.Enabled)
        {
            Console.WriteLine($"    {localizer.CLI("Config.Provider", "Provider")}: {config.Notifications.Sms.Provider}");
            Console.WriteLine($"    {localizer.CLI("Config.Recipients", "Recipients")}: {string.Join(", ", config.Notifications.Sms.Recipients ?? new List<string>())}");
            Console.WriteLine($"    {localizer.CLI("Config.AuthToken", "Auth Token")}: ***");
        }

        Console.WriteLine($"\n{localizer.CLI("Config.Credentials", "Credentials:")}");
        Console.WriteLine($"  {localizer.CLI("Config.Encrypted", "Encrypted")}: {config.Credentials.Encrypted}");
        Console.WriteLine($"  {localizer.CLI("Config.StorageMethod", "Storage Method")}: {config.Credentials.StorageMethod}");

        Console.WriteLine();
    }

    /// <summary>
    /// Validate configuration and report issues
    /// </summary>
    public static void ValidateConfiguration(ServiceConfiguration config, IResourceLocalizer localizer)
    {
        var issues = new List<string>();

        if (!config.Service.Enabled)
        {
            issues.Add(localizer.CLI("Config.Validation.ServiceDisabled", "Service is disabled"));
        }

        if (string.IsNullOrWhiteSpace(config.Monitoring.Store))
        {
            issues.Add(localizer.CLI("Config.Validation.NoStore", "No store selected for monitoring"));
        }

        if (config.Service.CheckIntervalSeconds < 30)
        {
            issues.Add(localizer.CLI("Config.Validation.CheckIntervalTooLow", "Check interval is less than 30 seconds"));
        }

        if (config.Notifications.Email.Enabled)
        {
            if (!config.Notifications.Email.IsValid())
            {
                issues.Add(localizer.CLI("Config.Validation.EmailInvalid", "Email configuration is invalid or incomplete"));
            }
        }

        if (config.Notifications.Sms.Enabled)
        {
            if (!config.Notifications.Sms.IsValid())
            {
                issues.Add(localizer.CLI("Config.Validation.SmsInvalid", "SMS configuration is invalid or incomplete"));
            }
        }

        if (issues.Count == 0)
        {
            Console.WriteLine($"✓ {localizer.CLI("Config.Validation.Valid", "Configuration is valid")}");
        }
        else
        {
            Console.WriteLine($"⚠ {localizer.CLI("Config.Validation.Issues", "Configuration issues found:")}");
            foreach (var issue in issues)
            {
                Console.WriteLine($"  • {issue}");
            }
        }
    }
}
