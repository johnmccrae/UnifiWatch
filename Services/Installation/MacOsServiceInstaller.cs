using System.Diagnostics;
using System.Xml.Linq;
using Microsoft.Extensions.Logging;

namespace UnifiWatch.Services.Installation;

/// <summary>
/// macOS launchd service installer
/// </summary>
public class MacOsServiceInstaller : IServiceInstaller
{
    private readonly ILogger<MacOsServiceInstaller> _logger;
    private string _serviceName = "UnifiWatch";
    private string _launchAgentPath = string.Empty;

    public MacOsServiceInstaller(ILogger<MacOsServiceInstaller> logger)
    {
        _logger = logger;
    }

    public async Task<bool> InstallAsync(ServiceInstallOptions options, CancellationToken cancellationToken = default)
    {
        _serviceName = options.ServiceName;
        _launchAgentPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
            "Library/LaunchAgents",
            $"com.unifiwatch.plist"
        );

        try
        {
            // Check if already installed
            var status = await GetStatusAsync(cancellationToken);
            if (status.State != ServiceState.NotInstalled)
            {
                _logger.LogWarning("Service '{Name}' is already installed", _serviceName);
                return false;
            }

            // Create LaunchAgents directory if it doesn't exist
            var launchAgentDir = Path.GetDirectoryName(_launchAgentPath) ?? "";
            if (!Directory.Exists(launchAgentDir))
            {
                Directory.CreateDirectory(launchAgentDir);
            }

            // Generate plist file
            var plistContent = GeneratePlist(options);

            // Write plist file
            await File.WriteAllTextAsync(_launchAgentPath, plistContent, cancellationToken);

            // Load the launch agent
            if (!await RunCommandAsync("launchctl", $"load \"{_launchAgentPath}\"", cancellationToken))
            {
                _logger.LogError("Failed to load launch agent");
                return false;
            }

            _logger.LogInformation("Service '{Name}' installed and started successfully", _serviceName);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception during service installation");
            return false;
        }
    }

    public async Task<bool> UninstallAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            // Unload the launch agent
            await RunCommandAsync("launchctl", $"unload \"{_launchAgentPath}\"", cancellationToken);

            // Remove the plist file
            if (File.Exists(_launchAgentPath))
            {
                File.Delete(_launchAgentPath);
            }

            _logger.LogInformation("Service '{Name}' uninstalled successfully", _serviceName);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception during service uninstallation");
            return false;
        }
    }

    public async Task<bool> StartAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            if (await RunCommandAsync("launchctl", $"start com.unifiwatch", cancellationToken))
            {
                _logger.LogInformation("Service '{Name}' started", _serviceName);
                return true;
            }

            _logger.LogError("Failed to start service '{Name}'", _serviceName);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception starting service");
            return false;
        }
    }

    public async Task<bool> StopAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            if (await RunCommandAsync("launchctl", $"stop com.unifiwatch", cancellationToken))
            {
                _logger.LogInformation("Service '{Name}' stopped", _serviceName);
                return true;
            }

            _logger.LogWarning("Failed to stop service '{Name}'", _serviceName);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception stopping service");
            return false;
        }
    }

    public async Task<ServiceStatus> GetStatusAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            if (!File.Exists(_launchAgentPath))
            {
                return new ServiceStatus
                {
                    State = ServiceState.NotInstalled,
                    Message = $"Service '{_serviceName}' is not installed"
                };
            }

            // Check if running via launchctl list
            var output = await RunCommandRawAsync("launchctl", "list", cancellationToken);

            var isRunning = output.Contains("com.unifiwatch", StringComparison.OrdinalIgnoreCase);

            return new ServiceStatus
            {
                State = isRunning ? ServiceState.Running : ServiceState.Stopped,
                DisplayName = _serviceName,
                StartupType = "Automatic",
                Message = $"Service is {(isRunning ? "running" : "stopped")}"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception getting service status");
            return new ServiceStatus
            {
                State = ServiceState.Unknown,
                Message = $"Error: {ex.Message}"
            };
        }
    }

    private string GeneratePlist(ServiceInstallOptions options)
    {
        var dotnetPath = GetDotnetPath();

        var doc = new XDocument(
            new XDeclaration("1.0", "UTF-8", null),
            new XDocumentType("plist", "-//Apple//DTD PLIST 1.0//EN",
                "http://www.apple.com/DTDs/PropertyList-1.0.dtd", null),
            new XElement("plist",
                new XAttribute("version", "1.0"),
                new XElement("dict",
                    new XElement("key", "Label"),
                    new XElement("string", "com.unifiwatch"),

                    new XElement("key", "ProgramArguments"),
                    new XElement("array",
                        new XElement("string", dotnetPath),
                        new XElement("string", options.ExecutablePath),
                        new XElement("string", "--service-mode")
                    ),

                    new XElement("key", "RunAtLoad"),
                    new XElement("true"),

                    new XElement("key", "KeepAlive"),
                    new XElement("true"),

                    new XElement("key", "StandardOutPath"),
                    new XElement("string", Path.Combine(
                        Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                        ".unifiwatch/logs/stdout.log"
                    )),

                    new XElement("key", "StandardErrorPath"),
                    new XElement("string", Path.Combine(
                        Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                        ".unifiwatch/logs/stderr.log"
                    )),

                    new XElement("key", "WorkingDirectory"),
                    new XElement("string", Path.GetDirectoryName(options.ExecutablePath) ?? "/opt/unifiwatch"),

                    new XElement("key", "ProcessType"),
                    new XElement("string", "Background"),

                    new XElement("key", "RestartDelay"),
                    new XElement("integer", options.RestartDelaySeconds)
                )
            )
        );

        using var writer = new StringWriter();
        doc.Save(writer);
        return writer.ToString();
    }

    private async Task<bool> RunCommandAsync(string command, string arguments, CancellationToken cancellationToken)
    {
        try
        {
            var output = await RunCommandRawAsync(command, arguments, cancellationToken);
            return !output.Contains("error", StringComparison.OrdinalIgnoreCase);
        }
        catch
        {
            return false;
        }
    }

    private async Task<string> RunCommandRawAsync(string command, string arguments, CancellationToken cancellationToken)
    {
        var psi = new ProcessStartInfo
        {
            FileName = command,
            Arguments = arguments,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        using var process = Process.Start(psi) ?? throw new InvalidOperationException($"Failed to start {command}");

        var output = await process.StandardOutput.ReadToEndAsync();
        var error = await process.StandardError.ReadToEndAsync();

        await process.WaitForExitAsync(cancellationToken);

        if (!string.IsNullOrEmpty(error))
        {
            _logger.LogWarning("{Command} error: {Error}", command, error);
        }

        return output + error;
    }

    private string GetDotnetPath()
    {
        // Try to find dotnet in PATH
        var pathVar = Environment.GetEnvironmentVariable("PATH") ?? "";
        var paths = pathVar.Split(':');

        foreach (var path in paths)
        {
            var dotnetPath = Path.Combine(path, "dotnet");
            if (File.Exists(dotnetPath))
            {
                return dotnetPath;
            }
        }

        // Fallback to common location
        return "/usr/local/bin/dotnet";
    }
}
