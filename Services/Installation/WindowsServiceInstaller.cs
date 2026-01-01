using System.Diagnostics;
using Microsoft.Extensions.Logging;

namespace UnifiWatch.Services.Installation;

/// <summary>
/// Windows service installer using PowerShell and WMI
/// </summary>
public class WindowsServiceInstaller : IServiceInstaller
{
    private readonly ILogger<WindowsServiceInstaller> _logger;
    private string _serviceName = "UnifiWatch";

    public WindowsServiceInstaller(ILogger<WindowsServiceInstaller> logger)
    {
        _logger = logger;
    }

    public async Task<bool> InstallAsync(ServiceInstallOptions options, CancellationToken cancellationToken = default)
    {
        _serviceName = options.ServiceName;

        try
        {
            // Check if already installed
            var status = await GetStatusAsync(cancellationToken);
            if (status.State != ServiceState.NotInstalled)
            {
                _logger.LogWarning("Service '{Name}' is already installed", _serviceName);
                // Ensure it's started and treat as success
                await StartAsync(cancellationToken);
                return true;
            }

            // Build PowerShell command to create service
            var psScript = BuildServiceCreationScript(options);
            var result = await RunPowerShellAsync(psScript, cancellationToken);

            if (result)
            {
                _logger.LogInformation("Service '{Name}' installed successfully", _serviceName);
                // Auto-start the service after installation
                var startResult = await StartAsync(cancellationToken);
                if (!startResult)
                {
                    _logger.LogWarning("Service '{Name}' installed but failed to start", _serviceName);
                    return false; // Installation succeeded but start failed
                }
            }
            else
            {
                _logger.LogError("Failed to install service '{Name}'", _serviceName);
                throw new InvalidOperationException($"PowerShell script failed to install service '{_serviceName}'");
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception during service installation");
            throw;  // Re-throw to expose the error
        }
    }

    public async Task<bool> UninstallAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            // Stop the service first
            await StopAsync(cancellationToken);

            // Give it a moment to stop
            await Task.Delay(1000, cancellationToken);

            var psScript = $@"
$serviceName = '{_serviceName}'
$service = Get-Service -Name $serviceName -ErrorAction SilentlyContinue
if ($service) {{
    # Stop if running
    try {{ Stop-Service -Name $serviceName -Force -ErrorAction SilentlyContinue }} catch {{}}
    # Delete via sc.exe (Remove-Service not available on some PS versions)
    cmd /c sc.exe delete ""{_serviceName}""
    Write-Host ""Service '$serviceName' removed successfully""
}}
else {{
    Write-Host ""Service '$serviceName' not found""
}}
";

            var result = await RunPowerShellAsync(psScript, cancellationToken);
            if (result)
            {
                _logger.LogInformation("Service '{Name}' uninstalled successfully", _serviceName);
            }
            else
            {
                _logger.LogError("Failed to uninstall service '{Name}'", _serviceName);
            }

            return result;
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
            var psScript = $@"
$serviceName = '{_serviceName}'
$service = Get-Service -Name $serviceName -ErrorAction SilentlyContinue
if ($service) {{
    Start-Service -Name $serviceName -ErrorAction Stop
    Write-Host ""Service '$serviceName' started successfully""
}}
else {{
    throw ""Service '$serviceName' not found""
}}
";

            var result = await RunPowerShellAsync(psScript, cancellationToken);
            if (result)
            {
                _logger.LogInformation("Service '{Name}' started", _serviceName);
            }
            else
            {
                _logger.LogError("Failed to start service '{Name}'", _serviceName);
            }

            return result;
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
            var psScript = $@"
$serviceName = '{_serviceName}'
$service = Get-Service -Name $serviceName -ErrorAction SilentlyContinue
if ($service) {{
    Stop-Service -Name $serviceName -ErrorAction Stop -Force
    Write-Host ""Service '$serviceName' stopped successfully""
}}
else {{
    Write-Host ""Service '$serviceName' not found, skipping stop""
}}
";

            var result = await RunPowerShellAsync(psScript, cancellationToken);
            if (result)
            {
                _logger.LogInformation("Service '{Name}' stopped", _serviceName);
            }

            return result;
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
            var psScript = $@"
$serviceName = '{_serviceName}'
$service = Get-Service -Name $serviceName -ErrorAction SilentlyContinue
if ($service) {{
    $status = @{{
        'Name' = $service.Name
        'DisplayName' = $service.DisplayName
        'Status' = $service.Status
        'StartType' = $service.StartType
    }}
    $status | ConvertTo-Json
}}
else {{
    Write-Host 'NOT_FOUND'
}}
";

            var output = await RunPowerShellRawAsync(psScript, cancellationToken);

            if (output.Contains("NOT_FOUND"))
            {
                return new ServiceStatus
                {
                    State = ServiceState.NotInstalled,
                    Message = $"Service '{_serviceName}' is not installed"
                };
            }

            try
            {
                using var doc = System.Text.Json.JsonDocument.Parse(output);
                var root = doc.RootElement;

                var statusElement = root.GetProperty("Status");
                // Status is an integer enum: 1=Stopped, 4=Running
                var statusValue = statusElement.ValueKind == System.Text.Json.JsonValueKind.Number
                    ? statusElement.GetInt32()
                    : int.TryParse(statusElement.GetString(), out var parsed) ? parsed : -1;
                
                var startTypeElement = root.GetProperty("StartType");
                // StartType is an integer enum: 2=Automatic, 3=Manual, 4=Disabled
                var startTypeValue = startTypeElement.ValueKind == System.Text.Json.JsonValueKind.Number
                    ? startTypeElement.GetInt32()
                    : int.TryParse(startTypeElement.GetString(), out var parsed2) ? parsed2 : -1;

                var state = statusValue switch
                {
                    4 => ServiceState.Running,
                    1 => ServiceState.Stopped,
                    _ => ServiceState.Unknown
                };

                var startTypeStr = startTypeValue switch
                {
                    2 => "Automatic",
                    3 => "Manual",
                    4 => "Disabled",
                    _ => "Unknown"
                };

                var displayNameElement = root.GetProperty("DisplayName");
                var displayName = displayNameElement.ValueKind == System.Text.Json.JsonValueKind.String
                    ? displayNameElement.GetString() ?? _serviceName
                    : displayNameElement.ToString();

                return new ServiceStatus
                {
                    State = state,
                    DisplayName = displayName,
                    StartupType = startTypeStr,
                    Message = $"Service is {state}"
                };
            }
            catch (Exception parseEx)
            {
                _logger.LogError(parseEx, "Failed to parse service status from output: {Output}", output);
                return new ServiceStatus
                {
                    State = ServiceState.Unknown,
                    Message = $"Failed to parse service status: {parseEx.Message}"
                };
            }
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

    private string BuildServiceCreationScript(ServiceInstallOptions options)
    {
        var serviceName = options.ServiceName;
        var displayName = EscapeForPowerShell(options.DisplayName);
        var description = EscapeForPowerShell(options.Description);
        // Quote the executable path so services can start when the path contains spaces
        var binaryPath = $"\"{EscapeForPowerShell(options.ExecutablePath)}\" --service-mode";

        // Build sc.exe failure actions (restart) in milliseconds and include reset window (24h)
        var delayMs = Math.Max(1000, options.RestartDelaySeconds * 1000);
        var attempts = Math.Max(1, options.RestartAttemptsOnFailure);
        var actions = string.Join("/", Enumerable.Repeat($"restart/{delayMs}", attempts));
        // sc.exe expects actions first, then reset
        var recovery = $"actions= {actions} reset= 86400";

        // Build the New-Service arguments dynamically
        var serviceArgs = new List<string>
        {
            $"-Name '{serviceName}'",
            $"-DisplayName '{displayName}'",
            $"-BinaryPathName '{binaryPath}'",
            $"-StartupType {options.StartupType}"
        };

        if (options.Dependencies.Count > 0)
            serviceArgs.Add($"-Dependency @({string.Join(",", options.Dependencies.Select(d => $"'{d}'"))})");

        var serviceArgsStr = string.Join(" `\n    ", serviceArgs);

        var script = $@"
# Check if service already exists
$serviceName = '{serviceName}'
$service = Get-Service -Name $serviceName -ErrorAction SilentlyContinue
if ($service) {{
    throw ""Service '$serviceName' already exists""
}}

# Create the service
New-Service {serviceArgsStr} -ErrorAction Stop | Out-Null

# Set description
$service = Get-Service -Name $serviceName
$service | Set-Service -Description '{description}' -ErrorAction Stop

if ({(options.DelayedAutoStart ? "$true" : "$false")}) {{
    cmd /c sc.exe config ""{serviceName}"" start=delayed-auto
}}

# Configure failure recovery (restart on failure)
cmd /c sc.exe failure ""{serviceName}"" {recovery}

Write-Host ""Service created and configured successfully""
";

        return script;
    }

    private string ServiceRecoveryAction(int attempts, int delaySecs)
    {
        // For sc.exe failure: restart/delaySecs restart/delaySecs ...
        var actions = string.Join(" ", Enumerable.Range(0, attempts).Select(_ => $"restart/{delaySecs}"));
        return actions;
    }

    private string EscapeForPowerShell(string input)
    {
        return input.Replace("'", "''").Replace("\"", "`\"");
    }

    private async Task<bool> RunPowerShellAsync(string script, CancellationToken cancellationToken)
    {
        try
        {
            var output = await RunPowerShellRawAsync(script, cancellationToken);
            var hasError = string.IsNullOrEmpty(output) || output.Contains("Error", StringComparison.OrdinalIgnoreCase) || output.Contains("Exception", StringComparison.OrdinalIgnoreCase) || output.Contains("STDERR", StringComparison.OrdinalIgnoreCase);
            if (hasError && !string.IsNullOrEmpty(output))
            {
                _logger.LogError("PowerShell script error or empty output: {Output}", output);
                Console.WriteLine($"DEBUG: PowerShell output:\n{output}");
            }
            return !hasError;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception in RunPowerShellAsync");
            return false;
        }
    }

    private async Task<string> RunPowerShellRawAsync(string script, CancellationToken cancellationToken)
    {
        // Write script to a temp file and execute it instead of passing via command line
        // This avoids escaping issues with complex PowerShell scripts
        var tempFile = Path.Combine(Path.GetTempPath(), $"unifiwatch_{Guid.NewGuid()}.ps1");
        
        try
        {
            await File.WriteAllTextAsync(tempFile, script, cancellationToken);
            Console.WriteLine($"DEBUG: PowerShell script written to: {tempFile}");

            var psi = new ProcessStartInfo
            {
                FileName = "powershell.exe",
                Arguments = $"-NoProfile -ExecutionPolicy Bypass -File \"{tempFile}\"",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = Process.Start(psi) ?? throw new InvalidOperationException("Failed to start PowerShell");

            var output = await process.StandardOutput.ReadToEndAsync();
            var error = await process.StandardError.ReadToEndAsync();

            await process.WaitForExitAsync(cancellationToken);

            // Combine both output and error for diagnostics
            var combined = output + (string.IsNullOrEmpty(error) ? "" : "\nSTDERR: " + error);
            if (!string.IsNullOrEmpty(error))
            {
                _logger.LogWarning("PowerShell stderr: {Error}", error);
                Console.WriteLine($"DEBUG: PowerShell stderr: {error}");
            }
            Console.WriteLine($"DEBUG: PowerShell stdout: {output}");

            return combined;
        }
        finally
        {
            // Keep file for debugging - don't delete
            // try
            // {
            //     if (File.Exists(tempFile))
            //         File.Delete(tempFile);
            // }
            // catch { }
        }
    }

    private string EscapeForCommand(string input)
    {
        return input.Replace("\"", "\\\"").Replace("$", "\\$");
    }
}
