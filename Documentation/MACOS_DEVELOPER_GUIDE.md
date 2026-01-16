# macOS Developer Guide

**Purpose**: Complete guide for building, testing, and validating UnifiWatch on macOS  
**Platform**: macOS 10.15 or later (Intel or Apple Silicon)  
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

- **Operating System**: macOS 10.15 (Catalina) or later
- **Processor**: Intel or Apple Silicon (M1/M2/M3/M4)
- **.NET SDK**: Version 9.0.203 or later
- **Terminal**: Any terminal application
- **Keychain Access**: For credential storage testing

---

## Installation

### Step 1: Install .NET 9.0 SDK

#### Option A: Download from Microsoft (Recommended for specific version)

1. Visit: https://dotnet.microsoft.com/download/dotnet/9.0
2. Select the appropriate version:
   - **Intel Mac**: Download "Installer" for `osx-x64`
   - **Apple Silicon (M1/M2/M3/M4)**: Download "Installer" for `osx-arm64`
3. Run the installer and follow the prompts
4. Restart Terminal if it was open

**Direct download for .NET 9.0.203**:

```bash
# For Apple Silicon (M1/M2/M3/M4):
curl -L https://download.visualstudio.microsoft.com/download/pr/822078c4-86ed-4e8d-9cbd-dc15d05f030f/9ff0b5b3e93ddc34c0e04ca93baf2e18/dotnet-sdk-9.0.203-osx-arm64.pkg -o dotnet-sdk-9.0.203.pkg

# For Intel Macs:
# curl -L https://download.visualstudio.microsoft.com/download/pr/822078c4-86ed-4e8d-9cbd-dc15d05f030f/9ff0b5b3e93ddc34c0e04ca93baf2e18/dotnet-sdk-9.0.203-osx-x64.pkg -o dotnet-sdk-9.0.203.pkg

# Install the downloaded package
sudo installer -pkg dotnet-sdk-9.0.203.pkg -target /

# Verify installation
dotnet --version  # Should show 9.0.203
```

#### Option B: Install via Homebrew

If you have Homebrew installed (installs latest 9.x version):

```bash
brew install dotnet-sdk
dotnet --version  # Will show whatever latest version Homebrew has
```

### Step 2: Get the Source Code

#### Option A: Clone from GitHub

```bash
git clone https://github.com/johnmccrae/UnifiWatch.git
cd UnifiWatch
```

#### Option B: Copy from Windows Machine

- Use AirDrop, email, USB drive, or network share to transfer the `UnifiWatch` folder
- Extract to your preferred location (e.g., `~/Projects/UnifiWatch`)

### Step 3: Restore Dependencies

```bash
cd UnifiWatch
sudo dotnet restore UnifiWatch.sln
```

**Expected**: NuGet packages downloaded successfully

---

## Building the Application

### Development Build

For development and testing:

```bash
sudo dotnet build UnifiWatch.sln
```

**Expected Output**:
- Build succeeded
- 0 Error(s)
- 0 Warning(s)
- Build time: ~2-3 minutes on first build, ~10-20 seconds on subsequent builds

### Release Build (Standalone Executable)

**For Apple Silicon (M1/M2/M3/M4):**

```bash
dotnet publish UnifiWatch.csproj -c Release -r osx-arm64 --self-contained -p:PublishSingleFile=true
```

**For Intel Mac:**

```bash
dotnet publish UnifiWatch.csproj -c Release -r osx-x64 --self-contained -p:PublishSingleFile=true
```

**Output Location**:
- Apple Silicon: `bin/Release/net9.0/osx-arm64/publish/UnifiWatch`
- Intel: `bin/Release/net9.0/osx-x64/publish/UnifiWatch`

### Make Executable

```bash
# For Apple Silicon:
chmod +x bin/Release/net9.0/osx-arm64/publish/UnifiWatch

# For Intel:
chmod +x bin/Release/net9.0/osx-x64/publish/UnifiWatch
```

---

## Testing

### Quick Functional Tests

#### Test 1: Check Stock (Basic Functionality)

```bash
# For Apple Silicon:
./bin/Release/net9.0/osx-arm64/publish/UnifiWatch --stock --store USA --product-names "Dream Machine" | head -30

# For Intel:
./bin/Release/net9.0/osx-x64/publish/UnifiWatch --stock --store USA --product-names "Dream Machine" | head -30
```

**Expected Output**:
- `Getting Unifi products from USA store...`
- `Retrieved XXX products`
- Table of products with stock status

#### Test 2: Monitor Mode

```bash
./bin/Release/net9.0/osx-arm64/publish/UnifiWatch --wait --store USA --product-names "Dream Machine" --seconds 5
```

**Expected Behavior**:
- Checks for the product every 5 seconds
- Displays current stock status
- Press Ctrl+C to stop

#### Test 3: Test All Stores

**GraphQL API Stores (Modern):**

```bash
# USA
./bin/Release/net9.0/osx-arm64/publish/UnifiWatch --stock --store USA

# Europe
./bin/Release/net9.0/osx-arm64/publish/UnifiWatch --stock --store Europe

# UK
./bin/Release/net9.0/osx-arm64/publish/UnifiWatch --stock --store UK
```

**Shopify API Stores (Legacy):**

```bash
# Brazil
./bin/Release/net9.0/osx-arm64/publish/UnifiWatch --stock --legacy-api-store Brazil

# Japan
./bin/Release/net9.0/osx-arm64/publish/UnifiWatch --stock --legacy-api-store Japan
```

#### Test 4: macOS Notifications

When stock is found, macOS native notifications should appear in Notification Center.

**Manual notification test**:

```bash
cat > /tmp/test_notification.sh << 'EOF'
osascript -e 'display notification "Dream Machine Pro is now in stock!" with title "UniFi Stock Alert" subtitle "UnifiWatch"'
EOF

chmod +x /tmp/test_notification.sh
/tmp/test_notification.sh
```

**Expected**: Notification appears in top-right corner of screen

### Run All Unit Tests

```bash
sudo dotnet test UnifiWatch.sln
```

**Expected Results**:
  - Total: 223 tests
  - Passed: 212 ✅
  - Failed: 0 ❌
  - Skipped: 11 (integration/advanced tests)

---

## Phase 1 Validation Tests

These tests validate Phase 1 infrastructure: Configuration provider and credential storage.

### Setup for Phase 1 Tests

#### Configure macOS Keychain Permissions

- [ ] Open **Keychain Access.app** (`/Applications/Utilities/Keychain Access.app`)
- [ ] Verify login keychain is unlocked
- [ ] Be prepared for authorization prompts when tests store credentials

---

### Test Group 1: Configuration Provider

#### Test 1.1: Configuration File Location

```bash
sudo dotnet test UnifiWatch.sln --filter "ConfigurationProviderTests.LoadAsync_ValidConfiguration_LoadsAllSettings"
```

**Verification**:

- [ ] Test passes
- [ ] Config directory created:
  ```bash
  ls -la ~/Library/Application\ Support/UnifiWatch/
  ```
- [ ] File permissions correct:
  ```bash
  stat -f "%Lp" ~/Library/Application\ Support/UnifiWatch/config.json
  ```
  **Expected**: `600` (read/write for owner only)

#### Test 1.2: Configuration Save/Load Cycle

```bash
sudo dotnet test UnifiWatch.sln --filter "ConfigurationProviderTests.SaveAsync_ValidConfiguration_SavesCorrectly"
```

**Verification**:

- [ ] Test passes
- [ ] JSON file created with valid structure:
  ```bash
  cat ~/Library/Application\ Support/UnifiWatch/config.json
  ```
  **Expected**: Valid JSON with ServiceSettings, MonitoringSettings, NotificationSettings

#### Test 1.3: Configuration Backup

```bash
sudo dotnet test UnifiWatch.sln --filter "ConfigurationProviderTests.BackupAsync_CreatesBackupFile"
```

**Verification**:

- [ ] Test passes
- [ ] Backup file created:
  ```bash
  ls -l ~/Library/Application\ Support/UnifiWatch/*.backup.json
  ```
  **Expected**: Backup file with timestamp

---

### Test Group 2: macOS Keychain Credential Storage

#### Test 2.1: Platform Detection

```bash
sudo dotnet test UnifiWatch.sln --filter "CredentialProviderTests.CredentialProviderFactory_CreateProvider_WithAutoStorage_ShouldSelectDefault"
```

**Verification**:

- [ ] Test passes
- [ ] Platform correctly detected as macOS

#### Test 2.2: Keychain Integration (CRITICAL TEST)

**Important Note**: Unit tests use mocked providers and do NOT interact with the real macOS Keychain.

**Manual Keychain Test using `security` command**:

```bash
# Store a test credential in macOS Keychain
security add-generic-password -a "test@example.com" -s "UnifiWatch.Test" -w "TestPassword123" -U -T ""

# Retrieve the credential (you may be prompted to allow access)
security find-generic-password -a "test@example.com" -s "UnifiWatch.Test" -w

# Verify in Keychain Access app
open -a "Keychain Access"
# Search for "UnifiWatch.Test"

# Clean up - delete the test credential
security delete-generic-password -a "test@example.com" -s "UnifiWatch.Test"
```

**Expected Behavior**:

- [ ] `security add-generic-password` succeeds without errors
- [ ] `security find-generic-password` returns "TestPassword123"
- [ ] Keychain Access.app shows the "UnifiWatch.Test" entry
- [ ] You may see a prompt to allow Terminal to access Keychain (click "Allow" or "Always Allow")
- [ ] `security delete-generic-password` removes the credential successfully

**Run unit tests (test encrypted file fallback)**:

```bash
sudo dotnet test UnifiWatch.sln --filter "CredentialProviderTests.StoreAsync_WithValidKeyAndSecret_ShouldSucceed"
```

#### Test 2.3: Retrieve Non-Existent Credential

```bash
sudo dotnet test UnifiWatch.sln --filter "CredentialProviderTests.RetrieveAsync_WithNonExistentKey_ShouldReturnNull"
```

**Verification**:

- [ ] Test passes
- [ ] Error handling works (no crash, returns null gracefully)

#### Test 2.4: Update Existing Credential

```bash
sudo dotnet test UnifiWatch.sln --filter "CredentialProviderTests.StoreAsync_UpdateExistingKey_ShouldOverwrite"
```

**Verification**:

- [ ] Test passes
- [ ] Open **Keychain Access.app** and search for "UnifiWatch"
- [ ] Double-click entry → Should show updated password

#### Test 2.5: Delete Credential

```bash
sudo dotnet test UnifiWatch.sln --filter "CredentialProviderTests.DeleteAsync_AfterStore_ShouldSucceed"
```

**Verification**:

- [ ] Test passes
- [ ] Verify in Keychain Access.app that entry is removed

#### Test 2.6: List All Credential Keys

```bash
sudo dotnet test UnifiWatch.sln --filter "CredentialProviderTests.ListAsync_AfterMultipleStores_ShouldReturnAllKeys"
```

**Verification**:

- [ ] Test passes
- [ ] Multiple credentials can be stored and listed

---

### Test Group 3: Encrypted File Fallback

#### Test 3.1: Encrypted File Provider

The encrypted file fallback is used when:
1. Keychain access is denied
2. Or explicitly configured with `storageMethod="encrypted-file"`

```bash
sudo dotnet test UnifiWatch.sln --filter "CredentialProviderTests.StoreAsync_ThenRetrieveAsync_ShouldReturnSameValue"
```

**Verification**:

- [ ] Test passes

**Note**: Unit tests use temporary directories. In production, encrypted file would be at:
- `~/Library/Application Support/UnifiWatch/credentials.enc.json` with `600` permissions

**Manual Fallback Test (Optional, Advanced)**:
- Deny Terminal access to Keychain when prompted by the app
- App should automatically fall back to `EncryptedFileCredentialProvider`
- Credentials stored in `~/Library/Application Support/UnifiWatch/credentials.enc.json`

#### Test 3.2: AES-256-CBC Encryption

```bash
sudo dotnet test UnifiWatch.sln --filter "CredentialProviderTests.StoreAsync_ThenRetrieveAsync_ShouldReturnSameValue"
```

**Verification**:

- [ ] Test passes
- [ ] Encryption uses AES-256-CBC (not DPAPI - that's Windows only)
- [ ] Conditional compilation: `#if WINDOWS` should NOT trigger on macOS

---

### Test Group 4: Cross-Platform Paths

#### Test 4.1: Configuration Paths

```bash
sudo dotnet test UnifiWatch.sln --filter "ConfigurationProviderTests"
```

**Verify macOS-specific paths**:

- [ ] Config directory: `~/Library/Application Support/UnifiWatch/`
- [ ] Config file: `~/Library/Application Support/UnifiWatch/config.json`
- [ ] Backup files: `~/Library/Application Support/UnifiWatch/config.backup.*.json`

#### Test 4.2: Credential Paths

```bash
sudo dotnet test UnifiWatch.sln --filter "CredentialProviderTests"
```

**Verify macOS-specific paths**:

- [ ] Credential file: `~/Library/Application Support/UnifiWatch/credentials.enc.json`
- [ ] File permissions: `600` (read/write for owner only)

**Note**: This file is only created in production when using encrypted file storage.

---

### Test Group 5: Build & Compilation

#### Test 5.1: Clean Build

```bash
sudo dotnet clean UnifiWatch.sln
sudo dotnet build UnifiWatch.sln --configuration Release
```

**Verification**:

- [ ] 0 errors
- [ ] 0 warnings
- [ ] Conditional compilation works (`#if WINDOWS` blocks excluded on macOS)

#### Test 5.2: All Unit Tests

```bash
sudo dotnet test UnifiWatch.sln --configuration Release
```

**Expected Results**:

  - [ ] 212 tests passed ✅
  - [ ] 11 tests skipped (integration/advanced tests)
- [ ] 0 tests failed ❌

---

## Known Issues & Workarounds

### Issue: Keychain Access Prompts Too Frequently

**Symptom**: macOS prompts for Keychain access every time app runs

**Workaround**:
1. Open **Keychain Access.app**
2. Search for "UnifiWatch"
3. Right-click entry → "Get Info"
4. Go to "Access Control" tab
5. Select "Allow all applications to access this item"
6. Save changes

### Issue: Permission Denied for Keychain

**Symptom**: `SecKeychainAddGenericPassword` returns `errSecAuthFailed`

**Workaround**:
1. Check System Settings → Privacy & Security
2. Ensure Terminal/IDE has "Full Disk Access" or "Automation" permission
3. Restart Terminal/IDE after granting permission

### Issue: Tests Require sudo

**Symptom**: Some dotnet commands fail without sudo on macOS

**Explanation**:
- File permissions in `~/Library/Application Support/UnifiWatch/` require elevated access
- Directory and files created with restricted permissions (600) for security
- All `dotnet` commands require `sudo` prefix

---

## Troubleshooting

### "command not found: dotnet"

**Solution**:
- .NET is not installed or Terminal needs to be restarted
- Try: `brew install dotnet-sdk` or reinstall from Microsoft
- Restart Terminal

### "xcrun: error: unable to find utility"

**Solution**:
- Xcode Command Line Tools are missing
- Run: `xcode-select --install`

### "Permission denied"

**Solution**:
- The executable isn't marked as executable
- Run: `chmod +x bin/Release/net9.0/osx-arm64/publish/UnifiWatch`

### No notifications appearing

**Solution**:
- Check System Settings → Notifications
- Ensure notifications are enabled for "Terminal" or your shell
- Notifications work with native mode (`--wait`) when stock is found

### Application hangs

**Solution**:
- Press Ctrl+C to stop
- Check your internet connection
- Verify the store name is correct (case-sensitive)

---

## Test Results Summary

| Test Group | Total Tests | Expected Pass | Expected Fail | Expected Skip |
|------------|-------------|---------------|---------------|---------------|
| Configuration Provider | 19 | 19 | 0 | 0 |
| Credential Provider | 46 | 46 | 0 | 0 |
| Stock Watcher & Service | 52 | 52 | 0 | 0 |
| Notifications & Email | 32 | 32 | 0 | 0 |
| SMS & Localization | 38 | 38 | 0 | 0 |
| CLI & Configuration | 36 | 25 | 0 | 11 |
| **TOTAL** | **223** | **212** | **0** | **11** |

**After Testing, Verify**:

- ✅ Build completes without errors
- ✅ `--stock` mode retrieves products from the store
- ✅ Product filtering works (`--product-names`, `--product-skus`)
- ✅ `--wait` mode monitors and counts down
- ✅ Native macOS notifications appear when monitored products are in stock
- ✅ Ctrl+C stops the monitoring gracefully
- ✅ Unit tests pass (65/71, 6 skipped)
- ✅ Keychain integration works (manual test with `security` command)
- ✅ Configuration files created in correct location with correct permissions

---

## Performance Notes

- **First build**: 2-3 minutes (downloads dependencies)
- **Subsequent builds**: 10-20 seconds
- **Runtime**: ~100-200ms for a single stock check
- **Executable size**: ~50-60 MB (includes .NET runtime)
- **Memory usage**: ~40-50 MB when running

---

## Next Steps

### After Successful Testing

1. Update `TEST_RESULTS.md` with macOS test results
2. Commit changes to repository
3. Proceed to Linux testing (see `LINUX_BUILD_AND_TEST.md`)

### If Critical Failures

1. Document issues in GitHub Issues
2. Fix macOS-specific bugs
3. Re-run test suite
4. Update `MacOsKeychain.cs` if needed

### Phase 2 Preparation

- Test email notifications with macOS Mail.app
- Test SMS notifications (if available)
- Begin internationalization infrastructure (Phase 2)

---

## Optional: Install Globally

To run the application from anywhere, copy it to a location in your PATH:

```bash
# Option 1: Copy to /usr/local/bin
sudo cp bin/Release/net9.0/osx-arm64/publish/UnifiWatch /usr/local/bin/

# Option 2: Create a symlink
sudo ln -s $(pwd)/bin/Release/net9.0/osx-arm64/publish/UnifiWatch /usr/local/bin/

# Now you can run it from anywhere:
UnifiWatch --stock --store USA
```

---

## Getting Help

If you encounter issues:

1. Check the [Troubleshooting](#troubleshooting) section above
2. Verify .NET 9.0 is installed: `dotnet --version`
3. Ensure you're on the correct Mac architecture (Intel vs Apple Silicon)
4. Run `sudo dotnet test UnifiWatch.sln` to check if the test suite passes
5. Check internet connectivity when running API calls
6. Review `TEST_RESULTS.md` for known issues

---

**Last Updated**: December 2025  
**Document Version**: 1.0 (Developer guide)  
**Phase**: Phase 1 Complete


