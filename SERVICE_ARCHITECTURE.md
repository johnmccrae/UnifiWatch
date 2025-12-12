# Service Mode Architecture & Implementation Plan

## Project Status

✅ **Phase 1 Complete** - Configuration & Credential Infrastructure (December 2025)
- Configuration system with JSON persistence
- Cross-platform credential storage (Windows CredMan, macOS Keychain, Linux secret-service, encrypted file fallback)
- 65 passing unit tests
- See `BUILD_PLAN.md` for detailed remaining phases

## Overview

Transform UnifiWatch into a dual-mode application:
1. **CLI Mode** (existing): `UnifiWatch --stock --store USA` 
2. **Service Mode** (in development): Background daemon that monitors stock and sends notifications

## Architecture Components

### 1. Configuration System ✅ IMPLEMENTED

**Status**: Complete with 19 passing tests

**Implementation Details**:
- **Interface**: `IConfigurationProvider` with Load/Save/Validate/Backup/Delete operations
- **Implementation**: `ConfigurationProvider` using `System.Text.Json`
- **Models**: `ServiceConfiguration`, `ServiceSettings`, `MonitoringSettings`, `NotificationSettings`
- **Validation**: Built-in validation with detailed error reporting
- **Backup**: Automatic backup on save with collision-free timestamps
- **Files**:
  - `Configuration/IConfigurationProvider.cs`
  - `Configuration/ConfigurationProvider.cs`
  - `Configuration/ServiceConfiguration.cs`

**Location by Platform:**
- Windows: `%APPDATA%\UnifiWatch\config.json`
- macOS: `~/.config/unifiwatch/config.json` (preferred) or `~/Library/Application Support/UnifiWatch/config.json`
- Linux: `~/.config/unifiwatch/config.json` (user) or `/etc/unifiwatch/config.json` (system-wide)

**Configuration Schema:**
```json
{
  "service": {
    "enabled": true,
    "autoStart": true,
    "checkIntervalSeconds": 300,
    "paused": false
  },
  "monitoring": {
    "store": "USA",
    "collections": ["dream-machine", "camera-security"],
    "productNames": ["Dream Machine", "Camera"],
    "productSkus": ["UDM-Pro"],
    "useModernApi": true
  },
  "notifications": {
    "desktop": {
      "enabled": true
    },
    "email": {
      "enabled": true,
      "recipients": ["user@example.com"],
      "smtpServer": "smtp.gmail.com",
      "smtpPort": 587,
      "useTls": true,
      "fromAddress": "alerts@example.com",
      "credentialKey": "email-smtp"
    },
    "sms": {
      "enabled": true,
      "provider": "twilio",
      "recipients": ["+15551234567"],
      "credentialKey": "twilio-api"
    }
  },
  "credentials": {
    "encrypted": true,
    "storageMethod": "windows-credential-manager"
  }
}
```

### 2. Credential Storage (OS-Specific) ✅ IMPLEMENTED

**Status**: Complete with 46 passing tests

**Implementation Details**:
- **Interface**: `ICredentialProvider` with Store/Retrieve/Delete/Exists/List operations
- **Factory**: `CredentialProviderFactory` with platform detection and "auto" selection
- **Providers Implemented**:
  - `WindowsCredentialManager` - Native Windows Credential Manager via P/Invoke
  - `MacOsKeychain` - macOS Keychain via `security` command
  - `LinuxSecretService` - Linux secret-service via D-Bus (Tmds.DBus)
  - `EncryptedFileCredentialProvider` - Cross-platform fallback with conditional compilation:
    - Windows: DPAPI (`System.Security.Cryptography.ProtectedData`)
    - Linux/macOS: AES-256-CBC encryption
  - `EnvironmentVariableCredentialProvider` - Basic fallback for CI/automation
- **Files**:
  - `Services/Credentials/ICredentialProvider.cs`
  - `Services/Credentials/CredentialProviderFactory.cs`
  - `Services/Credentials/WindowsCredentialManager.cs`
  - `Services/Credentials/MacOsKeychain.cs`
  - `Services/Credentials/LinuxSecretService.cs`
  - `Services/Credentials/EncryptedFileCredentialProvider.cs`
  - `Services/Credentials/EnvironmentVariableCredentialProvider.cs`

#### Windows: Credential Manager (CredMan)
- **API**: Windows Credential Manager via `cmdkey.exe` or direct Win32 API
- **Storage**: `HKEY_CURRENT_USER\Software\Microsoft\Credentials`
- **Implementation**: 
  - C# wrapper using P/Invoke or `CredentialManagement` NuGet package
  - Store as: `UnifiWatch:email-smtp`, `UnifiWatch:twilio-api`
  - User-friendly: credentials appear in Windows Credential Manager

#### macOS: Keychain
- **API**: Security framework via native interop or shell (`security` command)
- **Implementation**:
  - Use `security add-generic-password` command (fallback approach)
  - Or P/Invoke to macOS Security framework
  - Service: "UnifiWatch"
  - Account: "email-smtp", "twilio-api", etc.

#### Linux: Multiple Options
- **Primary (Recommended)**: `secret-service` (D-Bus Secret Service API)
  - Works with GNOME Keyring, KDE Wallet, pass
  - NuGet: `Tmds.DBus` for D-Bus communication
- **Fallback #1**: `pass` (password manager) CLI integration
- **Fallback #2**: Encrypted file with DPAPI-like encryption using OpenSSL
  - Store in `~/.config/unifiwatch/credentials.enc`
  - Show warning: "Credentials stored in encrypted file, not hardware keyring"
- **Fallback #3**: Environment variables (for headless/automation scenarios)
  - `unifiwatch_EMAIL_PASSWORD`, `unifiwatch_TWILIO_KEY`

### 3. Notification Providers

#### Multi-Channel Architecture
```
INotificationProvider (interface)
├── DesktopNotificationProvider (existing)
├── EmailNotificationProvider
├── SmsNotificationProvider (abstract)
│   ├── TwilioSmsProvider
│   ├── AwsSnsSmsProvider
│   ├── AzureCommunicationSmsProvider
│   └── SmtpGatewaySmsProvider (carrier emails)
└── CompositeNotificationProvider (sends to all enabled)
```

#### Email Notification
- **Library**: MailKit (cross-platform SMTP)
- **Format**: HTML with product table, stock status, direct link
- **Features**:
  - Dynamic template with product details
  - Retry logic (3 attempts with exponential backoff)
  - Logging of send attempts

#### SMS Notification - Multiple Providers

**Twilio**
- NuGet: `Twilio`
- Config: Account SID, Auth Token, From Phone Number
- Cost: ~$0.01 per SMS (approximate)
- Setup: [Twilio Console](https://www.twilio.com/console)

**AWS SNS**
- NuGet: `AWSSDK.SNS`
- Config: Access Key, Secret Key, Region
- Cost: ~$0.00645 per SMS in US (approximate)
- Setup: AWS Console IAM + SNS

**Azure Communication Services**
- NuGet: `Azure.Communication.Sms`
- Config: Connection String
- Cost: Variable by region, ~$0.0075 per SMS (approximate)
- Setup: Azure Portal

**SMTP Gateway (Carrier Email-to-SMS)**
- **Implementation**: Use existing email provider
- **Carriers** (US Examples):
  - T-Mobile: `<phone>@tmomail.net`
  - Verizon: `<phone>@vtext.com`
  - AT&T: `<phone>@txt.att.net`
  - Sprint: `<phone>@messaging.sprintpcs.com`
- **Advantage**: No 3rd party API keys needed, uses existing SMTP
- **Limitation**: Character limits (160 chars), reliability varies

### 4. BackgroundService Implementation

```csharp
public class UnifiWatchService : BackgroundService
{
    private readonly IunifiwatchService _stockService;
    private readonly INotificationProvider _notificationProvider;
    private readonly IConfigurationProvider _configProvider;
    private readonly ICredentialProvider _credentialProvider;
    private readonly ILogger<UnifiWatchService> _logger;
    private bool _isPaused;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                if (!_isPaused)
                {
                    var config = await _configProvider.LoadAsync();
                    var products = await _stockService.GetStockAsync(
                        config.Monitoring.Store,
                        config.Monitoring.Collections,
                        stoppingToken);
                    
                    var inStock = FilterInStockProducts(products, config);
                    if (inStock.Any())
                    {
                        await _notificationProvider.NotifyAsync(inStock, stoppingToken);
                    }
                }
                
                var interval = await _configProvider.GetCheckIntervalAsync();
                await Task.Delay(interval, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during stock check");
            }
        }
    }
}
```

**Features:**
- Respects `CancellationToken` for graceful shutdown
- Pause/Resume support via config file watcher
- Configurable check interval
- Comprehensive logging
- Automatic retry with exponential backoff

### 5. Service Installation Commands

#### Windows
```powershell
# Install
UnifiWatch --install-service

# Commands generated:
# New-Service -Name "UnifiWatch" -BinaryPathName "C:\path\to\UnifiWatch.exe" `
#   -StartupType Automatic -Description "UnifiWatch Service"

# Manage
UnifiWatch --start-service
UnifiWatch --stop-service
UnifiWatch --pause-service
UnifiWatch --resume-service
UnifiWatch --uninstall-service
```

#### Linux (systemd)
```bash
# Install
sudo ./UnifiWatch --install-service

# Generated file: /etc/systemd/system/unifiwatch.service
# [Unit]
# Description=UnifiWatch
# After=network.target
# 
# [Service]
# Type=simple
# User=unifiwatch
# ExecStart=/usr/local/bin/UnifiWatch --service-mode
# Restart=always
# RestartSec=10
# StandardOutput=journal
# StandardError=journal
# 
# [Install]
# WantedBy=multi-user.target

# Manage
sudo systemctl start unifiwatch
sudo systemctl stop unifiwatch
sudo systemctl enable unifiwatch  # auto-start on boot
sudo systemctl restart unifiwatch
sudo systemctl status unifiwatch
```

#### macOS (launchd)
```bash
# Install
./UnifiWatch --install-service

# Generated file: ~/Library/LaunchAgents/com.unifiwatch.tracker.plist
# <?xml version="1.0" encoding="UTF-8"?>
# <!DOCTYPE plist PUBLIC "-//Apple//DTD PLIST 1.0//EN" ...>
# <plist version="1.0">
# <dict>
#   <key>Label</key>
#   <string>com.unifiwatch.tracker</string>
#   <key>ProgramArguments</key>
#   <array>
#     <string>/usr/local/bin/UnifiWatch</string>
#     <string>--service-mode</string>
#   </array>
#   <key>RunAtLoad</key>
#   <true/>
#   ...
# </dict>
# </plist>

# Manage
launchctl load ~/Library/LaunchAgents/com.unifiwatch.tracker.plist
launchctl unload ~/Library/LaunchAgents/com.unifiwatch.tracker.plist
launchctl start com.unifiwatch.tracker
launchctl stop com.unifiwatch.tracker
```

### 6. Configuration Management Commands

```bash
# Interactive configuration wizard
UnifiWatch --configure

# Display current config (redacts credentials)
UnifiWatch --show-config

# Reset to defaults
UnifiWatch --reset-config

# Load from file
UnifiWatch --config-file /path/to/config.json
```

### 7. Dual-Mode Program Execution

**Startup Logic:**
```
If service-install/uninstall/start/stop command
  → Execute platform-specific service management
Else if --configure, --show-config, --reset-config
  → Execute configuration management
Else if --service-mode flag present OR running as Windows Service
  → Use HostBuilder with BackgroundService
  → Run as daemon
Else
  → Use existing CLI behavior
  → Execute command and exit
```

## Dependencies

### New NuGet Packages Required

```xml
<!-- Email -->
<PackageReference Include="MailKit" Version="4.7.0" />

<!-- SMS Providers -->
<PackageReference Include="Twilio" Version="6.13.0" />
<PackageReference Include="AWSSDK.SNS" Version="3.7.300.0" />
<PackageReference Include="Azure.Communication.Sms" Version="1.0.0" />

<!-- Credential Storage -->
<PackageReference Include="CredentialManagement" Version="1.0.2" /> <!-- Windows CredMan -->
<PackageReference Include="Tmds.DBus" Version="10.0.0" /> <!-- Linux secret-service -->

<!-- Hosting/Services -->
<PackageReference Include="Microsoft.Extensions.Hosting" Version="9.0.0" />
<PackageReference Include="Microsoft.Extensions.Hosting.Systemd" Version="9.0.0" />
<PackageReference Include="Microsoft.Extensions.Hosting.WindowsServices" Version="9.0.0" />

<!-- Configuration -->
<PackageReference Include="Microsoft.Extensions.Configuration.Json" Version="9.0.0" />
<PackageReference Include="Microsoft.Extensions.Options.ConfigurationExtensions" Version="9.0.0" />
```

## Security Best Practices

### Credential Handling
1. **Never store credentials in config file** - Always use OS credential storage
2. **Use read-only configuration files** for non-secret settings (mode 600 on Unix)
3. **DPAPI encryption on Windows** - Use `DataProtectionScope.CurrentUser` for user-specific isolation
4. **Prompt for credentials on first setup** - Never require manual JSON editing
5. **Rotate credentials regularly** - Document expiration warnings

### Email/SMS Security
1. **TLS/SSL required** - Enforce STARTTLS for SMTP (port 587, not 25)
2. **API key rotation** - Use limited scope tokens/keys from providers
3. **Rate limiting** - Implement per-recipient rate limiting to prevent spam
4. **Audit logging** - Log all notification sends (redact partial credential)

### Service Security
1. **Run as dedicated user** (Linux) - Create `unifiwatch` system user
2. **Least privilege** - Don't run as root/admin
3. **File permissions** - Config: 600, credentials key: 600
4. **Systemd hardening** - Use `PrivateTmp=yes`, `NoNewPrivileges=yes`

## Error Handling Strategy

| Scenario | Behavior |
|----------|----------|
| Config file missing | Generate default config, prompt user |
| Credential not found | Log error, skip notification, continue checking |
| Email send fails | Retry 3x with exponential backoff, log warning |
| SMS send fails | Fallback to email if enabled, log error |
| Stock API unavailable | Retry next interval, log debug |
| Service crash | Auto-restart (systemd/Windows Service Manager) |

## Testing Strategy

### Unit Tests
- Configuration loading and validation
- Credential encryption/decryption
- Notification formatting (email HTML, SMS length)
- Pause/resume logic
- Service lifecycle

### Integration Tests
- Full flow: config → credentials → check → notify
- All SMS providers (mocked)
- Email SMTP (test server)
- Multi-channel simultaneous sends

### Platform Tests
- Windows: Service Manager, CredMan integration
- Linux: systemctl, secret-service integration
- macOS: launchctl, Keychain integration

## Implementation Phases

### Phase 1: Foundation (Weeks 1-2)
- INotificationProvider interface
- Configuration system + JSON schema
- Credential storage abstraction
- Comprehensive tests

### Phase 2: Services (Weeks 2-3)
- EmailNotificationProvider
- SMS providers (Twilio, AWS SNS, Azure, SMTP Gateway)
- Multi-channel composition
- Tests + mocking

### Phase 3: BackgroundService (Weeks 3-4)
- BackgroundService implementation
- Config file watcher
- Pause/resume
- Logging strategy

### Phase 4: Installation (Weeks 4-5)
- Windows service install commands
- Linux systemd integration
- macOS launchd integration
- Cross-platform abstraction

### Phase 5: Configuration CLI (Weeks 5-6)
- `--configure` interactive wizard
- `--show-config` display
- `--reset-config` capability
- Input validation

### Phase 6: Program Integration (Weeks 6-7)
- Dual-mode Program.cs
- Service vs CLI detection
- HostBuilder setup
- Existing CLI unchanged

### Phase 7: Documentation + Polish (Weeks 7-8)
- SERVICE_SETUP.md (per-platform)
- SECURITY.md (credential handling)
- Configuration examples
- Troubleshooting guide

## Files to Create/Modify

### New Files
```
Configuration/
  ConfigurationProvider.cs           # Abstraction + JSON file management
  CredentialProvider.cs              # OS-specific credential storage abstraction
  
Services/
  INotificationProvider.cs           # Multi-provider interface
  EmailNotificationProvider.cs       # Email implementation
  SmsNotificationProvider.cs         # Abstract SMS base
  TwilioSmsProvider.cs              # Twilio implementation
  AwsSnsSmsProvider.cs              # AWS SNS implementation
  AzureCommunicationSmsProvider.cs  # Azure implementation
  SmtpGatewaySmsProvider.cs         # Carrier email implementation
  CompositeNotificationProvider.cs  # Multi-channel aggregator
  UnifiWatchService.cs       # BackgroundService
  ServiceLifecycleManager.cs        # Install/uninstall/start/stop

Models/
  ServiceConfiguration.cs
  NotificationConfiguration.cs
  CredentialMetadata.cs
  
Utilities/
  PlatformUtilities.cs             # Platform detection, paths
  CredentialManagement/
    WindowsCredentialManager.cs
    MacOsKeychain.cs
    LinuxSecretService.cs
    
Installation/
  ServiceInstaller.cs              # Abstract base
  WindowsServiceInstaller.cs
  LinuxServiceInstaller.cs
  MacOsServiceInstaller.cs

Documentation/
  SERVICE_SETUP.md
  SECURITY.md
  CONFIGURATION_EXAMPLES.md
```

### Modified Files
```
Program.cs                         # Dual-mode detection + HostBuilder
NotificationService.cs             # Refactor to use INotificationProvider
UnifiWatch.csproj          # New NuGet dependencies
README.md                          # Service mode overview + link to docs
```

