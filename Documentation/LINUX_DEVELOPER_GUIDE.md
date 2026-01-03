# Linux Developer Guide

**Audience**: Developers and contributors building and running the automated test suite

**Purpose**: Complete guide for building, testing, and validating UnifiWatch on Linux  
**Platform**: Linux (Ubuntu 20.04+, Fedora 35+, Debian 11+, or equivalent)  
**Phase**: Phase 1 - Configuration & Credential Infrastructure  
**Last Updated**: December 2025

---

## Table of Contents

1. [Prerequisites](#prerequisites)
2. [Installation](#installation)
3. [Building the Application](#building-the-application)
4. [Testing](#testing)
5. [Phase 1 Validation Tests](#phase-1-validation-tests)
6. [Known Issues & Workarounds](#known-issues--workarounds)
7. [Troubleshooting](#troubleshooting)

---

## Prerequisites

- **Operating System**: Linux distribution (Ubuntu 20.04+, Fedora 35+, Debian 11+, or equivalent)
- **.NET SDK**: Version 9.0 or later
- **Bash/Zsh**: Standard shell (included in all Linux distributions)
- **Credential Storage**: GNOME Keyring, KDE Wallet, or libsecret (optional but recommended)

---

## Installation

### Step 1: Install .NET 9.0 SDK

#### Ubuntu/Debian

```bash
# Add Microsoft package repository
wget https://packages.microsoft.com/config/ubuntu/$(lsb_release -rs)/packages-microsoft-prod.deb -O packages-microsoft-prod.deb
sudo dpkg -i packages-microsoft-prod.deb
rm packages-microsoft-prod.deb

# Install .NET SDK
sudo apt-get update
sudo apt-get install -y dotnet-sdk-9.0
```

#### Fedora

```bash
# Install .NET SDK
sudo dnf install dotnet-sdk-9.0
```

#### Other Distributions

Visit: https://dotnet.microsoft.com/download/dotnet/9.0 for distribution-specific instructions

**Verify Installation**:

```bash
dotnet --version
```

**Expected**: `9.0.x` or later

### Step 2: Get the Source Code

#### Option A: Clone from GitHub

```bash
git clone https://github.com/johnmccrae/UnifiWatch.git
cd UnifiWatch
```

#### Option B: Download ZIP

```bash
wget https://github.com/johnmccrae/UnifiWatch/archive/refs/heads/main.zip
unzip main.zip
cd UnifiWatch-main
```

### Step 3: Restore Dependencies

```bash
cd UnifiWatch
dotnet restore UnifiWatch.sln
```

**Expected**: NuGet packages downloaded successfully

---

## Building the Application

### Development Build

For development and testing:

```bash
dotnet build UnifiWatch.sln
```

**Expected Output**:
- Build succeeded
- 0 Error(s)
- 0 Warning(s)
- Build time: ~2-3 minutes on first build, ~10-20 seconds on subsequent builds

### Release Build

```bash
dotnet build UnifiWatch.sln --configuration Release
```

### Publish Standalone Executable

```bash
dotnet publish UnifiWatch.csproj -c Release -r linux-x64 --self-contained -p:PublishSingleFile=true
```

**Output Location**: `bin/Release/net9.0/linux-x64/publish/UnifiWatch`

---

## Testing

### Quick Functional Tests

#### Test 1: Check Stock (Basic Functionality)

```bash
./bin/Release/net9.0/linux-x64/publish/UnifiWatch --stock --store USA --product-names "Dream Machine"
```

**Expected Output**:
- `Getting Unifi products from USA store...`
- `Retrieved XXX products`
- Table of products with stock status

#### Test 2: Monitor Mode

```bash
./bin/Release/net9.0/linux-x64/publish/UnifiWatch --wait --store USA --product-names "Dream Machine" --seconds 5
```

**Expected Behavior**:
- Checks for the product every 5 seconds
- Displays current stock status
- Press Ctrl+C to stop

#### Test 3: Test All Stores

**GraphQL API Stores (Modern):**

```bash
# USA
./bin/Release/net9.0/linux-x64/publish/UnifiWatch --stock --store USA

# Europe
./bin/Release/net9.0/linux-x64/publish/UnifiWatch --stock --store Europe

# UK
./bin/Release/net9.0/linux-x64/publish/UnifiWatch --stock --store UK
```

**Shopify API Stores (Legacy):**

```bash
# Brazil
./bin/Release/net9.0/linux-x64/publish/UnifiWatch --stock --legacy-api-store Brazil

# Japan
./bin/Release/net9.0/linux-x64/publish/UnifiWatch --stock --legacy-api-store Japan
```

### Run All Unit Tests

```bash
dotnet test UnifiWatch.sln
```

**Expected Results**:
- Total: 71 tests
- Passed: 65 ✅
- Failed: 0 ❌
- Skipped: 6 (integration tests)

**Last Tested**: December 7, 2025  
**Platform**: Linux  
**SDK Version**: .NET 9.0

---

## Phase 1 Validation Tests

These tests validate Phase 1 infrastructure: Configuration provider and credential storage.

### Linux-Specific Notes

#### Credential Storage

Linux supports multiple credential storage backends:
- **GNOME Keyring**: For GNOME desktop environments
- **KDE Wallet**: For KDE Plasma desktop environments
- **libsecret**: Cross-desktop secret storage API
- **Encrypted File**: Fallback when no keyring is available

**To use native keyring storage:**

```bash
# Ubuntu/Debian (GNOME Keyring)
sudo apt-get install gnome-keyring libsecret-1-0

# Fedora (GNOME Keyring)
sudo dnf install gnome-keyring libsecret

# For headless servers, use encrypted file storage (automatic fallback)
```

#### File Paths

Linux uses XDG Base Directory specification:
- Configuration directory: `~/.config/UnifiWatch` or `$XDG_CONFIG_HOME/UnifiWatch`
- Data directory: `~/.local/share/UnifiWatch` or `$XDG_DATA_HOME/UnifiWatch`
- Credentials file (fallback): `~/.local/share/UnifiWatch/credentials.enc.json`

---

### Test Group 1: Configuration Provider

#### Test 1.1: Configuration File Location

```bash
dotnet test UnifiWatch.sln --filter "ConfigurationProviderTests.LoadAsync_ValidConfiguration_LoadsAllSettings"
```

**Verification**:

- [ ] Test passes
- [ ] Config directory created:
  ```bash
  test -d ~/.config/UnifiWatch && echo "Config dir exists" || echo "Config dir missing"
  ```
  **Expected**: `Config dir exists`
- [ ] Config file exists:
  ```bash
  test -f ~/.config/UnifiWatch/config.json && echo "Config file exists" || echo "Config file missing"
  ```
  **Expected**: `Config file exists`

#### Test 1.2: Configuration Save/Load Cycle

```bash
dotnet test UnifiWatch.sln --filter "ConfigurationProviderTests.SaveAsync_ValidConfiguration_SavesCorrectly"
```

**Verification**:

- [ ] Test passes
- [ ] JSON file created with valid structure:
  ```bash
  cat ~/.config/UnifiWatch/config.json
  ```
  **Expected**: Valid JSON with ServiceSettings, MonitoringSettings, NotificationSettings

#### Test 1.3: Configuration Backup

```bash
dotnet test UnifiWatch.sln --filter "ConfigurationProviderTests.BackupAsync_CreatesBackupFile"
```

**Verification**:

- [ ] Test passes
- [ ] Backup file created:
  ```bash
  ls -la ~/.config/UnifiWatch/*.backup.json
  ```
  **Expected**: Backup file with timestamp

---

### Test Group 2: Linux Credential Storage

#### Test 2.1: Platform Detection

```bash
dotnet test UnifiWatch.sln --filter "CredentialProviderTests.CredentialProviderFactory_CreateProvider_WithAutoStorage_ShouldSelectDefault"
```

**Verification**:

- [ ] Test passes
- [ ] Platform correctly detected as Linux

#### Test 2.2: Credential Provider Selection

Linux credential provider uses this priority:
1. **secret-service** (GNOME Keyring/KDE Wallet) if available
2. **encrypted-file** fallback with user-specific encryption

```bash
dotnet test UnifiWatch.sln --filter "CredentialProviderTests"
```

**Expected**: All 46 credential provider tests should pass

**Linux Security Features**:
- Keyring credentials encrypted by system keyring daemon
- File-based credentials encrypted using .NET Data Protection API with machine key
- Credentials tied to user account
- File permissions set to 600 (user read/write only)

#### Test 2.3: Verify Credential Encryption

To verify credential encryption is working:

```bash
# Run credential tests
dotnet test UnifiWatch.sln --filter "FullyQualifiedName~CredentialProviderTests"

# Check if keyring is being used
echo "Checking for secret-service availability..."
which secret-tool && echo "Keyring available" || echo "Using encrypted file"

# Verify encrypted file exists (if using file storage)
test -f ~/.local/share/UnifiWatch/credentials.enc.json && \
  echo "Encrypted credentials file exists"

# Verify file is encrypted (not plaintext)
if [ -f ~/.local/share/UnifiWatch/credentials.enc.json ]; then
  cat ~/.local/share/UnifiWatch/credentials.enc.json
  # Should show encrypted/binary data, not readable JSON
fi
```

#### Test 2.4: Retrieve Non-Existent Credential

```bash
dotnet test UnifiWatch.sln --filter "CredentialProviderTests.RetrieveAsync_WithNonExistentKey_ShouldReturnNull"
```

**Verification**:

- [ ] Test passes
- [ ] Error handling works (no crash, returns null gracefully)

#### Test 2.5: Update Existing Credential

```bash
dotnet test UnifiWatch.sln --filter "CredentialProviderTests.StoreAsync_UpdateExistingKey_ShouldOverwrite"
```

**Verification**:

- [ ] Test passes
- [ ] Credential updated successfully

#### Test 2.6: Delete Credential

```bash
dotnet test UnifiWatch.sln --filter "CredentialProviderTests.DeleteAsync_AfterStore_ShouldSucceed"
```

**Verification**:

- [ ] Test passes
- [ ] Credential removed from storage

#### Test 2.7: List All Credential Keys

```bash
dotnet test UnifiWatch.sln --filter "CredentialProviderTests.ListAsync_AfterMultipleStores_ShouldReturnAllKeys"
```

**Verification**:

- [ ] Test passes
- [ ] Multiple credentials can be stored and listed

---

### Test Group 3: Cross-Platform Path Handling

#### Test 3.1: Configuration Paths

```bash
dotnet test UnifiWatch.sln --filter "ConfigurationProviderTests"
```

**Verify Linux-specific paths**:

- [ ] Config directory: `~/.config/UnifiWatch/`
- [ ] Config file: `~/.config/UnifiWatch/config.json`
- [ ] Backup files: `~/.config/UnifiWatch/config.backup.*.json`

#### Test 3.2: Credential Paths

```bash
dotnet test UnifiWatch.sln --filter "CredentialProviderTests"
```

**Verify Linux-specific paths** (when using encrypted file):

- [ ] Credential file: `~/.local/share/UnifiWatch/credentials.enc.json`

---

### Test Group 4: Build & Compilation

#### Test 4.1: Clean Build

```bash
dotnet clean UnifiWatch.sln
dotnet build UnifiWatch.sln --configuration Release
```

**Verification**:

- [ ] 0 errors
- [ ] 0 warnings
- [ ] Conditional compilation works (`#if LINUX` blocks included on Linux)

#### Test 4.2: All Unit Tests

```bash
dotnet test UnifiWatch.sln --configuration Release
```

**Expected Results**:

- [ ] 65 tests passed ✅
- [ ] 6 tests skipped (integration tests)
- [ ] 0 tests failed ❌

---

## Running Tests with Coverage

Generate test coverage report:

```bash
dotnet test --collect:"XPlat Code Coverage"
```

---

## Running Specific Test Categories

Run only unit tests:
```bash
dotnet test --filter Category=Unit
```

Run only integration tests (requires network):
```bash
dotnet test --filter Category=Integration
```

---

## Performance Testing

Run tests with detailed timing:

```bash
dotnet test --logger "console;verbosity=detailed"
```

---

## Known Issues & Workarounds

### Issue: "Permission denied" errors

**Symptom**: Cannot create files in system directories or execute published binary

**Solution**: 
```bash
# Make executable
chmod +x bin/Release/net9.0/linux-x64/publish/UnifiWatch

# Or run with sudo if testing service installation
sudo ./UnifiWatch --install-service
```

### Issue: Tests fail with path errors

**Symptom**: Path separator errors or file not found

**Solution**: 
- Ensure paths use forward slashes (`/`)
- Verify the application correctly detects Linux as the platform
- Use `Path.Combine()` in code for cross-platform compatibility

### Issue: Keyring/secret-service not available

**Symptom**: Application falls back to encrypted file storage

**Solution**: 
```bash
# Install keyring support (Ubuntu/Debian)
sudo apt-get install gnome-keyring libsecret-1-0

# Install keyring support (Fedora)
sudo dnf install gnome-keyring libsecret

# For headless servers, encrypted file storage is recommended
```

### Issue: Port conflicts when testing

**Symptom**: Tests fail with port already in use errors

**Solution**: 
```bash
# Find processes using the port
sudo netstat -tlnp | grep :PORT
# Or
sudo lsof -i :PORT

# Kill the process if necessary
sudo kill -9 PID
```

---

## Clean Test Environment

Remove test artifacts:

```bash
rm -rf bin obj
rm -rf UnifiWatch.Tests/bin UnifiWatch.Tests/obj
rm -rf ~/.config/UnifiWatch
rm -rf ~/.local/share/UnifiWatch
```

---

## Troubleshooting

### "command not found: dotnet"

**Solution**:
- .NET SDK is not installed or not in PATH
- Install from https://dotnet.microsoft.com/download/dotnet/9.0
- Verify PATH: `echo $PATH`
- Add to PATH if needed: `export PATH=$PATH:$HOME/.dotnet`

### Build errors with NuGet packages

**Solution**:
```bash
# Clear NuGet cache
dotnet nuget locals all --clear

# Restore packages
dotnet restore

# Rebuild
dotnet build
```

### Tests timeout or hang

**Solution**:
- Check internet connectivity: `ping 8.8.8.8`
- Verify firewall isn't blocking: `sudo ufw status`
- Check DNS resolution: `nslookup github.com`

### Permission errors on systemd service installation

**Solution**:
```bash
# Service installation requires sudo
sudo ./UnifiWatch --install-service

# Verify service created
sudo systemctl status unifiwatch
```

### Missing dependencies

**Solution**:
```bash
# Ubuntu/Debian - install common dependencies
sudo apt-get install -y libc6 libgcc1 libgssapi-krb5-2 libicu70 libssl3 libstdc++6 zlib1g

# Fedora - install common dependencies
sudo dnf install -y compat-openssl11 krb5-libs libicu openssl-libs zlib
```

---

## Test Results Summary

| Test Group | Total Tests | Expected Pass | Expected Fail | Expected Skip |
|------------|-------------|---------------|---------------|---------------|
| Configuration Provider | 19 | 19 | 0 | 0 |
| Credential Provider | 46 | 46 | 0 | 0 |
| Build & Compilation | 2 | 2 | 0 | 0 |
| Integration Tests | 6 | 0 | 0 | 6 |
| **TOTAL** | **71** | **65** | **0** | **6** |

### Skipped Tests

The following tests are skipped because they require real HTTP services:
- `Main_WithStoreOption_ShouldStartMonitoring`
- `Main_WithLegacyApiStoreOption_ShouldStartMonitoring`
- `Main_WithCheckNowOption_ShouldCheckOnce`
- `Main_WithCheckNowAndNoSoundOptions_ShouldCheckOnceWithoutSound`
- `Main_WithNoSoundOption_ShouldSucceed`
- `GetProductsAsync_WithRealStore_ShouldReturnProducts`

---

## CI/CD Integration

For Linux-based CI/CD pipelines (GitHub Actions, GitLab CI, Jenkins):

```yaml
# Example GitHub Actions workflow
jobs:
  test:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v3
      - name: Setup .NET
        uses: actions/setup-dotnet@v3
        with:
          dotnet-version: '9.0.x'
      - name: Restore dependencies
        run: dotnet restore
      - name: Build
        run: dotnet build --no-restore
      - name: Test
        run: dotnet test --no-build --verbosity normal --logger "trx;LogFileName=test-results.trx"
      - name: Publish test results
        uses: dorny/test-reporter@v1
        if: always()
        with:
          name: Linux Test Results
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

### Credential Encryption

**With Keyring (GNOME/KDE):**
- Credentials stored in system keyring encrypted by keyring daemon
- Access controlled by desktop session authentication
- Credentials automatically locked when session ends

**With Encrypted File (Headless):**
- Credentials encrypted using .NET Data Protection API
- Encryption key derived from machine-specific identifier
- File permissions set to 600 (user read/write only)
- Credentials cannot be decrypted on different machines without key migration

### File Permissions

```bash
# Verify secure permissions on credential file
ls -la ~/.local/share/UnifiWatch/credentials.enc.json
# Should show: -rw------- (600)

# Verify config directory permissions
ls -ld ~/.config/UnifiWatch
# Should show: drwx------ (700) recommended
```

---

## Next Steps

### After Successful Testing

1. Update `TEST_RESULTS.md` with Linux test results
2. Commit changes to repository
3. Test on other platforms (macOS, Windows) for cross-platform validation

### If Critical Failures

1. Document issues in GitHub Issues
2. Fix Linux-specific bugs
3. Re-run test suite
4. Update credential provider if needed

### Phase 2 Preparation

- Test email notifications with common Linux mail clients
- Test SMS notifications (if available)
- Begin internationalization infrastructure (Phase 2)

---

## Known Platform Differences

- **Line Endings**: Tests handle both CRLF (Windows) and LF (Unix) line endings
- **Case Sensitivity**: Linux file system is case-sensitive; tests account for this
- **Path Separators**: Code uses `Path.Combine()` for cross-platform compatibility
- **Credential Storage**: Keyring/encrypted file on Linux vs DPAPI on Windows
- **Service Management**: systemd on Linux vs Windows Service Manager

---

## Support

For issues specific to Linux:
- Check system logs: `sudo journalctl -xe`
- Verify .NET SDK installation: `dotnet --info`
- Check systemd service logs: `sudo journalctl -u unifiwatch`
- Review `TEST_RESULTS.md` for known issues

---

**Last Updated**: December 2025  
**Document Version**: 1.0 (Developer guide)  
**Phase**: Phase 1 Complete
