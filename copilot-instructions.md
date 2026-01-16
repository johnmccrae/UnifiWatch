# Copilot Instructions for UnifiWatch Development

## Critical: Development Environment

This project is primarily developed on **Windows** using **PowerShell**. 

### Terminal Commands - WINDOWS ONLY

⚠️ **DO NOT use Linux/bash commands on Windows.** Always use PowerShell cmdlets and command equivalents.

**Prohibited (Linux/bash commands):**
- ❌ `head -50`
- ❌ `tail -20`
- ❌ `cat file.txt`
- ❌ `ls -la`
- ❌ `grep pattern file`
- ❌ `find . -name "*.cs"`
- ❌ `sed 's/old/new/g'`
- ❌ `awk`
- ❌ `cut`

**Use instead (PowerShell equivalents):**
- ✅ `Get-Content file.txt -Head 50`
- ✅ `Get-Content file.txt -Tail 20`
- ✅ `Get-Content file.txt`
- ✅ `Get-ChildItem -Force` or `Get-ChildItem -Recurse`
- ✅ `Select-String -Path file.txt -Pattern "pattern"`
- ✅ `Get-ChildItem -Filter "*.cs" -Recurse`
- ✅ Use `Select-String` or native PowerShell filtering
- ✅ Use PowerShell objects/filtering

### Build & Test Commands

```powershell
# Build the solution
dotnet build

# Run all tests
dotnet test

# Run specific test file
dotnet test UnifiWatch.Tests/NotificationServiceTests.cs

# Run with coverage
dotnet test /p:CollectCoverage=true /p:CoverageFormat=opencover
```

### File Operations in PowerShell

```powershell
# Remove files
Remove-Item "path/to/*.cs" -Force

# Copy files
Copy-Item -Path "src/*" -Destination "dest/" -Recurse

# List files
Get-ChildItem -Path "path" -Filter "*.cs" -Recurse

# Create directory
New-Item -ItemType Directory -Path "path/to/dir" -Force
```

## Code Standards

### Namespaces
- **Correct:** `namespace UnifiWatch.Services.Notifications;`
- **Incorrect:** `namespace UnifiWatch;` (then Services.Notifications subfolder)

### Localization
**All user-facing strings must use `IStringLocalizer`**, not hardcoded English:
```csharp
// ❌ WRONG
_logger.LogWarning("SMTP send failed");

// ✅ CORRECT
_logger.LogWarning(_localizer["SMTP send failed"]);
```

String keys must exist in resource files:
- `Resources/Notifications.en-CA.json` (English - Canadian)
- `Resources/Notifications.fr-CA.json` (French - Canadian)
- `Resources/Notifications.de-DE.json` (German)
- `Resources/Notifications.es-ES.json` (Spanish)
- `Resources/Notifications.fr-FR.json` (French)
- `Resources/Notifications.it-IT.json` (Italian)

### Dependency Injection
Email notification providers require:
```csharp
public SmtpEmailProvider(
    EmailNotificationConfig config,
    ICredentialProvider credentialProvider,
    IStringLocalizer localizer,
    ILogger<SmtpEmailProvider> logger)
```

## Architecture

### Phase Completion Status
- ✅ **Phase 1:** Configuration & Credentials (65 tests passing)
- ✅ **Phase 2:** Localization (92 tests passing, 7 cultures)
- 🔄 **Phase 3a:** Email Notifications (in progress)
  - 🔄 SMTP Provider (with IStringLocalizer integration)
  - 🔄 Microsoft Graph Provider (OAuth2, with IStringLocalizer integration)
  - 🔄 Email Template Builder (with IStringLocalizer integration)
- ⏳ **Phase 3b:** SMS Notifications (pending)
- ⏳ **Phase 3c:** Multi-channel Orchestration (pending)

### Key Integration Points
1. **Configuration:** Use `EmailNotificationConfig` from `Configuration/ServiceConfiguration.cs`
2. **Credentials:** Use `ICredentialProvider.RetrieveAsync(key, cancellationToken)`
3. **Localization:** Inject `IStringLocalizer` into all notification providers
4. **Logging:** Use `ILogger<T>` with localized messages
5. **Models:** Reference `UnifiProduct` from `Models/UnifiProduct.cs`

## Testing

All unit tests must pass before committing:
```powershell
dotnet test
```

Expected results: ~92 tests passing, 11 skipped, 0 failed

## Git Workflow

1. Create feature branch from `main`
2. Implement changes with full localization
3. Run tests: `dotnet test`
4. Commit: `git commit -m "Phase 3a: SMTP email provider with localization"`
5. Push and create pull request

## Common Issues

### Build Failures
- **Missing `using` statements:** Always include required namespaces at top of file
- **Namespace mismatch:** Use `UnifiWatch.*` (not `UnifiStockTracker.*`)
- **Missing interfaces:** Implement all methods from `INotificationProvider`

### Localization Issues
- **Hardcoded English strings:** Replace with `_localizer["key"]` pattern
- **Missing resource keys:** Add keys to all resource files (not just en-CA)
- **String formatting:** Use `string.Format(_localizer["key"], param)` for parameterized strings

### Credential Issues
- **Async methods:** Always use `RetrieveAsync()`, not synchronous `Retrieve()`
- **CancellationToken:** Pass to credential retrieval: `RetrieveAsync(key, cancellationToken)`

## Resources

- **BUILD_PLAN.md:** Project phases and roadmap
- **SERVICE_ARCHITECTURE.md:** System design and data flow
- **LOCALIZATION_GUIDELINES.md:** Internationalization standards
- **EMAIL_SMS_SETUP.md:** Configuration guide for email/SMS providers
