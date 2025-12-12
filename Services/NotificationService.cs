using System.Runtime.InteropServices;
using Microsoft.Toolkit.Uwp.Notifications;
using System;

namespace UnifiWatch.Services;

public static class NotificationService
{
    public static void ShowNotification(string title, string message)
    {
        if (OperatingSystem.IsWindows())
        {
            ShowWindowsToast(title, message);
        }
        else if (OperatingSystem.IsMacOS())
        {
            ShowMacOSNotification(title, message);
        }
        else if (OperatingSystem.IsLinux())
        {
            ShowLinuxNotification(title, message);
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
            Console.WriteLine($"[Notification] {title}: {message}");
#endif
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to show Windows notification: {ex.Message}");
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
                Console.WriteLine($"[Notification] {title}: {message}");
            }
#else
            // Fallback to console on non-Windows platforms
            Console.WriteLine($"[Notification] {title}: {message}");
#endif
        }
    }

    private static void ShowMacOSNotification(string title, string message)
    {
        try
        {
            // Use osascript to display notification via AppleScript with Ubiquiti branding
            var script = $"display notification \"{EscapeAppleScript(message)}\" with title \"{EscapeAppleScript(title)}\" subtitle \"Ubiquiti Stock Alert\"";
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

    private static void ShowLinuxNotification(string title, string message)
    {
        // Try multiple notification methods in order of preference
        if (TryNotifySend(title, message)) return;
        if (TryZenity(title, message)) return;
        if (TryKDialog(title, message)) return;
        if (TryXMessage(title, message)) return;

        // Final fallback to console
        Console.WriteLine($"[Ubiquiti Stock Alert] {title}: {message}");
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
