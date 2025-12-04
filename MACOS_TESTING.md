# macOS Testing Checklist

**Purpose**: Validate Phase 1 credential and configuration functionality on macOS  
**Platform**: macOS (tested on: ___________ version _______)  
**Date**: December 2025  
**Tester**: _____________

## Pre-Testing Setup

### 1. Development Environment
- [ ] Install .NET 9.0 SDK for macOS
  ```bash
  brew install dotnet-sdk
  dotnet --version  # Should show 9.0.x
  ```

- [ ] Clone repository
  ```bash
  git clone https://github.com/EvotecIT/UnifiStockTracker.git
  cd UnifiStockTracker/UnifiStockTracker-CSharp
  ```

- [ ] Restore NuGet packages
  ```bash
  dotnet restore
  ```

- [ ] Build project
  ```bash
  dotnet build
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
dotnet test --filter "ConfigurationProviderTests.LoadAsync_ValidConfiguration_LoadsAllSettings"
```

- [ ] **PASS**: Test passes
- [ ] Verify config directory created at:
  ```bash
  ls -la ~/Library/Application\ Support/UnifiStockTracker/
  ```
  **Expected**: Directory exists

- [ ] Check file permissions:
  ```bash
  stat -f "%Lp" ~/Library/Application\ Support/UnifiStockTracker/config.json
  ```
  **Expected**: `600` (read/write for owner only)

#### Test 1.2: Configuration Save/Load Cycle
```bash
dotnet test --filter "ConfigurationProviderTests.SaveAsync_ValidConfiguration_SavesCorrectly"
```

- [ ] **PASS**: Test passes
- [ ] Verify JSON file created:
  ```bash
  cat ~/Library/Application\ Support/UnifiStockTracker/config.json
  ```
  **Expected**: Valid JSON with ServiceSettings, MonitoringSettings, NotificationSettings

#### Test 1.3: Configuration Backup
```bash
dotnet test --filter "ConfigurationProviderTests.BackupAsync_CreatesBackupFile"
```

- [ ] **PASS**: Test passes
- [ ] Verify backup file created:
  ```bash
  ls -l ~/Library/Application\ Support/UnifiStockTracker/*.backup.json
  ```
  **Expected**: Backup file with timestamp

---

### Test Group 2: macOS Keychain Credential Storage

#### Test 2.1: Platform Detection
```bash
dotnet test --filter "CredentialProviderTests.CredentialProviderFactory_OnMacOS_ReturnsKeychainProvider"
```

- [ ] **PASS**: Test passes
- [ ] Verify runtime platform:
  ```bash
  dotnet run --project UnifiStockTracker.csproj -- --version
  # Should detect macOS
  ```

#### Test 2.2: Store Credential to Keychain (CRITICAL TEST)

**Manual Test Required** - Create a simple test app:

```bash
cd UnifiStockTracker-CSharp
cat > TestKeychainStore.cs << 'EOF'
using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using UnifiStockTracker.Services.Credentials;

public class TestKeychainStore
{
    public static async Task Main(string[] args)
    {
        var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
        var logger = loggerFactory.CreateLogger<MacOsKeychain>();
        
        var keychain = new MacOsKeychain(logger);
        
        Console.WriteLine("Storing test credential to macOS Keychain...");
        var testCred = new Credential
        {
            Key = "TestEmailSMTP",
            Username = "test@example.com",
            Password = "TestPassword123",
            Type = CredentialType.EmailSmtp
        };
        
        await keychain.StoreCredentialAsync(testCred);
        Console.WriteLine("✅ Credential stored successfully!");
        
        Console.WriteLine("\nRetrieving credential...");
        var retrieved = await keychain.GetCredentialAsync("TestEmailSMTP");
        
        if (retrieved != null)
        {
            Console.WriteLine($"✅ Username: {retrieved.Username}");
            Console.WriteLine($"✅ Password: {retrieved.Password}");
            Console.WriteLine($"✅ Type: {retrieved.Type}");
        }
        else
        {
            Console.WriteLine("❌ Failed to retrieve credential!");
        }
        
        Console.WriteLine("\nDeleting credential...");
        await keychain.DeleteCredentialAsync("TestEmailSMTP");
        Console.WriteLine("✅ Credential deleted");
    }
}
EOF

dotnet run TestKeychainStore.cs
```

**Expected Behavior**:
- [ ] macOS prompts: "UnifiStockTracker wants to access Keychain"
- [ ] Click "Allow" or "Always Allow"
- [ ] Console shows: "✅ Credential stored successfully!"
- [ ] Console shows: "✅ Username: test@example.com"
- [ ] Console shows: "✅ Password: TestPassword123"

**If Prompts Appear Multiple Times**:
- [ ] Click "Always Allow" to avoid future prompts
- [ ] Check Keychain Access.app → Search "UnifiStockTracker" → Verify entry exists

**If Authorization Denied**:
- [ ] Check System Preferences → Security & Privacy → Privacy → Automation
- [ ] Ensure Terminal (or your IDE) has permission

#### Test 2.3: Retrieve Non-Existent Credential
```bash
dotnet test --filter "CredentialProviderTests.GetCredentialAsync_NonExistentKey_ReturnsNull"
```

- [ ] **PASS**: Test passes
- [ ] Verify error handling (no crash, returns null gracefully)

#### Test 2.4: Update Existing Credential
```bash
dotnet test --filter "CredentialProviderTests.StoreCredentialAsync_UpdatesExistingCredential"
```

- [ ] **PASS**: Test passes
- [ ] Open **Keychain Access.app**
- [ ] Search for "UnifiStockTracker"
- [ ] Double-click entry → Should show updated password

#### Test 2.5: Delete Credential
```bash
dotnet test --filter "CredentialProviderTests.DeleteCredentialAsync_RemovesCredential"
```

- [ ] **PASS**: Test passes
- [ ] Verify in Keychain Access.app that entry is removed

#### Test 2.6: List All Credential Keys
```bash
dotnet test --filter "CredentialProviderTests.ListCredentialKeysAsync_ReturnsAllKeys"
```

- [ ] **PASS**: Test passes
- [ ] Verify multiple credentials can be stored and listed

---

### Test Group 3: Fallback to Encrypted File Provider

#### Test 3.1: Simulate Keychain Unavailable

**Manual Test**: Deny Keychain access to test fallback

```bash
# Modify CredentialProviderFactory temporarily to force fallback
# OR deny Keychain access when prompted
```

- [ ] App falls back to `EncryptedFileCredentialProvider`
- [ ] Credentials stored in encrypted file:
  ```bash
  ls -la ~/Library/Application\ Support/UnifiStockTracker/.credentials
  ```
- [ ] File permissions: `600`
- [ ] File content is encrypted (not plain text)

#### Test 3.2: AES-256-CBC Encryption on macOS
```bash
dotnet test --filter "CredentialProviderTests.EncryptedFileProvider_StoresAndRetrievesCredential"
```

- [ ] **PASS**: Test passes
- [ ] Verify encryption uses AES-256-CBC (not DPAPI - that's Windows only)
- [ ] Check conditional compilation: `#if WINDOWS` should NOT trigger on macOS

---

### Test Group 4: Cross-Platform Path Handling

#### Test 4.1: Configuration Directory
```bash
dotnet test --filter "ConfigurationProviderTests"
```

**Verify Paths**:
- [ ] Config directory: `~/Library/Application Support/UnifiStockTracker/`
- [ ] Config file: `~/Library/Application Support/UnifiStockTracker/config.json`
- [ ] Backup files: `~/Library/Application Support/UnifiStockTracker/config.backup.*.json`

#### Test 4.2: Credential Directory
```bash
dotnet test --filter "CredentialProviderTests"
```

**Verify Paths**:
- [ ] Credential file: `~/Library/Application Support/UnifiStockTracker/.credentials`
- [ ] File is hidden (starts with `.`)

---

### Test Group 5: Build & Compilation

#### Test 5.1: Clean Build
```bash
dotnet clean
dotnet build --configuration Release
```

- [ ] **0 errors**
- [ ] **0 warnings**
- [ ] Conditional compilation works (`#if WINDOWS` blocks excluded on macOS)

#### Test 5.2: All Unit Tests
```bash
dotnet test --configuration Release
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
2. Search for "UnifiStockTracker"
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

### Issue: Encrypted File Fallback Not Used
**Symptom**: App crashes instead of falling back to encrypted file

**Debug**:
```bash
# Add logging to see which provider is selected
dotnet run --verbosity detailed
```

Check `CredentialProviderFactory.CreateProvider()` logs

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
