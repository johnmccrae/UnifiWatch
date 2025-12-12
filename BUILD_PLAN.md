# UnifiWatch Service Mode - Build Plan

## Overview
Transform UnifiWatch from CLI-only to a dual-mode application supporting both CLI and background service operation with multi-channel notifications (desktop, email, SMS).

## ✅ Completed Phases

### Phase 1: Configuration & Credential Infrastructure (COMPLETE)

**Phase 1a: Configuration Provider & Models**
- ✅ Created `ServiceConfiguration` with nested models (ServiceSettings, MonitoringSettings, NotificationSettings)
- ✅ Implemented `IConfigurationProvider` interface with JSON serialization
- ✅ Built `ConfigurationProvider` with Load/Save/Validate/Backup operations
- ✅ Platform-specific config paths: Windows (`%APPDATA%\UnifiWatch`), macOS/Linux (`~/.config/unifiwatch`)
- ✅ Configuration validation with detailed error reporting
- ✅ Automatic backup creation with collision-free timestamps
- ✅ 19 passing unit tests for configuration system

**Phase 1b: OS-Specific Credential Storage**
- ✅ `ICredentialProvider` interface with Store/Retrieve/Delete/Exists/List operations
- ✅ `WindowsCredentialManager` - Windows Credential Manager integration via P/Invoke
- ✅ `MacOsKeychain` - macOS Keychain integration via `security` command
- ✅ `LinuxSecretService` - Linux secret-service integration via D-Bus
- ✅ `EncryptedFileCredentialProvider` - Cross-platform fallback with conditional compilation:
  - Windows: DPAPI (`System.Security.Cryptography.ProtectedData`)
  - Linux/macOS: AES-256-CBC encryption
- ✅ `EnvironmentVariableCredentialProvider` - Basic fallback for CI/automation
- ✅ `CredentialProviderFactory` - Platform-aware provider selection
- ✅ 46 passing unit tests for credential providers

**Status**: 65/71 tests passing (6 skipped integration tests), 0 build errors, 0 warnings

**Documentation**:
- ✅ `TEST_RESULTS.md` - Comprehensive test results, platform-specific status, macOS/Linux test requirements
- ✅ `MACOS_TESTING.md` - Complete checklist for macOS validation (Keychain, paths, encryption fallback)
- ✅ `BUILD_PLAN.md` - This file, serves as AI assistant handoff document for future sessions

**Lessons Learned**:
- Mock setup complexity: `ILoggerFactory.CreateLogger()` required `It.IsAny<string>()` 
- Backup timestamp collisions: Fixed with millisecond precision + counter loop
- Platform conditionals: `#if WINDOWS` for DPAPI vs AES-CBC required careful testing
- File permissions: Unix chmod 600 requires P/Invoke, not available via managed APIs
- Async testing: All async methods tested with `await`, no `.Result` blocking

**Next Testing Steps**:
1. ⚠️ **macOS testing** (see `MACOS_TESTING.md`) - Validate Keychain integration, config paths, file permissions
2. ⚠️ **Linux testing** - Validate secret-service, GNOME Keyring/KWallet, fallback behavior
3. 📝 Document platform-specific issues/workarounds

---

## 🚀 Remaining Phases

### Phase 2: Internationalization (i18n) Assessment & Infrastructure

**Objective**: Set up localization infrastructure BEFORE building notification templates to avoid rewriting them later

**Context**: Application will be used in Canada (English and French), and the rest of the English Speaking Countries, as well as Germany, Spain, and France. Must establish i18n patterns NOW before hardcoding English in email templates (Phase 3a), SMS messages (Phase 3b), and CLI wizards (Phase 5).

**Why Phase 2**: Email/SMS templates are much easier to build with localization from the start than to refactor later. This phase sets up infrastructure but only creates English resources initially.

**Tasks**:
1. **Audit existing user-facing strings**:
   - Current CLI help text and command descriptions  
   - Existing error messages and validation messages
   - Current desktop notification text
   - Document locations in `I18N_AUDIT.md`

2. **Identify i18n requirements for upcoming phases**:
   - Email templates (Phase 3a) - need structure for multilingual HTML
   - SMS messages (Phase 3b) - 160 char limit across languages
   - Configuration wizard (Phase 5) - interactive prompts
   - Date/time formatting (Canada: YYYY-MM-DD, Germany: DD.MM.YYYY, Spain/France: DD/MM/YYYY)
   - Number formatting (Germany/Spain use comma: 1.234,56)
   - Currency display (Euro, Canadian Dollar)
   - Time zones (UTC internally, local display)

3. **Set up localization infrastructure**:
   ```csharp
   // Add NuGet package
   <PackageReference Include="Microsoft.Extensions.Localization" Version="9.0.0" />
   
   // Create resource structure
   Resources/
     Notifications.en-CA.json  // English Canadian
     Notifications.fr-CA.json  // French Canadian
     Notifications.fr-FR.json  // French (France)
     Notifications.de-DE.json  // German
     Notifications.es-ES.json  // Spanish (Castilian)
     CLI.en-CA.json
     Errors.en-CA.json
     
   // JSON format
   {
     "ProductInStock": "Product {0} is now in stock at {1}",
     "StockCheckInterval": "Checking stock every {0} seconds"
   }
   ```

4. **Create culture infrastructure**:
   - Add `Services/Localization/CultureProvider.cs`:
     ```csharp
     public class CultureProvider
     {
         public CultureInfo GetUserCulture()
         {
             // 1. Check CLI flag: --language fr-CA
             // 2. Check config: "language": "de-DE"  
             // 3. System default: CultureInfo.CurrentUICulture
             // 4. Fallback: en-CA
         }
     }
     ```
   - Update `ServiceConfiguration` to add language setting
   - Inject `IStringLocalizer` in all services via DI

5. **Establish coding standards** (document in `LOCALIZATION_GUIDELINES.md`):
   ```csharp
   // ✅ CORRECT - Localizable
   var message = _localizer["ProductInStock", productName, price];
   
   // ❌ WRONG - Hardcoded English
   var message = $"Product {productName} is now in stock at ${price}";
   
   // ✅ CORRECT - Culture-aware formatting  
   var priceStr = price.ToString("C", CultureInfo.CurrentCulture);
   var dateStr = date.ToString("d", CultureInfo.CurrentCulture);
   
   // ✅ CORRECT - Invariant for internal/logs
   _logger.LogDebug("Price: {Price}", price.ToString(CultureInfo.InvariantCulture));
   ```

6. **Create initial English resources**:
   - Phase 2 scope: English (en-US) only
   - Structure all resource files with placeholder sections
   - Mark with TODO comments for future translation
   - Translations to be added in Phase 7 (professional translators or community)

7. **Update configuration schema**:
   ```csharp
   public class ServiceSettings
   {
       // ... existing properties ...
       
       [JsonPropertyName("language")]
       public string Language { get; set; } = "auto";  // auto, en-CA, fr-CA, fr-FR, de-DE, es-ES
       
       [JsonPropertyName("timeZone")]
       public string TimeZone { get; set; } = "auto";  // auto, or IANA timezone
   }
   ```

8. **Test with international data NOW**:
   - Test Spanish characters (ñ, á, é, í, ó, ú, ü, ¿, ¡)
   - Test German characters (ü, ö, ä, ß)
   - Test French characters (à, â, ç, é, è, ê, ë, î, ï, ô, ù, û, ü, ÿ, æ, œ)
   - Verify UTF-8 encoding throughout (email, SMS, config files)
   - Test currency symbols (€, $, £)

9. **Add unit tests**:
   - Test culture selection logic
   - Test resource loading
   - Test fallback to English when translation missing
   - Test date/number formatting across cultures

**Deliverables**:
- `I18N_AUDIT.md` - Current state assessment
- `LOCALIZATION_GUIDELINES.md` - Coding standards for the team
- `Resources/` directory with initial .json files (English Canadian only, structured for 5 languages)
- `Services/Localization/CultureProvider.cs`
- Updated `Configuration/ServiceConfiguration.cs` with language/timezone
- `UnifiWatch.Tests/LocalizationTests.cs`
- All subsequent phases MUST use `IStringLocalizer` for user-facing text

**Priority**: HIGH - Must complete before Phase 3 to avoid expensive template rewrites

**Estimated Effort**: 1 week (infrastructure + English resources only, not full translation)

**Future Work (Phase 7)**: 
- Hire professional translators for pt-BR, de-DE, ja-JP
- Community contributions for fr-CA, en-CA
- Cultural adaptations (date formats, greeting styles)
- Estimated 2-3 weeks for full translation of all strings

---

### Phase 3: Multi-Channel Notification System

#### Phase 3a: Email Notification Provider
**Objective**: Implement email notifications with HTML templates and retry logic

**Tasks**:
1. Create `INotificationProvider` interface with methods:
   - `Task<bool> SendAsync(NotificationMessage message, CancellationToken ct)`
   - `Task<bool> TestConnectionAsync(CancellationToken ct)`
   - `string ProviderName { get; }`
   - `bool IsConfigured { get; }`

2. Create `NotificationMessage` model:
   - Subject, body/HTML content
   - List of `UnifiProduct` objects for stock alerts
   - Priority level, timestamp

3. Implement `EmailNotificationProvider` using MailKit:
   - Read SMTP config from `EmailNotificationConfig`
   - Retrieve credentials from `ICredentialProvider` using `credentialKey`
   - HTML email template with product table (name, price, stock status, link)
   - SMTP retry logic: 3 attempts with exponential backoff (1s, 2s, 4s)
   - TLS/SSL support based on config
   - Comprehensive logging for send attempts, failures, retries

4. Add email template engine:
   - Create `EmailTemplateBuilder` class
   - HTML template with responsive design
   - Product table with Unifi branding colors
   - Include store name, timestamp, product details

5. Unit tests:
   - Mock SMTP server responses
   - Test retry logic with transient failures
   - Validate HTML template generation
   - Test credential retrieval integration

**NuGet Packages**: `MailKit` (already referenced)

**Files to Create**:
- `Services/Notifications/INotificationProvider.cs`
- `Services/Notifications/NotificationMessage.cs`
- `Services/Notifications/EmailNotificationProvider.cs`
- `Services/Notifications/EmailTemplateBuilder.cs`
- `UnifiWatch.Tests/EmailNotificationProviderTests.cs`

---

#### Phase 3b: Multi-Provider SMS Implementation
**Objective**: Implement SMS notifications with multiple provider backends

**Tasks**:
1. Create abstract `SmsNotificationProvider` base class:
   - Implements `INotificationProvider`
   - Abstract methods: `SendSmsAsync(string phoneNumber, string message)`
   - Shared logic: phone validation (E.164 format), message length checks (160 chars)
   - Message truncation with "..." for long messages
   - Credential retrieval from `ICredentialProvider`

2. Implement `TwilioSmsProvider`:
   - NuGet: `Twilio`
   - Config: Account SID, Auth Token (from credentials), From phone number
   - API integration with retry logic
   - Error handling for invalid numbers, insufficient credits

3. Implement `AwsSnsSmsProvider`:
   - NuGet: `AWSSDK.SimpleNotificationService`
   - Config: AWS Access Key, Secret Key (from credentials), Region
   - SNS publish message API
   - Handle rate limiting, quota errors

4. Implement `AzureCommunicationSmsProvider`:
   - NuGet: `Azure.Communication.Sms`
   - Config: Connection string (from credentials), From phone number
   - Azure Communication Services SMS API
   - Error handling for unsupported regions

5. Implement `SmtpGatewaySmsProvider`:
   - No additional NuGet (reuses MailKit)
   - Config: Carrier gateway mapping (e.g., AT&T: `@txt.att.net`, T-Mobile: `@tmomail.net`)
   - Send email to `{phonenumber}@{carrier-gateway}`
   - Carrier selection from config or auto-detection
   - Fallback option when API providers unavailable

6. Create `SmsProviderFactory`:
   - Creates provider based on config (`provider` field: "twilio", "aws-sns", "azure", "smtp-gateway")
   - Passes `ICredentialProvider` and `ILoggerFactory`
   - Throws descriptive errors for unsupported providers

7. Unit tests for each provider:
   - Mock API clients
   - Test phone validation (valid/invalid formats)
   - Test message truncation
   - Test credential retrieval
   - Test retry logic and error handling

**NuGet Packages**:
- `Twilio`
- `AWSSDK.SimpleNotificationService`
- `Azure.Communication.Sms`

**Files to Create**:
- `Services/Notifications/SmsNotificationProvider.cs` (abstract base)
- `Services/Notifications/TwilioSmsProvider.cs`
- `Services/Notifications/AwsSnsSmsProvider.cs`
- `Services/Notifications/AzureCommunicationSmsProvider.cs`
- `Services/Notifications/SmtpGatewaySmsProvider.cs`
- `Services/Notifications/SmsProviderFactory.cs`
- `UnifiWatch.Tests/SmsNotificationProviderTests.cs`

---

#### Phase 3c: Multi-Channel Notification Composition
**Objective**: Orchestrate notifications across multiple channels simultaneously

**Tasks**:
1. Create `CompositeNotificationProvider`:
   - Implements `INotificationProvider`
   - Aggregates multiple providers (desktop, email, SMS)
   - Constructor takes `ServiceConfiguration`, `ICredentialProvider`, `ILoggerFactory`
   - Loads enabled channels from config

2. Implement notification orchestration:
   - `SendAsync()` sends to all enabled channels in parallel using `Task.WhenAll()`
   - Returns `true` if at least one channel succeeds
   - Logs success/failure for each channel independently
   - Doesn't abort on single channel failure (best-effort delivery)

3. Add priority/fallback logic:
   - Primary channels (desktop, email) always attempted
   - Secondary channels (SMS) only if configured
   - Log summary: "Sent to 2/3 channels (desktop: success, email: success, SMS: failed)"

4. Implement notification deduplication:
   - Track recently sent notifications (in-memory cache with timestamps)
   - Prevent duplicate notifications within configurable time window (default: 5 minutes)
   - Cache key: hash of product list + store

5. Integration tests:
   - Mock all providers (desktop, email, SMS)
   - Test parallel sending
   - Test partial failures (some channels fail)
   - Test deduplication logic
   - Test with various config combinations (email-only, SMS-only, all channels)

6. Update existing `NotificationService`:
   - Refactor to use `CompositeNotificationProvider`
   - Maintain backward compatibility with desktop notifications
   - Add config-driven channel selection

**Files to Create**:
- `Services/Notifications/CompositeNotificationProvider.cs`
- `Services/Notifications/NotificationCache.cs`
- `UnifiWatch.Tests/CompositeNotificationProviderTests.cs`

**Files to Modify**:
- `Services/NotificationService.cs` - Integrate new providers

---

### Phase 4: Background Service Implementation

**Objective**: Create Windows Service / systemd daemon / launchd service for continuous monitoring

**Tasks**:
1. Create `UnifiWatchService : BackgroundService`:
   - Implement `ExecuteAsync(CancellationToken stoppingToken)`
   - Constructor: `IConfigurationProvider`, `IunifiwatchService`, `INotificationProvider`, `ILogger`
   - Dependency injection setup

2. Implement service lifecycle:
   ```csharp
   protected override async Task ExecuteAsync(CancellationToken stoppingToken)
   {
       // Load configuration
       var config = await _configProvider.LoadAsync(stoppingToken);
       
       // Main monitoring loop
       while (!stoppingToken.IsCancellationRequested)
       {
           if (!config.Service.Paused)
           {
               // Fetch stock data
               // Compare with last known state
               // Send notifications if new items in stock
           }
           
           // Wait for check interval
           await Task.Delay(config.Service.CheckIntervalSeconds * 1000, stoppingToken);
       }
   }
   ```

3. Add configuration file watching:
   - Use `FileSystemWatcher` to monitor `config.json`
   - Reload config on file change
   - Support pause/resume via `Service.Paused` flag without service restart
   - Log config reloads

4. Implement graceful shutdown:
   - Handle `stoppingToken.IsCancellationRequested`
   - Complete in-flight notification sends
   - Save state before exit (last check timestamp, product states)
   - Timeout: 30 seconds maximum for shutdown

5. Add state persistence:
   - Save last known product states to `state.json` in config directory
   - Load on startup to detect changes since last run
   - Only notify on new products or status changes

6. Comprehensive logging:
   - Startup: config loaded, providers initialized
   - Each check: products found, changes detected
   - Notifications: channels used, success/failure
   - Errors: API failures, notification failures
   - Shutdown: graceful stop, in-flight operations

7. Unit tests:
   - Mock `BackgroundService` execution
   - Test configuration reload
   - Test pause/resume functionality
   - Test graceful shutdown with in-flight operations
   - Test state persistence and reload

**Files to Create**:
- `Services/UnifiWatchService.cs`
- `Models/ServiceState.cs`
- `UnifiWatch.Tests/UnifiWatchServiceTests.cs`

---

### Phase 5: Service Installation & Management Framework

**Objective**: Cross-platform service installation with OS-native tools

**Tasks**:
1. Create `IServiceInstaller` interface:
   ```csharp
   Task<bool> InstallAsync(ServiceInstallOptions options);
   Task<bool> UninstallAsync();
   Task<bool> StartAsync();
   Task<bool> StopAsync();
   Task<ServiceStatus> GetStatusAsync();
   ```

2. Create `ServiceInstallOptions` model:
   - Service name, display name, description
   - Auto-start on boot (default: true)
   - User account (default: current user)
   - Dependencies, recovery options

3. Implement `WindowsServiceInstaller`:
   - Use `New-Service` PowerShell cmdlet via `System.Diagnostics.Process`
   - Alternatively: Use P/Invoke to Win32 Service Control Manager APIs
   - Generate service configuration:
     - Service name: "UnifiWatch"
     - Binary path: `{exe} --service-mode`
     - Startup type: Automatic (Delayed Start)
     - Recovery: Restart on failure (3 attempts)
   - Commands: `Start-Service`, `Stop-Service`, `Get-Service`
   - Verify installation by querying SCM

4. Implement `LinuxServiceInstaller`:
   - Generate systemd unit file: `/etc/systemd/system/UnifiWatch.service`
   - Unit file template:
     ```ini
     [Unit]
     Description=UnifiWatch Service
     After=network.target
     
     [Service]
     Type=notify
     ExecStart={dotnet-path} {dll-path} --service-mode
     Restart=on-failure
     RestartSec=10
     User={current-user}
     
     [Install]
     WantedBy=multi-user.target
     ```
   - Commands: `systemctl daemon-reload`, `systemctl enable`, `systemctl start`
   - Requires sudo/root for `/etc/systemd/system/` write access
   - Check service status with `systemctl status`

5. Implement `MacOsServiceInstaller`:
   - Generate launchd plist: `~/Library/LaunchAgents/com.unifiwatch.plist`
   - Plist template:
     ```xml
     <?xml version="1.0" encoding="UTF-8"?>
     <!DOCTYPE plist PUBLIC "-//Apple//DTD PLIST 1.0//EN" ...>
     <plist version="1.0">
     <dict>
         <key>Label</key>
         <string>com.unifiwatch</string>
         <key>ProgramArguments</key>
         <array>
             <string>{dotnet-path}</string>
             <string>{dll-path}</string>
             <string>--service-mode</string>
         </array>
         <key>RunAtLoad</key>
         <true/>
         <key>KeepAlive</key>
         <true/>
     </dict>
     </plist>
     ```
   - Commands: `launchctl load`, `launchctl unload`, `launchctl start`
   - Check status with `launchctl list | grep unifiwatch`

6. Create `ServiceInstallerFactory`:
   - Platform detection (Windows/Linux/macOS)
   - Returns appropriate installer implementation
   - Throws on unsupported platforms

7. Add CLI commands to `Program.cs`:
   - `--install-service` - Install and start service
   - `--uninstall-service` - Stop and remove service
   - `--start-service` - Start existing service
   - `--stop-service` - Stop running service
   - `--service-status` - Check service state
   - Require admin/sudo for installation (check and prompt)

8. Unit tests:
   - Mock service installation commands
   - Test unit file / plist generation
   - Validate command execution
   - Test error handling (no permissions, service exists, etc.)

**Files to Create**:
- `Services/Installation/IServiceInstaller.cs`
- `Services/Installation/ServiceInstallOptions.cs`
- `Services/Installation/ServiceStatus.cs`
- `Services/Installation/WindowsServiceInstaller.cs`
- `Services/Installation/LinuxServiceInstaller.cs`
- `Services/Installation/MacOsServiceInstaller.cs`
- `Services/Installation/ServiceInstallerFactory.cs`
- `UnifiWatch.Tests/ServiceInstallerTests.cs`

---

### Phase 6: Configuration CLI & Dual-Mode Program.cs

**Objective**: Interactive configuration wizard and service/CLI mode integration

#### Phase 6a: Configuration CLI Commands

**Tasks**:
1. Create `ConfigurationWizard` class:
   - Interactive prompts using `System.CommandLine` or `Spectre.Console`
   - Step-by-step configuration:
     1. Store selection (USA/Europe/UK)
     2. Product filters (collections, names, SKUs)
     3. Check interval (seconds, with validation)
     4. Notification channels (desktop/email/SMS)
     5. Email SMTP configuration (if enabled)
     6. SMS provider selection and config (if enabled)
   - Credential collection with masking (password input)
   - Store credentials via `ICredentialProvider`
   - Save config via `IConfigurationProvider`
   - Validate all inputs before saving

2. Add `--configure` command:
   - Launch interactive wizard
   - Display current config if exists (offer to modify or recreate)
   - Test connections (SMTP, SMS API) before saving

3. Add `--show-config` command:
   - Load and display current configuration
   - Redact credentials (show "***" for passwords)
   - Pretty-print JSON or formatted output
   - Show config file path

4. Add `--reset-config` command:
   - Delete current configuration
   - Optionally delete credentials
   - Confirm before deletion

5. Add `--test-notifications` command:
   - Send test notification to all enabled channels
   - Verify email delivery, SMS delivery, desktop notification
   - Display success/failure for each channel

**Files to Create**:
- `CLI/ConfigurationWizard.cs`
- `CLI/ConfigurationCommands.cs`

**Files to Modify**:
- `Program.cs` - Add new command handlers

---

#### Phase 6b: Dual-Mode Program.cs Integration

**Tasks**:
1. Detect execution mode:
   ```csharp
   static async Task<int> Main(string[] args)
   {
       // Check for service mode flag or Windows Service context
       if (args.Contains("--service-mode") || WindowsServiceHelpers.IsWindowsService())
       {
           return await RunAsServiceAsync(args);
       }
       else
       {
           return await RunAsCliAsync(args);
       }
   }
   ```

2. Implement `RunAsServiceAsync`:
   - Create `HostBuilder` with `BackgroundService`
   - Configure dependency injection:
     - `IConfigurationProvider`
     - `ICredentialProvider`
     - `IunifiwatchService`
     - `INotificationProvider`
     - `ILogger`
   - Add Windows Service/systemd/launchd lifetime support
   - Start `UnifiWatchService`

3. Implement `RunAsCliAsync`:
   - Keep existing CLI commands intact
   - Add new commands: `--configure`, `--show-config`, `--install-service`, etc.
   - Use `System.CommandLine` or keep existing command parsing
   - Maintain backward compatibility with current usage

4. Add host configuration:
   - Windows: `UseWindowsService()`
   - Linux: `UseSystemd()`
   - macOS: Standard hosted service (no special lifetime needed)

5. Integration tests:
   - Test CLI mode commands
   - Test service mode startup (mock `BackgroundService`)
   - Test mode detection logic
   - Verify DI container configuration

**Files to Modify**:
- `Program.cs` - Complete refactor for dual-mode support

**NuGet Packages**:
- Ensure `Microsoft.Extensions.Hosting.WindowsServices` and `Microsoft.Extensions.Hosting.Systemd` are referenced

---

### Phase 7: Full Localization & Translation

**Objective**: Complete professional translation of all strings to target languages

**Context**: Infrastructure established in Phase 2. All code uses `IStringLocalizer`. Now hire translators and complete cultural adaptations.

**Prerequisites**: Phases 2-6 complete with all English resources in `Resources/` directory

**Tasks**:
1. **Hire professional translators**:
   - Portuguese (Brazilian) - pt-BR translator
   - German - de-DE translator  
   - Japanese - ja-JP translator
   - French (Canadian) - fr-CA translator (or community contribution)
   - Provide context: app is for stock tracking, technical terms

2. **Complete all translation files**:
   - `Resources/Notifications.{locale}.json`
   - `Resources/CLI.{locale}.json`
   - `Resources/Errors.{locale}.json`
   - `Resources/EmailTemplates.{locale}.json`
   - ~500-1000 strings total (estimated from Phase 2 audit)

3. **Cultural adaptations**:
   - **Date formats**:
     - en-US: MM/DD/YYYY
     - en-CA/en-GB: DD/MM/YYYY
     - de-DE: DD.MM.YYYY
     - ja-JP: YYYY年MM月DD日 or YYYY/MM/DD
     - pt-BR: DD/MM/YYYY
   - **Number formats**:
     - US/CA: 1,234.56
     - DE/PT-BR: 1.234,56
     - JP: 1,234.56
   - **Currency symbols**: $, €, R$, ¥
   - **Greeting styles**: Formal (DE, JP) vs casual (US)

4. **Localize email templates**:
   - HTML templates with proper encoding
   - Subject lines translated
   - Product table headers
   - Call-to-action buttons
   - Test rendering in common email clients

5. **Localize SMS messages**:
   - Respect 160 character limit in all languages
   - German compound words may be longer
   - Japanese can be more concise (fewer chars)
   - Test truncation behavior

6. **Test all localizations**:
   - Native speaker review for each language
   - Technical accuracy (stock tracking terminology)
   - Consistency across all modules
   - Missing translation fallback to English

7. **Update documentation**:
   - Add language examples to README
   - Document how to contribute new translations
   - Add `TRANSLATION.md` guide for community

**Deliverables**:
- Complete translation files for 5 languages (en-US, pt-BR, de-DE, ja-JP, fr-CA, en-CA)
- Localized email HTML templates
- Native speaker QA sign-off for each language
- `TRANSLATION.md` - Guide for future community contributions

**Priority**: Medium (after core features stable)

**Estimated Effort**: 2-3 weeks (depends on translator availability)

**Cost Estimate**: $2,000-4,000 for professional translation (~$0.10-0.20 per word)
   ```
   Recommended approach:
   - Phase 6.1: Extract all strings to resource files
   - Phase 6.2: Implement IStringLocalizer in CLI/notifications
   - Phase 6.3: Create translation files (en-US, pt-BR, de-DE, ja-JP, en-CA, fr-CA)
   - Phase 6.4: Add --language flag or detect system locale
   - Phase 6.5: Localized email templates
   ```

5. **Document quick wins for now**:
   - Use `CultureInfo.InvariantCulture` for all logging/internal operations
   - Use `DateTime.UtcNow` consistently (avoid local time confusion)
   - Store all timestamps as UTC in state files
   - Use ISO 8601 format for config file dates
   - Ensure UTF-8 encoding for all file operations
   - Phone numbers: Already planning E.164 format (international-ready)

6. **Estimate localization effort**:
   - Low effort (1-2 weeks):
     - CLI help text only
     - Keep notifications in English
   - Medium effort (2-4 weeks):
     - CLI + email templates
     - Core error messages
     - 4 languages (en, pt-BR, de-DE, ja-JP)
   - High effort (4-6 weeks):
     - Full localization (all strings)
     - 6 languages (add fr-CA, en-CA)
     - Right-to-left language prep (future: Arabic, Hebrew)
     - Cultural adaptations (date formats, greetings)

7. **Create i18n-ready patterns**:
   - Example pattern to follow now:
     ```csharp
     // GOOD: Easy to extract later
     var message = "Product {0} is now in stock at ${1}";
     Console.WriteLine(string.Format(message, productName, price));
     
     // BAD: Hard to localize later
     Console.WriteLine($"Product {productName} is now in stock at ${price}");
     ```
   - Document in coding guidelines

8. **Test with non-English product data**:
   - Ensure Japanese product names (Kanji/Katakana) display correctly
   - Test German umlauts (ü, ö, ä, ß) in email templates
   - Verify UTF-8 encoding throughout stack

**Deliverable**: 
- `I18N_AUDIT.md` - Assessment report with:
  - String count and categories
  - Pain point analysis
  - Recommended localization approach
  - Effort estimate (low/medium/high)
  - "Safe patterns" guide for writing i18n-ready code now
- No code changes yet, just documentation

**Priority**: Low (Phase 6) - Document now, implement after core features stable

---

### Phase 8: Documentation & Deployment Testing

#### Phase 8a: Documentation

**Tasks**:
1. Create `SERVICE_SETUP.md`:
   - Installation instructions per platform
   - Windows:
     - Prerequisites (Credential Manager enabled)
     - Installation: `UnifiWatch.exe --install-service`
     - Configuration: `UnifiWatch.exe --configure`
     - Viewing credentials in Credential Manager
   - Linux:
     - Prerequisites (D-Bus secret-service, systemd)
     - Installation: `sudo ./UnifiWatch --install-service`
     - systemd commands: `systemctl status UnifiWatch`
   - macOS:
     - Prerequisites (Keychain access)
     - Installation: `./UnifiWatch --install-service`
     - launchctl commands

2. Create `SECURITY.md`:
   - Credential storage best practices
   - Why DPAPI/Keychain/secret-service are secure
   - Encrypted file fallback security considerations
   - TLS/SSL for SMTP
   - API key rotation recommendations
   - Permissions (file: 600, directory: 700)

3. Add configuration examples:
   - `examples/config.email-only.json`
   - `examples/config.sms-twilio.json`
   - `examples/config.all-channels.json`
   - `examples/config.minimal.json`

4. Update `README.md`:
   - Add "Service Mode" section
   - Overview of dual-mode operation
   - Quick start for service installation
   - Link to SERVICE_SETUP.md for details

5. Add troubleshooting guide:
   - Service won't start: check logs, permissions
   - Notifications not sending: test with `--test-notifications`
   - Credential errors: verify storage method supported
   - High CPU/memory: adjust check interval

**Files to Create**:
- `SERVICE_SETUP.md`
- `SECURITY.md`
- `examples/config.email-only.json`
- `examples/config.sms-twilio.json`
- `examples/config.all-channels.json`
- `examples/config.minimal.json`

**Files to Modify**:
- `README.md`

---

#### Phase 8b: Platform-Specific Deployment Testing

**Tasks**:
1. Windows testing:
   - Install service: `UnifiWatch.exe --install-service`
   - Verify service in Services.msc
   - Check Windows Event Viewer for logs
   - Store credentials in Credential Manager via wizard
   - Verify notifications work (desktop toast, email, SMS)
   - Test pause/resume via config change
   - Uninstall and verify cleanup

2. Linux (Ubuntu/Debian) testing:
   - Build and publish: `dotnet publish -r linux-x64 -c Release`
   - Install service with sudo
   - Check systemd status: `systemctl status UnifiWatch`
   - View logs: `journalctl -u UnifiWatch -f`
   - Test secret-service credential storage (requires GNOME Keyring or KDE Wallet)
   - Verify email/SMS notifications (desktop notifications may not work on headless)
   - Test auto-restart on failure

3. macOS testing:
   - Build and publish: `dotnet publish -r osx-x64 -c Release`
   - Install service (user-level launchd)
   - Check status: `launchctl list | grep unifiwatch`
   - View logs: `tail -f ~/Library/Logs/UnifiWatch/service.log`
   - Test Keychain credential storage
   - Verify desktop notifications work
   - Test uninstall and cleanup

4. Cross-platform integration test:
   - Run same configuration on all three platforms
   - Verify identical behavior (notifications, stock checks)
   - Performance benchmarking (CPU, memory usage)

5. Edge case testing:
   - No internet connection: verify retry logic
   - Invalid credentials: verify error messages
   - Config file corruption: verify fallback to defaults
   - Rapid config changes: verify reload works
   - Service crash: verify auto-restart

**Deliverable**: Test report with platform compatibility matrix

---

## Summary of Remaining Work

| Phase | Estimated Complexity | Key Deliverables |
|-------|---------------------|------------------|
| Phase 2 | Medium | i18n infrastructure + English resources (must do BEFORE templates) |
| Phase 3a | Medium | Email provider with localizable HTML templates |
| Phase 3b | High | 4 SMS providers + factory with localized messages |
| Phase 3c | Medium | Composite provider with orchestration |
| Phase 4 | Medium | BackgroundService with monitoring loop |
| Phase 5 | High | 3 platform installers + CLI commands |
| Phase 6 | Medium | Localized configuration wizard + dual-mode Program.cs |
| Phase 7 | Medium | Professional translation to 5 languages |
| Phase 8 | Low | Documentation + deployment testing |

**Total Estimated Test Count**: ~170 additional tests across all phases

**Critical Path**: Phase 2 (i18n infrastructure) blocks Phase 3 (notifications). Must establish localization patterns before building email/SMS templates to avoid expensive rewrites.

**Internationalization Strategy**: 
- **Phase 2** (Week 1): Set up infrastructure, English resources only
- **Phase 3-6** (Weeks 2-7): Build features using IStringLocalizer 
- **Phase 7** (Weeks 8-10): Professional translation + cultural adaptation
- **Target Languages**: English (US/Canada), Portuguese (Brazil), German, Japanese, French (Canada)

---

## How to Use This Document

When resuming work on this project, provide this section as context:

```
I'm working on UnifiWatch C# service implementation. 
We've completed Phase 1 (Configuration & Credentials). 
Please implement [Phase X] following the BUILD_PLAN.md specification.
```

This document serves as a complete implementation roadmap with enough detail for AI-assisted development while maintaining consistency across sessions.
