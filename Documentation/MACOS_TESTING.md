# macOS Testing Checklist

**Purpose**: Validate Phase 1 credential and configuration functionality on macOS
**Platform**: macOS (tested on: ___________ version ______)
**Date**: December 2025
**Tester**: _____________

## Pre-Testing Setup

### 1. Development Environment
- [ ] Install .NET 9.0 SDK for macOS (version 9.0.203 specifically)

  **Option A: Direct download from Microsoft (for specific version)**
  ```bash
  # Download .NET 9.0.203 SDK for your architecture
  # For Apple Silicon (M1/M2/M3/M4):
  curl -L https://download.visualstudio.microsoft.com/download/pr/822078c4-86ed-4e8d-9cbd-dc15d05f030f/9ff0b5b3e93ddc34c0e04ca93baf2e18/dotnet-sdk-9.0.203-osx-arm64.pkg -o dotnet-sdk-9.0.203.pkg

  # For Intel Macs:
  # curl -L https://download.visualstudio.microsoft.com/download/pr/822078c4-86ed-4e8d-9cbd-dc15d05f030f/9ff0b5b3e93ddc34c0e04ca93baf2e18/dotnet-sdk-9.0.203-osx-x64.pkg -o dotnet-sdk-9.0.203.pkg

  # Install the downloaded package
  sudo installer -pkg dotnet-sdk-9.0.203.pkg -target /

  # Verify installation
  dotnet --version  # Should show 9.0.203
  ```

  **Option B: Using Homebrew (installs latest 9.x version, not guaranteed to be 9.0.203)**

  ```bash
  brew install dotnet-sdk
  dotnet --version  # Will show whatever latest version Homebrew has
  ```

- [ ] Clone repository

  ```bash
  git clone https://github.com/EvotecIT/UnifiWatch.git
  cd UnifiWatch
  ```

- [ ] Restore NuGet packages

  ```bash
  sudo dotnet restore UnifiWatch.sln
  ```

- [ ] Build project

  ```bash
  sudo dotnet build UnifiWatch.sln
  ```

  **Expected**: 0 errors, 0 warnings

### 2. macOS Keychain Permissions

- [ ] Open **Keychain Access.app** (`/Applications/Utilities/Keychain Access.app`)
- [ ] Verify login keychain is unlocked
- [ ] Be prepared for authorization prompts when app stores credentials

## Critical Test Cases

### Test Group 1: Configuration Provider

#### Test 1.1: Configuration File Location

```bash
sudo dotnet test UnifiWatch.sln --filter "ConfigurationProviderTests.LoadAsync_ValidConfiguration_LoadsAllSettings"
```

- [ ] **PASS**: Test passes
- [ ] Verify config directory created at:

  ```bash
  ls -la ~/Library/Application\ Support/UnifiWatch/
  ```

  **Expected**: Directory exists

- [ ] Check file permissions:

  ```bash
  stat -f "%Lp" ~/Library/Application\ Support/UnifiWatch/config.json
  ```

  **Expected**: `600` (read/write for owner only)

#### Test 1.2: Configuration Save/Load Cycle

```bash
sudo dotnet test UnifiWatch.sln --filter "ConfigurationProviderTests.SaveAsync_ValidConfiguration_SavesCorrectly"
```

- [ ] **PASS**: Test passes
- [ ] Verify JSON file created:

  ```bash
  cat ~/Library/Application\ Support/UnifiWatch/config.json
  ```
  **Expected**: Valid JSON with ServiceSettings, MonitoringSettings, NotificationSettings

#### Test 1.3: Configuration Backup

```bash
sudo dotnet test UnifiWatch.sln --filter "ConfigurationProviderTests.BackupAsync_CreatesBackupFile"
```

- [ ] **PASS**: Test passes
- [ ] Verify backup file created:

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

- [ ] **PASS**: Test passes
- [ ] Verify runtime platform:

  ```bash
  sudo dotnet run --project UnifiWatch.csproj -- --version
  # Should detect macOS
  ```

#### Test 2.2: Store Credential to Keychain (CRITICAL TEST)

**Note**: The unit tests use mocked providers and do NOT interact with the real macOS Keychain. To manually test the actual Keychain integration:

**Manual Keychain Test using macOS `security` command**:

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
- [ ] You may see a prompt to allow Terminal to access Keychain (click "Allow")
- [ ] `security delete-generic-password` removes the credential successfully

**Run the unit tests (these test the encrypted file fallback, not the actual Keychain)**:

```bash
sudo dotnet test UnifiWatch.sln --filter "CredentialProviderTests.StoreAsync_WithValidKeyAndSecret_ShouldSucceed"
```

**If Prompts Appear Multiple Times**:

- [ ] Click "Always Allow" to avoid future prompts
- [ ] Check Keychain Access.app → Search "UnifiWatch" → Verify entry exists

**If Authorization Denied**:

- [ ] Check System Settings → Privacy & Security → Automation
- [ ] Ensure Terminal (or your IDE) has permission
- [ ] You may need to grant "Full Disk Access" to your terminal application

#### Test 2.3: Retrieve Non-Existent Credential

```bash
sudo dotnet test UnifiWatch.sln --filter "CredentialProviderTests.RetrieveAsync_WithNonExistentKey_ShouldReturnNull"
```

- [ ] **PASS**: Test passes
- [ ] Verify error handling (no crash, returns null gracefully)

#### Test 2.4: Update Existing Credential

```bash
sudo dotnet test UnifiWatch.sln --filter "CredentialProviderTests.StoreAsync_UpdateExistingKey_ShouldOverwrite"
```

- [ ] **PASS**: Test passes
- [ ] Open **Keychain Access.app**
- [ ] Search for "UnifiWatch"
- [ ] Double-click entry → Should show updated password

#### Test 2.5: Delete Credential

```bash
sudo dotnet test UnifiWatch.sln --filter "CredentialProviderTests.DeleteAsync_AfterStore_ShouldSucceed"
```

- [ ] **PASS**: Test passes
- [ ] Verify in Keychain Access.app that entry is removed

#### Test 2.6: List All Credential Keys

```bash
sudo dotnet test UnifiWatch.sln --filter "CredentialProviderTests.ListAsync_AfterMultipleStores_ShouldReturnAllKeys"
```

- [ ] **PASS**: Test passes
- [ ] Verify multiple credentials can be stored and listed

---

### Test Group 3: Fallback to Encrypted File Provider

#### Test 3.1: Encrypted File Provider

**Note**: The encrypted file fallback is only used when:

1. Keychain access is denied
2. Or explicitly configured with `storageMethod="encrypted-file"`

**Test the encrypted file provider directly:**

```bash
# Run the encrypted file provider test
sudo dotnet test UnifiWatch.sln --filter "CredentialProviderTests.StoreAsync_ThenRetrieveAsync_ShouldReturnSameValue"
```

- [ ] **PASS**: Test passes

**Note**: The unit tests don't persist files to the production directory - they use temporary test directories and clean up after themselves. The encrypted file would only be created in production when:

1. The app runs with `storageMethod="encrypted-file"` 
2. Or when Keychain access is denied and it falls back automatically
3. The file would be located at: `~/Library/Application Support/UnifiWatch/credentials.enc.json` with `600` permissions

**To manually test Keychain fallback** (optional, advanced):

- Deny Terminal access to Keychain when prompted by the app
- App should automatically fall back to `EncryptedFileCredentialProvider`
- Credentials will be stored in `~/Library/Application Support/UnifiWatch/credentials.enc.json`

#### Test 3.2: AES-256-CBC Encryption on macOS

```bash
sudo dotnet test UnifiWatch.sln --filter "CredentialProviderTests.StoreAsync_ThenRetrieveAsync_ShouldReturnSameValue"
```

- [ ] **PASS**: Test passes
- [ ] Verify encryption uses AES-256-CBC (not DPAPI - that's Windows only)
- [ ] Check conditional compilation: `#if WINDOWS` should NOT trigger on macOS

---

### Test Group 4: Cross-Platform Path Handling

#### Test 4.1: Configuration Directory

```bash
sudo dotnet test UnifiWatch.sln --filter "ConfigurationProviderTests"
```

**Verify Paths**:
- [ ] Config directory: `~/Library/Application Support/UnifiWatch/`
- [ ] Config file: `~/Library/Application Support/UnifiWatch/config.json`
- [ ] Backup files: `~/Library/Application Support/UnifiWatch/config.backup.*.json`

#### Test 4.2: Credential Directory
```bash
sudo dotnet test UnifiWatch.sln --filter "CredentialProviderTests"
```

**Verify Paths**:
- [ ] Credential file: `~/Library/Application Support/UnifiWatch/credentials.enc.json`
- [ ] File permissions: `600` (read/write for owner only)

**Note**: This file is only created in production when using encrypted file storage, not during unit tests which use temporary directories.

---

### Test Group 5: Build & Compilation

#### Test 5.1: Clean Build
```bash
sudo dotnet clean UnifiWatch.sln
sudo dotnet build UnifiWatch.sln --configuration Release
```

- [ ] **0 errors**
- [ ] **0 warnings**
- [ ] Conditional compilation works (`#if WINDOWS` blocks excluded on macOS)

#### Test 5.2: All Unit Tests
```bash
sudo dotnet test UnifiWatch.sln --configuration Release
```

**Expected Results**:
- [ ] **65 tests passed** ✅
- [ ] **6 tests skipped** (integration tests)
- [ ] **0 tests failed** ❌

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
1. Check System Preferences → Security & Privacy
2. Ensure Terminal/IDE has "Full Disk Access" or "Automation" permission
3. Restart Terminal/IDE after granting permission

### Issue: Tests Running with sudo
**Symptom**: Some dotnet commands fail without sudo on macOS

**Workaround**:
- All `dotnet` commands in this test suite require `sudo` prefix on macOS
- This is due to file permissions in `~/Library/Application Support/UnifiWatch/`
- The directory and files are created with restricted permissions (600) for security

---

## Test Results Summary

**Date**: _______________  
**macOS Version**: _______________  
**Tester**: _______________

| Test Group | Tests Passed | Tests Failed | Notes |
|------------|--------------|--------------|-------|
| Configuration Provider | __ / 19 | __ | |
| Keychain Credential Storage | __ / 46 | __ | |
| Encrypted File Fallback | __ / 5 | __ | |
| Cross-Platform Paths | __ / 10 | __ | |
| Build & Compilation | __ / 2 | __ | |
| **TOTAL** | **__ / 82** | **__** | |

---

## Issues Found

**Document any failures, crashes, or unexpected behavior**:

1. **Issue**: _______________________________________________
   - **Severity**: Critical / High / Medium / Low
   - **Steps to Reproduce**: _______________________________
   - **Expected**: ________________________________________
   - **Actual**: __________________________________________
   - **Workaround**: ______________________________________

2. **Issue**: _______________________________________________
   - **Severity**: Critical / High / Medium / Low
   - **Steps to Reproduce**: _______________________________
   - **Expected**: ________________________________________
   - **Actual**: __________________________________________
   - **Workaround**: ______________________________________

---

## Screenshots / Logs

**Attach screenshots of**:
- [ ] Keychain Access showing stored credentials
- [ ] Terminal output of successful test runs
- [ ] Any error messages or crashes

**Attach logs**:
- [ ] `dotnet test` output (full)
- [ ] Any crash logs from Console.app

---

## Sign-Off

**Phase 1 macOS Testing**: ✅ PASS / ❌ FAIL  

**Blocker Issues**: __ (number)  
**Non-Blocker Issues**: __ (number)  

**Ready for Production on macOS**: YES / NO  

**Tester Signature**: _______________  
**Date**: _______________

---

## Next Steps After macOS Testing

1. **If all tests pass**:
   - Update `TEST_RESULTS.md` with macOS results
   - Commit changes to repository
   - Proceed to Linux testing (Ubuntu/Debian)

2. **If critical failures**:
   - Document issues in GitHub Issues
   - Fix macOS-specific bugs
   - Re-run test suite
   - Update `MacOsKeychain.cs` if needed

3. **Phase 2 Preparation**:
   - Test email notifications with macOS Mail.app
   - Test SMS notifications (if available)
   - Begin internationalization infrastructure (Phase 2)
