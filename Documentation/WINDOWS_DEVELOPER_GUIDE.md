# Windows Developer Guide

**Audience**: Developers and contributors building and running the automated test suite

**Purpose**: Complete guide for building, testing, and validating UnifiWatch on Windows  
**Platform**: Windows 10 or later  
**Phase**: Phase 1 - Configuration & Credential Infrastructure  
**Last Updated**: December 2025

---

## Table of Contents

1. [Prerequisites](#prerequisites)
2. [Installation](#installation)
3. [Building the Application](#building-the-application)
4. [Testing](#testing)
5. [Phase 1 Validation Tests](#phase 1-validation-tests)
6. [Known Issues & Workarounds](#known-issues--workarounds)
7. [Troubleshooting](#troubleshooting)

---

## Prerequisites

- **Operating System**: Windows 10 or later (Windows 11 recommended)
- **.NET SDK**: Version 9.0 or later
- **PowerShell**: 5.1 or later (built into Windows)
- **Windows Credential Manager**: For credential storage (built into Windows)

---

## Installation

### Step 1: Install .NET 9.0 SDK

1. Visit: https://dotnet.microsoft.com/download/dotnet/9.0
2. Download the Windows x64 installer
3. Run the installer and follow the prompts
4. Restart PowerShell/Command Prompt if open

**Verify Installation**:

```powershell
dotnet --version
```

**Expected**: `9.0.x` or later

### Step 2: Get the Source Code

#### Option A: Clone from GitHub

```powershell
git clone https://github.com/johnmccrae/UnifiWatch.git
cd UnifiWatch
```

#### Option B: Download ZIP

1. Download the repository as ZIP
2. Extract to your preferred location (e.g., `C:\Projects\UnifiWatch`)
3. Navigate to the folder

### Step 3: Restore Dependencies

```powershell
cd UnifiWatch
dotnet restore UnifiWatch.sln
```

**Expected**: NuGet packages downloaded successfully

---

## Building the Application

### Development Build

For development and testing:

```powershell
dotnet build UnifiWatch.sln
```

**Expected Output**:
- Build succeeded
- 0 Error(s)
- 0 Warning(s)
- Build time: ~2-3 minutes on first build, ~10-20 seconds on subsequent builds

### Release Build

```powershell
dotnet build UnifiWatch.sln --configuration Release
```

### Publish Standalone Executable

```powershell
dotnet publish UnifiWatch.csproj -c Release -r win-x64 --self-contained -p:PublishSingleFile=true
```

**Output Location**: `bin\Release\net9.0\win-x64\publish\UnifiWatch.exe`

---

## Testing

### Quick Functional Tests

#### Test 1: Check Stock (Basic Functionality)

```powershell
.\bin\Release\net9.0\win-x64\publish\UnifiWatch.exe --stock --store USA --product-names "Dream Machine"
```

**Expected Output**:
- `Getting Unifi products from USA store...`
- `Retrieved XXX products`
- Table of products with stock status

#### Test 2: Monitor Mode

```powershell
.\bin\Release\net9.0\win-x64\publish\UnifiWatch.exe --wait --store USA --product-names "Dream Machine" --seconds 5
```

**Expected Behavior**:
- Checks for the product every 5 seconds
- Displays current stock status
- Press Ctrl+C to stop

#### Test 3: Test All Stores

**GraphQL API Stores (Modern):**

```powershell
# USA
.\bin\Release\net9.0\win-x64\publish\UnifiWatch.exe --stock --store USA

# Europe
.\bin\Release\net9.0\win-x64\publish\UnifiWatch.exe --stock --store Europe

# UK
.\bin\Release\net9.0\win-x64\publish\UnifiWatch.exe --stock --store UK
```

**Shopify API Stores (Legacy):**

```powershell
# Brazil
.\bin\Release\net9.0\win-x64\publish\UnifiWatch.exe --stock --legacy-api-store Brazil

# Japan
.\bin\Release\net9.0\win-x64\publish\UnifiWatch.exe --stock --legacy-api-store Japan
```

### Run All Unit Tests

```powershell
dotnet test UnifiWatch.sln
```

**Expected Results**:
  - Total: 223 tests
  - Passed: 212 ✅
  - Failed: 0 ❌
  - Skipped: 11 (integration/advanced tests)

**Last Tested**: December 7, 2025  
**Platform**: Windows  
**SDK Version**: .NET 9.0

---

## Phase 1 Validation Tests

These tests validate Phase 1 infrastructure: Configuration provider and credential storage.

### Windows-Specific Notes

#### Credential Storage

Windows uses **Windows Credential Manager (DPAPI)** for secure credential storage, providing:
- Native OS-level encryption
- Automatic key management by Windows
- No manual machine ID setup required
- Integration with Windows security policies
- Encryption keys tied to Windows user account

#### File Paths

Windows uses different path conventions:
- Configuration directory: `%APPDATA%\UnifiWatch` (e.g., `C:\Users\YourName\AppData\Roaming\UnifiWatch`)
- Credentials file: `%APPDATA%\UnifiWatch\credentials.enc.json`

---

### Test Group 1: Configuration Provider

#### Test 1.1: Configuration File Location

```powershell
dotnet test UnifiWatch.sln --filter "ConfigurationProviderTests.LoadAsync_ValidConfiguration_LoadsAllSettings"
```

**Verification**:

- [ ] Test passes
- [ ] Config directory created:
  ```powershell
  Test-Path $env:APPDATA\UnifiWatch
  ```
  **Expected**: `True`
- [ ] Config file exists:
  ```powershell
  Test-Path $env:APPDATA\UnifiWatch\config.json
  ```
  **Expected**: `True`

#### Test 1.2: Configuration Save/Load Cycle

```powershell
dotnet test UnifiWatch.sln --filter "ConfigurationProviderTests.SaveAsync_ValidConfiguration_SavesCorrectly"
```

**Verification**:

- [ ] Test passes
- [ ] JSON file created with valid structure:
  ```powershell
  Get-Content $env:APPDATA\UnifiWatch\config.json
  ```
  **Expected**: Valid JSON with ServiceSettings, MonitoringSettings, NotificationSettings

#### Test 1.3: Configuration Backup

```powershell
dotnet test UnifiWatch.sln --filter "ConfigurationProviderTests.BackupAsync_CreatesBackupFile"
```

**Verification**:

- [ ] Test passes
- [ ] Backup file created:
  ```powershell
  Get-ChildItem $env:APPDATA\UnifiWatch\*.backup.json
  ```
  **Expected**: Backup file with timestamp

---

### Test Group 2: Windows Credential Manager Storage

#### Test 2.1: Platform Detection

```powershell
dotnet test UnifiWatch.sln --filter "CredentialProviderTests.CredentialProviderFactory_CreateProvider_WithAutoStorage_ShouldSelectDefault"
```

**Verification**:

- [ ] Test passes
- [ ] Platform correctly detected as Windows

#### Test 2.2: DPAPI Encryption

Windows uses DPAPI (Data Protection API) for credential encryption:

```powershell
dotnet test UnifiWatch.sln --filter "CredentialProviderTests"
```

**Expected**: All 46 credential provider tests should pass

**DPAPI Security Features**:
- Credentials encrypted using Windows DPAPI
- Keys tied to your Windows user account
- Credentials cannot be decrypted by other users
- If user profile is moved to another machine, credentials must be re-entered

#### Test 2.3: Windows Credential Manager Integration

To verify DPAPI encryption is working:

```powershell
# Run credential tests
dotnet test UnifiWatch.sln --filter "FullyQualifiedName~CredentialProviderTests"

# Verify encrypted file exists
Test-Path $env:APPDATA\UnifiWatch\credentials.enc.json

# Verify file is encrypted (not plaintext)
Get-Content $env:APPDATA\UnifiWatch\credentials.enc.json
```

**Expected**: Content should be binary/encrypted data, not readable JSON

#### Test 2.4: Retrieve Non-Existent Credential

```powershell
dotnet test UnifiWatch.sln --filter "CredentialProviderTests.RetrieveAsync_WithNonExistentKey_ShouldReturnNull"
```

**Verification**:

- [ ] Test passes
- [ ] Error handling works (no crash, returns null gracefully)

#### Test 2.5: Update Existing Credential

```powershell
dotnet test UnifiWatch.sln --filter "CredentialProviderTests.StoreAsync_UpdateExistingKey_ShouldOverwrite"
```

**Verification**:

- [ ] Test passes
- [ ] Credential updated successfully in encrypted file

#### Test 2.6: Delete Credential

```powershell
dotnet test UnifiWatch.sln --filter "CredentialProviderTests.DeleteAsync_AfterStore_ShouldSucceed"
```

**Verification**:

- [ ] Test passes
- [ ] Credential removed from encrypted file

#### Test 2.7: List All Credential Keys

```powershell
dotnet test UnifiWatch.sln --filter "CredentialProviderTests.ListAsync_AfterMultipleStores_ShouldReturnAllKeys"
```

**Verification**:

- [ ] Test passes
- [ ] Multiple credentials can be stored and listed

---

### Test Group 3: Cross-Platform Path Handling

#### Test 3.1: Configuration Paths

```powershell
dotnet test UnifiWatch.sln --filter "ConfigurationProviderTests"
```

**Verify Windows-specific paths**:

- [ ] Config directory: `%APPDATA%\UnifiWatch\`
- [ ] Config file: `%APPDATA%\UnifiWatch\config.json`
- [ ] Backup files: `%APPDATA%\UnifiWatch\config.backup.*.json`

#### Test 3.2: Credential Paths

```powershell
dotnet test UnifiWatch.sln --filter "CredentialProviderTests"
```

**Verify Windows-specific paths**:

- [ ] Credential file: `%APPDATA%\UnifiWatch\credentials.enc.json`

---

### Test Group 4: Build & Compilation

#### Test 4.1: Clean Build

```powershell
dotnet clean UnifiWatch.sln
dotnet build UnifiWatch.sln --configuration Release
```

**Verification**:

- [ ] 0 errors
- [ ] 0 warnings
- [ ] Conditional compilation works (`#if WINDOWS` blocks included on Windows)

#### Test 4.2: All Unit Tests

```powershell
dotnet test UnifiWatch.sln --configuration Release
```

**Expected Results**:

  - [ ] 212 tests passed ✅
  - [ ] 11 tests skipped (integration/advanced tests)
- [ ] 0 tests failed ❌

---

## Running Tests with Coverage

Generate test coverage report:

```powershell
dotnet test --collect:"XPlat Code Coverage"
```

---

## Running Specific Test Categories

Run only unit tests:
```powershell
dotnet test --filter Category=Unit
```

Run only integration tests (requires network):
```powershell
dotnet test --filter Category=Integration
```

---

## Performance Testing

Run tests with detailed timing:

```powershell
dotnet test --logger "console;verbosity=detailed"
```

---

## Known Issues & Workarounds

### Issue: "Access Denied" errors

**Symptom**: Permission errors when testing credential storage in system directories

**Solution**: Run PowerShell as Administrator if testing credential storage in system directories

### Issue: Tests fail with path errors

**Symptom**: Path separator errors or file not found

**Solution**: 
- Ensure paths use Windows path separators (`\`)
- Verify the application correctly detects Windows as the platform
- Use `Path.Combine()` in code for cross-platform compatibility

### Issue: DPAPI encryption fails

**Symptom**: Credential encryption/decryption throws exceptions

**Solution**: 
- Verify your Windows user account is properly configured
- Check that Windows Credential Manager service is running
- Ensure your user profile is not corrupted
- Try running as a different user to isolate the issue

### Issue: Port conflicts when testing

**Symptom**: Tests fail with port already in use errors

**Solution**: Stop any running instances of the application or other services using the same ports

---

## Clean Test Environment

Remove test artifacts:

```powershell
Remove-Item -Recurse -Force bin, obj
Remove-Item -Recurse -Force UnifiWatch.Tests\bin, UnifiWatch.Tests\obj
Remove-Item -Recurse -Force $env:APPDATA\UnifiWatch
```

---

## Troubleshooting

### "command not found: dotnet"

**Solution**:
- .NET SDK is not installed
- Install from https://dotnet.microsoft.com/download/dotnet/9.0
- Restart PowerShell after installation

### Build errors with NuGet packages

**Solution**:
- Clear NuGet cache: `dotnet nuget locals all --clear`
- Restore packages: `dotnet restore`
- Rebuild: `dotnet build`

### Tests timeout or hang

**Solution**:
- Check internet connectivity
- Disable firewall temporarily to test
- Verify antivirus isn't blocking network access

### Windows Defender blocking application

**Solution**:
- Add exception in Windows Defender for the project directory
- Check Windows Event Viewer for application errors

---

## Test Results Summary

| Test Group | Total Tests | Expected Pass | Expected Fail | Expected Skip |
|------------|-------------|---------------|---------------|---------------|
| Configuration Provider | 19 | 19 | 0 | 0 |
| Credential Provider (DPAPI) | 46 | 46 | 0 | 0 |
| Stock Watcher & Service | 52 | 52 | 0 | 0 |
| Notifications & Email | 32 | 32 | 0 | 0 |
| SMS & Localization | 38 | 38 | 0 | 0 |
| CLI & Configuration | 36 | 25 | 0 | 11 |
| **TOTAL** | **223** | **212** | **0** | **11** |

### Skipped Tests (11 total)

The following 11 tests are skipped because they require real HTTP services or are deferred to Phase 2+:
- Integration tests for real stock checking (HTTP API tests)
- Email notification integration tests
- SMS provider integration tests (Twilio, AWS SNS, Azure)
- Localization resource validation (deferred to Phase 2b)
- Service lifecycle and background task integration tests
- `GetProductsAsync_WithRealStore_ShouldReturnProducts`

---

## CI/CD Integration

For Windows-based CI/CD pipelines (Azure DevOps, GitHub Actions Windows runners):

```yaml
# Example GitHub Actions workflow
- name: Run tests on Windows
  run: dotnet test --logger "trx;LogFileName=test-results.trx"
  
- name: Publish test results
  uses: dorny/test-reporter@v1
  if: always()
  with:
    name: Windows Test Results
    path: '**/test-results.trx'
    reporter: dotnet-trx
```

---

## Performance Notes

- **First build**: 2-3 minutes (downloads dependencies)
- **Subsequent builds**: 10-20 seconds
- **Runtime**: ~100-200ms for a single stock check
- **Executable size**: ~50-60 MB (includes .NET runtime)
- **Memory usage**: ~40-50 MB when running

---

## Security Considerations

### DPAPI Encryption

- Credentials are encrypted using Windows DPAPI
- Keys are tied to your Windows user account
- Credentials cannot be decrypted by other users
- If the user profile is moved to another machine, credentials must be re-entered
- DPAPI provides strong security without additional key management

### File Permissions

- Configuration files stored in user's AppData folder
- Credentials encrypted with DPAPI before storage
- No additional file permission management needed on Windows

---

## Next Steps

### After Successful Testing

1. Update `TEST_RESULTS.md` with Windows test results
2. Commit changes to repository
3. Test on other platforms (macOS, Linux) for cross-platform validation

### If Critical Failures

1. Document issues in GitHub Issues
2. Fix Windows-specific bugs
3. Re-run test suite
4. Update credential provider if needed

### Phase 2 Preparation

- Test email notifications with Windows Mail/Outlook
- Test SMS notifications (if available)
- Begin internationalization infrastructure (Phase 2)

---

## Known Platform Differences

- **Line Endings**: Tests handle both CRLF (Windows) and LF (Unix) line endings
- **Case Sensitivity**: Windows file system is case-insensitive; tests account for this
- **Path Separators**: Code uses `Path.Combine()` for cross-platform compatibility
- **Credential Storage**: DPAPI on Windows vs file-based encryption on Unix

---

## Support

For issues specific to Windows:
- Check Windows Event Viewer for application errors
- Verify .NET SDK installation: `dotnet --info`
- Ensure Windows Defender or antivirus isn't blocking the application
- Review `TEST_RESULTS.md` for known issues

---

**Last Updated**: December 2025  
**Document Version**: 1.0 (Developer guide)  
**Phase**: Phase 1 Complete


