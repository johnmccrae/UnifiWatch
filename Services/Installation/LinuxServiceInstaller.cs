using System.Diagnostics;
using Microsoft.Extensions.Logging;

namespace UnifiWatch.Services.Installation;

/// <summary>
/// Linux systemd service installer
/// </summary>
public class LinuxServiceInstaller : IServiceInstaller
{
    private readonly ILogger<LinuxServiceInstaller> _logger;
    private string _serviceName = "UnifiWatch";
    private string _unitFilePath = "/etc/systemd/system/unifiwatch.service";

    public LinuxServiceInstaller(ILogger<LinuxServiceInstaller> logger)
    {
        _logger = logger;
    }

    public async Task<bool> InstallAsync(ServiceInstallOptions options, CancellationToken cancellationToken = default)
    {
        _serviceName = options.ServiceName;
        _unitFilePath = $"/etc/systemd/system/{options.ServiceName.ToLowerInvariant()}.service";

        try
        {
            // Check if already installed
            var status = await GetStatusAsync(cancellationToken);
            if (status.State != ServiceState.NotInstalled)
            {
                _logger.LogWarning("Service '{Name}' is already installed", _serviceName);
                return false;
            }

            // Generate systemd unit file
            var unitContent = GenerateUnitFile(options);

            // Write unit file (requires sudo)
            if (!await WriteUnitFileAsync(unitContent, cancellationToken))
            {
                _logger.LogError("Failed to write unit file for service '{Name}'", _serviceName);
                return false;
            }

            // Reload systemd daemon
            if (!await RunCommandAsync("systemctl", "daemon-reload", cancellationToken))
            {
                _logger.LogError("Failed to reload systemd daemon");
                return false;
            }

            // Enable service
            if (!await RunCommandAsync("systemctl", $"enable {_serviceName}.service", cancellationToken))
            {
                _logger.LogWarning("Failed to enable service '{Name}' for auto-start", _serviceName);
                // Don't fail completely, service can still be started manually
            }

            // Start service
            if (!await RunCommandAsync("systemctl", $"start {_serviceName}.service", cancellationToken))
            {
                _logger.LogError("Failed to start service '{Name}'", _serviceName);
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
            // Stop the service
            await StopAsync(cancellationToken);

            // Disable service
            await RunCommandAsync("systemctl", $"disable {_serviceName}.service", cancellationToken);

            // Remove unit file (requires sudo)
            if (!await RemoveUnitFileAsync(cancellationToken))
            {
                _logger.LogWarning("Failed to remove unit file, but service stopped");
            }

            // Reload systemd daemon
            await RunCommandAsync("systemctl", "daemon-reload", cancellationToken);

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
            if (await RunCommandAsync("systemctl", $"start {_serviceName}.service", cancellationToken))
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
            if (await RunCommandAsync("systemctl", $"stop {_serviceName}.service", cancellationToken))
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
            var output = await RunCommandRawAsync("systemctl", $"is-active {_serviceName}.service", cancellationToken);

            if (output.Contains("unknown unit", StringComparison.OrdinalIgnoreCase))
            {
                return new ServiceStatus
                {
                    State = ServiceState.NotInstalled,
                    Message = $"Service '{_serviceName}' is not installed"
                };
            }

            var state = output.Trim() switch
            {
                "active" => ServiceState.Running,
                "inactive" => ServiceState.Stopped,
                _ => ServiceState.Unknown
            };

            // Get startup type
            var startupOutput = await RunCommandRawAsync("systemctl", $"is-enabled {_serviceName}.service", cancellationToken);
            var startupType = startupOutput.Trim() switch
            {
                "enabled" => "Automatic",
                "disabled" => "Manual",
                _ => "Unknown"
            };

            return new ServiceStatus
            {
                State = state,
                DisplayName = _serviceName,
                StartupType = startupType,
                Message = $"Service is {state}"
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

    private string GenerateUnitFile(ServiceInstallOptions options)
    {
        var dotnetPath = GetDotnetPath();
        var workingDirectory = Path.GetDirectoryName(options.ExecutablePath) ?? "/opt/unifiwatch";

        return $@"[Unit]
Description={options.Description}
After=network.target

[Service]
Type=simple
ExecStart={dotnetPath} {options.ExecutablePath} --service-mode
WorkingDirectory={workingDirectory}
Restart=on-failure
RestartSec={options.RestartDelaySeconds}
User={GetCurrentUser()}
StandardOutput=journal
StandardError=journal

[Install]
WantedBy=multi-user.target
";
    }

    private async Task<bool> WriteUnitFileAsync(string content, CancellationToken cancellationToken)
    {
        try
        {
            var tempFile = Path.GetTempFileName();
            await File.WriteAllTextAsync(tempFile, content, cancellationToken);

            // Use sudo to move file
            var result = await RunCommandAsync("sudo", $"cp {tempFile} {_unitFilePath}", cancellationToken);
            File.Delete(tempFile);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception writing unit file");
            return false;
        }
    }

    private async Task<bool> RemoveUnitFileAsync(CancellationToken cancellationToken)
    {
        try
        {
            return await RunCommandAsync("sudo", $"rm {_unitFilePath}", cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception removing unit file");
            return false;
        }
    }

    private async Task<bool> RunCommandAsync(string command, string arguments, CancellationToken cancellationToken)
    {
        try
        {
            var output = await RunCommandRawAsync(command, arguments, cancellationToken);
            return !output.Contains("error", StringComparison.OrdinalIgnoreCase) &&
                   !output.Contains("failed", StringComparison.OrdinalIgnoreCase);
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

        // Fallback to common locations
        return "/usr/bin/dotnet";
    }

    private string GetCurrentUser()
    {
        return Environment.UserName;
    }
}
