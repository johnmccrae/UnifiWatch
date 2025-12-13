using System.Runtime.InteropServices;
using Microsoft.Toolkit.Uwp.Notifications;
using System;
using UnifiWatch.Services.Localization;

namespace UnifiWatch.Services;

public static class NotificationService
{
    public static void ShowNotification(string title, string message)
    {
        var loc = ServiceProviderHolder.GetService<ResourceLocalizer>()
                  ?? ResourceLocalizerHolder.Instance
                  ?? ResourceLocalizer.Load(System.Globalization.CultureInfo.CurrentUICulture);
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

    private static void ShowWindowsToast(string title, string message)
    {
        try
        {
#if WINDOWS
            // Use Microsoft.Toolkit.Uwp.Notifications for proper toast notifications
            // Using local Ubiquiti logo file
            var ubiquitiLogoPath = @"C:\Users\JohnMcCrae\Downloads\logo\ulogoubiquiti-square\Ubiquiti-logo-dark.png";

            new ToastContentBuilder()
                .AddArgument("action", "stockAlert") // Argument for handling notification clicks
                .AddText(title)
                .AddText(message)
                .AddAppLogoOverride(new Uri(ubiquitiLogoPath), ToastGenericAppLogoCrop.Circle)
                .SetToastDuration(ToastDuration.Long) // Keep notification visible longer
                .Show();
#else
            // Fallback if not compiled for Windows
            var loc = ResourceLocalizerHolder.Instance ?? ResourceLocalizer.Load(System.Globalization.CultureInfo.CurrentUICulture);
            Console.WriteLine(loc.Notification("Notification.ConsoleFallback", title, message));
#endif
        }
        catch (Exception ex)
        {
            var loc = ResourceLocalizerHolder.Instance ?? ResourceLocalizer.Load(System.Globalization.CultureInfo.CurrentUICulture);
            Console.WriteLine(loc.Notification("Notification.Failed", ex.Message));
#if WINDOWS
            // Try without logo as fallback
            try
            {
                new ToastContentBuilder()
                    .AddArgument("action", "stockAlert")
                    .AddText(title)
                    .AddText(message)
                    .SetToastDuration(ToastDuration.Long)
                    .Show();
            }
            catch
            {
                // Final fallback to console
                Console.WriteLine(loc.Notification("Notification.ConsoleFallback", title, message));
            }
#else
            // Fallback to console on non-Windows platforms
            Console.WriteLine(loc.Notification("Notification.ConsoleFallback", title, message));
#endif
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
