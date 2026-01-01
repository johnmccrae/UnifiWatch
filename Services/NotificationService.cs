using System.Runtime.InteropServices;
using System;
using UnifiWatch.Services.Localization;
using UnifiWatch.Services.Notifications;
using UnifiWatch.Models;

namespace UnifiWatch.Services;

public static class NotificationService
{
    /// <summary>
    /// Shows a desktop notification. If a NotificationOrchestrator is available via DI,
    /// also triggers multi-channel notifications (email, SMS) if configured.
    /// </summary>
    public static void ShowNotification(string title, string message)
    {
        var loc = ServiceProviderHolder.GetService<ResourceLocalizer>()
                  ?? ResourceLocalizerHolder.Instance
                  ?? ResourceLocalizer.Load(System.Globalization.CultureInfo.CurrentUICulture);
        
        // Always show desktop notification
        if (OperatingSystem.IsWindows())
        {
            ShowWindowsToast(title, message);
        }
        else if (OperatingSystem.IsMacOS())
        {
            ShowMacOSNotification(title, message, loc);
        }
        else if (OperatingSystem.IsLinux())
        {
            ShowLinuxNotification(title, message, loc);
        }
    }

    /// <summary>
    /// Shows a desktop notification and triggers multi-channel notifications (email, SMS) via NotificationOrchestrator.
    /// This is the primary integration point for NotificationOrchestrator in stock monitoring workflow.
    /// </summary>
    public static async Task ShowNotificationMultiChannelAsync(
        string title, 
        string message, 
        IEnumerable<UnifiProduct>? products = null,
        string? store = null,
        CancellationToken cancellationToken = default)
    {
        // Show desktop notification first
        ShowNotification(title, message);

        // If products provided, trigger multi-channel notifications via orchestrator
        if (products != null && !string.IsNullOrEmpty(store))
        {
            try
            {
                var orchestrator = ServiceProviderHolder.GetService<NotificationOrchestrator>();
                if (orchestrator != null)
                {
                    await orchestrator.NotifyInStockAsync(products, store, cancellationToken);
                }
            }
            catch (Exception ex)
            {
                var loc = ResourceLocalizerHolder.Instance 
                    ?? ResourceLocalizer.Load(System.Globalization.CultureInfo.CurrentUICulture);
                // Log error but don't crash - desktop notification already shown
                Console.WriteLine(loc.Error("Error.NotificationFailed", ex.Message));
            }
        }
    }

    private static void ShowWindowsToast(string title, string message)
    {
        if (!OperatingSystem.IsWindows())
        {
            var loc = ResourceLocalizerHolder.Instance ?? ResourceLocalizer.Load(System.Globalization.CultureInfo.CurrentUICulture);
            Console.WriteLine(loc.Notification("Notification.ConsoleFallback", title, message));
            return;
        }

        try
        {
            // Use PowerShell to show a Windows toast notification
            // This works in both interactive and service modes (though service mode toasts may not be visible to user)
            var escapedTitle = title.Replace("'", "''").Replace("`", "``");
            var escapedMessage = message.Replace("'", "''").Replace("`", "``");
            
            var script = $@"
                [Windows.UI.Notifications.ToastNotificationManager, Windows.UI.Notifications, ContentType = WindowsRuntime] | Out-Null
                [Windows.Data.Xml.Dom.XmlDocument, Windows.Data.Xml.Dom.XmlDocument, ContentType = WindowsRuntime] | Out-Null
                
                $template = @'
                <toast>
                    <visual>
                        <binding template='ToastText02'>
                            <text id='1'>{escapedTitle}</text>
                            <text id='2'>{escapedMessage}</text>
                        </binding>
                    </visual>
                </toast>
'@
                
                $xml = New-Object Windows.Data.Xml.Dom.XmlDocument
                $xml.LoadXml($template)
                $toast = New-Object Windows.UI.Notifications.ToastNotification $xml
                [Windows.UI.Notifications.ToastNotificationManager]::CreateToastNotifier('UnifiWatch').Show($toast)
            ";

            var psi = new System.Diagnostics.ProcessStartInfo
            {
                FileName = "powershell.exe",
                Arguments = $"-NoProfile -NonInteractive -WindowStyle Hidden -Command \"{script}\"",
                CreateNoWindow = true,
                UseShellExecute = false,
                RedirectStandardError = true
            };

            using var process = System.Diagnostics.Process.Start(psi);
            process?.WaitForExit(2000); // Wait max 2 seconds
        }
        catch (Exception ex)
        {
            // Toast notifications may fail - fall back to console output
            var loc = ResourceLocalizerHolder.Instance ?? ResourceLocalizer.Load(System.Globalization.CultureInfo.CurrentUICulture);
            Console.WriteLine(loc.Notification("Notification.ConsoleFallback", title, message));
            Console.WriteLine($"Toast notification via PowerShell failed: {ex.Message}");
        }
    }

    private static void ShowMacOSNotification(string title, string message, ResourceLocalizer loc)
    {
        try
        {
            // Use osascript to display notification via AppleScript with Ubiquiti branding
            var subtitle = EscapeAppleScript(loc.Notification("Notification.MacOSSubtitle"));
            var script = $"display notification \"{EscapeAppleScript(message)}\" with title \"{EscapeAppleScript(title)}\" subtitle \"{subtitle}\"";
            var startInfo = new System.Diagnostics.ProcessStartInfo
            {
                FileName = "osascript",
                Arguments = $"-e \"{script}\"",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = System.Diagnostics.Process.Start(startInfo);
            process?.WaitForExit();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to show macOS notification: {ex.Message}");
        }
    }

    private static void ShowLinuxNotification(string title, string message, ResourceLocalizer loc)
    {
        // Try multiple notification methods in order of preference
        if (TryNotifySend(title, message)) return;
        if (TryZenity(title, message)) return;
        if (TryKDialog(title, message)) return;
        if (TryXMessage(title, message)) return;

        // Final fallback to console
        Console.WriteLine(loc.Notification("Notification.ConsoleFallback", title, message));
    }

    private static bool TryNotifySend(string title, string message)
    {
        try
        {
            var startInfo = new System.Diagnostics.ProcessStartInfo
            {
                FileName = "notify-send",
                Arguments = $"--app-name=\"UnifiWatch\" --icon=network \"{title}\" \"{message}\"",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = System.Diagnostics.Process.Start(startInfo);
            process?.WaitForExit();
            return process?.ExitCode == 0;
        }
        catch
        {
            return false;
        }
    }

    private static bool TryZenity(string title, string message)
    {
        try
        {
            var startInfo = new System.Diagnostics.ProcessStartInfo
            {
                FileName = "zenity",
                Arguments = $"--info --title=\"{title}\" --text=\"{message}\" --window-icon=network",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = System.Diagnostics.Process.Start(startInfo);
            process?.WaitForExit();
            return process?.ExitCode == 0;
        }
        catch
        {
            return false;
        }
    }

    private static bool TryKDialog(string title, string message)
    {
        try
        {
            var startInfo = new System.Diagnostics.ProcessStartInfo
            {
                FileName = "kdialog",
                Arguments = $"--msgbox \"{message}\" --title \"{title}\" --icon network",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = System.Diagnostics.Process.Start(startInfo);
            process?.WaitForExit();
            return process?.ExitCode == 0;
        }
        catch
        {
            return false;
        }
    }

    private static bool TryXMessage(string title, string message)
    {
        try
        {
            var startInfo = new System.Diagnostics.ProcessStartInfo
            {
                FileName = "xmessage",
                Arguments = $"-center \"{title}\\n\\n{message}\"",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = System.Diagnostics.Process.Start(startInfo);
            process?.WaitForExit();
            return process?.ExitCode == 0;
        }
        catch
        {
            return false;
        }
    }

    private static string EscapeAppleScript(string text)
    {
        return text.Replace("\"", "\\\"").Replace("\\", "\\\\");
    }
}
