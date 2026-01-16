<#
.SYNOPSIS
    Installs UnifiWatch as a Windows Service
    
.DESCRIPTION
    Registers UnifiWatch as a Windows Service, creates necessary directories,
    sets up proper permissions, and configures auto-start behavior.
    
.PARAMETER BinaryPath
    Path to the UnifiWatch executable. If not provided, uses the current directory.
    
.PARAMETER ServiceName
    Name of the Windows Service (default: 'UnifiWatch')
    
.PARAMETER DisplayName
    Display name for the service in Services MMC (default: 'UniFi Stock Watch Service')
    
.PARAMETER StartupType
    Startup type: 'Automatic', 'Manual', 'Disabled' (default: 'Automatic')
    
.PARAMETER ConfigDirectory
    Directory for configuration files. If not provided, creates under %APPDATA%\UnifiWatch
    
.EXAMPLE
    .\Install-UnifiWatch.ps1
    
.EXAMPLE
    .\Install-UnifiWatch.ps1 -BinaryPath "C:\Program Files\UnifiWatch" -StartupType Manual
    
.NOTES
    Requires Administrator privileges
#>

[CmdletBinding()]
param(
    [Parameter(Mandatory=$false)]
    [string]$BinaryPath = (Get-Location).Path,
    
    [Parameter(Mandatory=$false)]
    [string]$ServiceName = 'UnifiWatch',
    
    [Parameter(Mandatory=$false)]
    [string]$DisplayName = 'UniFi Stock Watch Service',
    
    [Parameter(Mandatory=$false)]
    [ValidateSet('Automatic', 'Manual', 'Disabled')]
    [string]$StartupType = 'Automatic',
    
    [Parameter(Mandatory=$false)]
    [string]$ConfigDirectory
)

# Require Administrator
if (-not ([Security.Principal.WindowsPrincipal][Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)) {
    Write-Error "This script requires Administrator privileges. Please run as Administrator."
    exit 1
}

# Validate binary exists
$executablePath = Join-Path $BinaryPath "UnifiWatch.exe"
if (-not (Test-Path $executablePath)) {
    Write-Error "UnifiWatch.exe not found at: $executablePath"
    exit 1
}

# Set config directory if not provided
if ([string]::IsNullOrEmpty($ConfigDirectory)) {
    $ConfigDirectory = Join-Path $env:APPDATA "UnifiWatch"
}

try {
    # Create config directory
    if (-not (Test-Path $ConfigDirectory)) {
        Write-Host "Creating configuration directory: $ConfigDirectory"
        New-Item -ItemType Directory -Path $ConfigDirectory -Force | Out-Null
    }
    
    # Check if service already exists
    if (Get-Service -Name $ServiceName -ErrorAction SilentlyContinue) {
        Write-Host "Service '$ServiceName' already exists. Stopping and removing..."
        Stop-Service -Name $ServiceName -Force -ErrorAction SilentlyContinue
        Remove-Service -Name $ServiceName -Force
        Start-Sleep -Seconds 1
    }
    
    # Create service
    Write-Host "Creating Windows Service: $ServiceName"
    $nssm = Join-Path $BinaryPath "nssm.exe"
    
    # Check if nssm exists, if not use native New-Service
    if (Test-Path $nssm) {
        Write-Host "Using nssm service wrapper..."
        & $nssm install $ServiceName $executablePath "--service-mode"
        & $nssm set $ServiceName Description "Monitors Ubiquiti product stock availability and sends notifications"
        & $nssm set $ServiceName Start $StartupType
    } else {
        Write-Host "Using native Windows Service Manager..."
        $params = @{
            Name           = $ServiceName
            DisplayName    = $DisplayName
            BinaryPathName = "$executablePath --service-mode"
            StartupType    = $StartupType
            Description    = "Monitors Ubiquiti product stock availability and sends notifications"
        }
        New-Service @params | Out-Null
    }
    
    # Set service to run as Local System (adjust if needed)
    Write-Host "Configuring service permissions..."
    sc.exe config $ServiceName obj= "LocalSystem" | Out-Null
    sc.exe config $ServiceName type= own | Out-Null
    sc.exe config $ServiceName depend= "" | Out-Null
    
    # Grant config directory permissions to Local System
    Write-Host "Setting directory permissions..."
    $acl = Get-Acl $ConfigDirectory
    $systemIdentity = New-Object System.Security.Principal.NTAccount("SYSTEM")
    $permission = New-Object System.Security.AccessControl.FileSystemAccessRule(
        $systemIdentity,
        [System.Security.AccessControl.FileSystemRights]::FullControl,
        [System.Security.AccessControl.InheritanceFlags]::ContainerInherit -bor [System.Security.AccessControl.InheritanceFlags]::ObjectInherit,
        [System.Security.AccessControl.PropagationFlags]::None,
        [System.Security.AccessControl.AccessControlType]::Allow
    )
    $acl.AddAccessRule($permission)
    Set-Acl -Path $ConfigDirectory -AclObject $acl
    
    # Start service if startup type is Automatic
    if ($StartupType -eq 'Automatic') {
        Write-Host "Starting service..."
        Start-Service -Name $ServiceName
        Start-Sleep -Seconds 2
        
        if ((Get-Service -Name $ServiceName).Status -eq 'Running') {
            Write-Host "✓ Service started successfully!"
        } else {
            Write-Warning "Service created but failed to start. Check Event Viewer for details."
        }
    } else {
        Write-Host "Service created with '$StartupType' startup type."
        Write-Host "Start the service manually: net start $ServiceName"
    }
    
    Write-Host ""
    Write-Host "=========================================="
    Write-Host "Installation Summary"
    Write-Host "=========================================="
    Write-Host "Service Name:        $ServiceName"
    Write-Host "Display Name:        $DisplayName"
    Write-Host "Binary Path:         $executablePath"
    Write-Host "Config Directory:    $ConfigDirectory"
    Write-Host "Startup Type:        $StartupType"
    Write-Host "Status:              $(Get-Service -Name $ServiceName | Select-Object -ExpandProperty Status)"
    Write-Host ""
    Write-Host "Next steps:"
    Write-Host "1. Run: UnifiWatch configure"
    Write-Host "2. Test notifications: UnifiWatch test-notifications"
    Write-Host "3. View service: services.msc"
    Write-Host "=========================================="
    
}
catch {
    Write-Error "Installation failed: $_"
    exit 1
}
