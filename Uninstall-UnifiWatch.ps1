<#
.SYNOPSIS
    Uninstalls the UnifiWatch Windows Service
    
.DESCRIPTION
    Removes the UnifiWatch Windows Service and optionally cleans up configuration files.
    
.PARAMETER ServiceName
    Name of the Windows Service to remove (default: 'UnifiWatch')
    
.PARAMETER RemoveConfig
    If specified, also removes configuration files and state data
    
.EXAMPLE
    .\Uninstall-UnifiWatch.ps1
    
.EXAMPLE
    .\Uninstall-UnifiWatch.ps1 -RemoveConfig
    
.NOTES
    Requires Administrator privileges
#>

[CmdletBinding()]
param(
    [Parameter(Mandatory=$false)]
    [string]$ServiceName = 'UnifiWatch',
    
    [Parameter(Mandatory=$false)]
    [switch]$RemoveConfig
)

# Require Administrator
if (-not ([Security.Principal.WindowsPrincipal][Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)) {
    Write-Error "This script requires Administrator privileges. Please run as Administrator."
    exit 1
}

try {
    $service = Get-Service -Name $ServiceName -ErrorAction SilentlyContinue
    
    if (-not $service) {
        Write-Warning "Service '$ServiceName' not found."
        exit 0
    }
    
    # Stop service if running
    if ($service.Status -eq 'Running') {
        Write-Host "Stopping service '$ServiceName'..."
        Stop-Service -Name $ServiceName -Force
        Start-Sleep -Seconds 2
    }
    
    # Remove service
    Write-Host "Removing service '$ServiceName'..."
    Remove-Service -Name $ServiceName -Force
    Start-Sleep -Seconds 1
    
    # Remove config if requested
    if ($RemoveConfig) {
        $configDir = Join-Path $env:APPDATA "UnifiWatch"
        if (Test-Path $configDir) {
            Write-Host "Removing configuration directory: $configDir"
            Remove-Item -Path $configDir -Recurse -Force
        }
    }
    
    Write-Host "✓ Uninstallation completed successfully!"
    
    if (-not $RemoveConfig) {
        Write-Host ""
        Write-Host "Configuration files preserved at: $env:APPDATA\UnifiWatch"
        Write-Host "To remove configuration: .\Uninstall-UnifiWatch.ps1 -RemoveConfig"
    }
}
catch {
    Write-Error "Uninstallation failed: $_"
    exit 1
}
