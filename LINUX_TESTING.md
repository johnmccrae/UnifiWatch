# Linux Testing Checklist

**Purpose**: Validate Phase 1 credential and configuration functionality on Linux  
**Platform**: Linux (tested on: ___________ distribution _______)  
**Date**: December 2025  
**Tester**: _____________

---

## ⚠️ KNOWN LIMITATIONS - PHASE 1

### ❌ CRITICAL SECURITY ISSUE: Insecure Encryption on Linux/macOS

**SEVERITY: HIGH** - The encrypted file provider has a **critical security flaw** on Linux and macOS:

**The Problem**:
- AES-256 encryption key is stored **IN THE SAME FILE** as the encrypted credentials
- File format: `[key length][key][iv length][iv][ciphertext]`
- Anyone with read access to `credentials.enc.json` can decrypt the credentials
- This is effectively **obfuscation, not encryption**

**Why This Happens**:
- Windows uses DPAPI which derives keys from user login credentials ✅ SECURE
- Linux/macOS have no equivalent without GUI/keyring daemon
- Current implementation generates random key and stores it with the data ❌ INSECURE

**Impact on Headless/Server Linux**:
- No GNOME Keyring daemon (requires GUI session)
- No KDE Wallet available
- Current fallback provides **NO REAL SECURITY**
- File permissions (600) are the ONLY protection

**Phase 2 Required Fixes** (HIGH PRIORITY):
1. **Implement proper key derivation**:
   - Use PBKDF2 to derive key from user passphrase
   - Store only salt, never the key itself
   - Similar to Windows DPAPI behavior

2. **Linux Kernel Keyring Integration** (headless-friendly):
   - Store AES keys in kernel keyring (`keyctl`)
   - Works without GUI or desktop environment
   - Survives logout/reboot (session vs user vs persistent keyrings)

3. **Environment Variable Key Source**:
   - Allow key to be provided via `UNIFI_ENCRYPTION_KEY`
   - For Docker/container deployments
   - Document secure key management

4. **systemd Credentials** (modern Linux):
   - Use `systemd-creds` for encrypted credential storage
   - Integrated with systemd services

**Current Recommendation**:
- ⚠️ **DO NOT use encrypted file storage for production secrets on Linux**
- Use environment variables instead: `UNIFI_API_KEY`, `UNIFI_API_SECRET`
- Wait for Phase 2 Secret Service or proper key derivation implementation
- File permissions (600) provide minimal protection against other users

---

### ⚠️ LIMITATION: Native Secret Service Not Implemented

**CRITICAL**: The Linux Secret Service credential provider is NOT fully implemented in Phase 1. The current implementation falls back to the insecure encrypted file storage described above.

**Impact**:
- Credentials are stored in `~/.config/unifistock/credentials.enc.json` 
- NOT stored in system keyring (GNOME Keyring, KDE Wallet, pass, etc.)
- Encryption key stored alongside encrypted data (see security issue above)
- Functional for testing, but requires Phase 2 enhancement

**Phase 2 TODO**:
- Implement proper DBus Secret Service API integration using `Tmds.DBus` library
- Support GNOME Keyring (Ubuntu, Fedora, Debian, RHEL, CentOS Stream, openSUSE)
- Support KDE Wallet (Kubuntu, KDE Neon, Fedora KDE, openSUSE KDE)
- Support `pass` (command-line password manager)
- See `Services/Credentials/LinuxSecretService.cs` for implementation notes

**Distribution Compatibility** (Secret Service API - planned for Phase 2):
- ✅ **Ubuntu** (20.04+): GNOME Keyring included by default
- ✅ **Debian** (11+): GNOME Keyring available via `gnome-keyring` package
- ✅ **Fedora** (35+): GNOME Keyring included by default
- ✅ **RHEL/CentOS Stream** (8+): GNOME Keyring available
- ✅ **openSUSE Leap/Tumbleweed**: GNOME Keyring or KDE Wallet
- ✅ **Kubuntu/KDE Neon**: KDE Wallet (kwallet) included
- ✅ **Arch Linux**: Install `gnome-keyring` or `kwalletmanager`
- ✅ **Linux Mint**: GNOME Keyring included
- ⚠️ **Headless/Server**: Requires `gnome-keyring-daemon` or `pass` setup

---

## Pre-Testing Setup

### 1. Development Environment
- [ ] Install .NET 9.0 SDK for Linux (version 9.0.203)
  
  **Option A: Direct download from Microsoft (Ubuntu/Debian x64)**
  ```bash
  # Download .NET 9.0.203 SDK
  wget https://download.visualstudio.microsoft.com/download/pr/822078c4-86ed-4e8d-9cbd-dc15d05f030f/9ff0b5b3e93ddc34c0e04ca93baf2e18/dotnet-sdk-9.0.203-linux-x64.tar.gz
  
  # Extract to /usr/share/dotnet
  sudo mkdir -p /usr/share/dotnet
  sudo tar -xzf dotnet-sdk-9.0.203-linux-x64.tar.gz -C /usr/share/dotnet
  
  # Create symlink
  sudo ln -sf /usr/share/dotnet/dotnet /usr/bin/dotnet
  
  # Verify installation
  dotnet --version  # Should show 9.0.203
  ```
  
  **Option B: Using package manager (Ubuntu/Debian)**
  ```bash
  # Add Microsoft package repository
  wget https://packages.microsoft.com/config/ubuntu/$(lsb_release -rs)/packages-microsoft-prod.deb -O packages-microsoft-prod.deb
  sudo dpkg -i packages-microsoft-prod.deb
  rm packages-microsoft-prod.deb
  
  # Install .NET SDK
  sudo apt-get update
  sudo apt-get install -y dotnet-sdk-9.0
  
  # Verify installation
  dotnet --version
  ```
  
  **Option C: Fedora/RHEL/CentOS**
  ```bash
  sudo dnf install dotnet-sdk-9.0
  dotnet --version
  ```
  
  **Option D: Arch Linux**
  ```bash
  sudo pacman -S dotnet-sdk
  dotnet --version
  ```

- [ ] Clone repository
  ```bash
  git clone https://github.com/EvotecIT/UnifiStockTracker.git
  cd UnifiStockTracker/UnifiStockTracker-CSharp
  ```

- [ ] Restore NuGet packages
  ```bash
  dotnet restore UnifiStockTracker-CSharp.sln
  ```

- [ ] Build project
  ```bash
  dotnet build UnifiStockTracker-CSharp.sln
  ```
  **Expected**: 0 errors, 0 warnings

### 2. Linux Secret Service Prerequisites (Not used in Phase 1, but document for Phase 2)

**GNOME Keyring (Ubuntu, Debian, Fedora, RHEL)**:
```bash
# Check if gnome-keyring is installed
dpkg -l | grep gnome-keyring  # Debian/Ubuntu
rpm -qa | grep gnome-keyring  # Fedora/RHEL

# Install if needed
sudo apt-get install gnome-keyring  # Debian/Ubuntu
sudo dnf install gnome-keyring       # Fedora/RHEL

# Check if daemon is running
ps aux | grep gnome-keyring-daemon
```

**KDE Wallet (Kubuntu, KDE Neon, openSUSE KDE)**:
```bash
# Check if KDE Wallet is installed
dpkg -l | grep kwalletmanager  # Debian/Ubuntu
rpm -qa | grep kwalletmanager  # Fedora/RHEL

# Install if needed
sudo apt-get install kwalletmanager  # Debian/Ubuntu
sudo dnf install kwalletmanager      # Fedora/RHEL
```

---

## Critical Test Cases

### Test Group 1: Configuration Provider

#### Test 1.1: Configuration File Location
```bash
dotnet test UnifiStockTracker-CSharp.sln --filter "ConfigurationProviderTests.LoadAsync_ValidConfiguration_LoadsAllSettings"
```

- [ ] **PASS**: Test passes
- [ ] Verify config directory created at:
  ```bash
  ls -la ~/.config/unifistock/
  ```
  **Expected**: Directory exists

- [ ] Check file permissions:
  ```bash
  stat -c "%a" ~/.config/unifistock/config.json
  ```
  **Expected**: `600` (read/write for owner only)

#### Test 1.2: Configuration Save/Load Cycle
```bash
dotnet test UnifiStockTracker-CSharp.sln --filter "ConfigurationProviderTests.SaveAsync_ValidConfiguration_SavesCorrectly"
```

- [ ] **PASS**: Test passes
- [ ] Verify JSON file created:
  ```bash
  cat ~/.config/unifistock/config.json
  ```
  **Expected**: Valid JSON with ServiceSettings, MonitoringSettings, NotificationSettings

#### Test 1.3: Configuration Backup
```bash
dotnet test UnifiStockTracker-CSharp.sln --filter "ConfigurationProviderTests.BackupAsync_CreatesBackupFile"
```

- [ ] **PASS**: Test passes
- [ ] Verify backup file created:
  ```bash
  ls -l ~/.config/unifistock/config.backup.*.json
  ```
  **Expected**: Backup file with timestamp

---

### Test Group 2: Linux Credential Storage (FALLBACK MODE - Phase 1)

⚠️ **IMPORTANT**: Phase 1 uses encrypted file storage, NOT native Secret Service integration.

#### Test 2.1: Platform Detection
```bash
dotnet test UnifiStockTracker-CSharp.sln --filter "CredentialProviderTests.CredentialProviderFactory_CreateProvider_WithAutoStorage_ShouldSelectDefault"
```

- [ ] **PASS**: Test passes
- [ ] Verify runtime platform:
  ```bash
  dotnet run --project UnifiStockTracker.csproj -- --version
  # Should detect Linux
  ```

#### Test 2.2: Encrypted File Credential Storage (CURRENT IMPLEMENTATION)

**Note**: The unit tests use the encrypted file provider since native Secret Service is not implemented yet.

**Manual Test - Verify Fallback Behavior**:
```bash
# Check that LinuxSecretService falls back to encrypted file
dotnet test UnifiStockTracker-CSharp.sln --filter "CredentialProviderTests.StoreAsync_WithValidKeyAndSecret_ShouldSucceed"

# Verify encrypted file is created (in test temp directory during unit tests)
# In production, credentials would be at: ~/.config/unifistock/credentials.enc.json
```

**Expected Behavior**:
- [ ] Tests pass using encrypted file storage
- [ ] Warning logged: "Using encrypted file credential storage"
- [ ] File created with `600` permissions (owner read/write only)
- [ ] AES-256-CBC encryption used (not DPAPI - that's Windows only)

#### Test 2.3: Retrieve Non-Existent Credential
```bash
dotnet test UnifiStockTracker-CSharp.sln --filter "CredentialProviderTests.RetrieveAsync_WithNonExistentKey_ShouldReturnNull"
```

- [ ] **PASS**: Test passes
- [ ] Verify error handling (no crash, returns null gracefully)

#### Test 2.4: Update Existing Credential
```bash
dotnet test UnifiStockTracker-CSharp.sln --filter "CredentialProviderTests.StoreAsync_UpdateExistingKey_ShouldOverwrite"
```

- [ ] **PASS**: Test passes
- [ ] Verify encrypted file is updated correctly

#### Test 2.5: Delete Credential
```bash
dotnet test UnifiStockTracker-CSharp.sln --filter "CredentialProviderTests.DeleteAsync_AfterStore_ShouldSucceed"
```

- [ ] **PASS**: Test passes
- [ ] Verify credential is removed from encrypted file

#### Test 2.6: List All Credential Keys
```bash
dotnet test UnifiStockTracker-CSharp.sln --filter "CredentialProviderTests.ListAsync_AfterMultipleStores_ShouldReturnAllKeys"
```

- [ ] **PASS**: Test passes
- [ ] Verify multiple credentials can be stored and listed

---

### Test Group 3: Encrypted File Provider (Primary Storage Method - Phase 1)

#### Test 3.1: Encrypted File Provider

**Test the encrypted file provider directly:**
```bash
# Run the encrypted file provider test
dotnet test UnifiStockTracker-CSharp.sln --filter "CredentialProviderTests.StoreAsync_ThenRetrieveAsync_ShouldReturnSameValue"
```

- [ ] **PASS**: Test passes

**Note**: The unit tests don't persist files to the production directory - they use temporary test directories and clean up after themselves. The encrypted file would be created in production at:
- **User mode**: `~/.config/unifistock/credentials.enc.json`
- **Root mode**: `/etc/unifistock/credentials.enc.json`

**Production Test (Optional)**:
```bash
# Run actual application to create production credential file
# This will create ~/.config/unifistock/credentials.enc.json

# Check file permissions
stat -c "%a" ~/.config/unifistock/credentials.enc.json
# Expected: 600

# Check file ownership
ls -l ~/.config/unifistock/credentials.enc.json
# Expected: owned by your user
```

#### Test 3.2: AES-256-CBC Encryption on Linux
```bash
dotnet test UnifiStockTracker-CSharp.sln --filter "CredentialProviderTests.StoreAsync_ThenRetrieveAsync_ShouldReturnSameValue"
```

- [ ] **PASS**: Test passes
- [ ] Verify encryption uses AES-256-CBC (not DPAPI - that's Windows only)
- [ ] Check conditional compilation: `#if WINDOWS` should NOT trigger on Linux

#### Test 3.3: ⚠️ SECURITY VERIFICATION - Encryption Key Storage (CRITICAL)

**Purpose**: Verify the known security flaw where encryption keys are stored with the encrypted data.

```bash
# Create a test credential file in production location
mkdir -p ~/.config/unifistock
echo '{"test-key":"test-secret"}' > /tmp/test-creds.json

# Run app to encrypt credentials (this would need actual app execution)
# For now, verify the file structure manually after tests create it

# Check if the encrypted file contains both key and ciphertext
# This demonstrates the security flaw
hexdump -C ~/.config/unifistock/credentials.enc.json | head -20
```

**Expected Findings** (demonstrating the flaw):
- [ ] File format: `[1 byte: key length][32 bytes: AES key][1 byte: IV length][16 bytes: IV][remaining: ciphertext]`
- [ ] AES-256 key is visible in hexdump at bytes 1-33
- [ ] Anyone with file access can extract the key and decrypt
- [ ] ⚠️ **CONFIRMED**: File permissions (600) are the ONLY security measure

**Workaround for Production** (until Phase 2):
```bash
# Use environment variables instead of encrypted file storage
export UNIFI_STOCK_TRACKER_STORAGE="environment-variables"
export UNIFI_API_KEY="your-api-key-here"
export UNIFI_API_SECRET="your-api-secret-here"

# Run the application
dotnet run --project UnifiStockTracker.csproj
```

---

### Test Group 4: Cross-Platform Path Handling

#### Test 4.1: Configuration Directory
```bash
dotnet test UnifiStockTracker-CSharp.sln --filter "ConfigurationProviderTests"
```

**Verify Paths** (User Mode):
- [ ] Config directory: `~/.config/unifistock/`
- [ ] Config file: `~/.config/unifistock/config.json`
- [ ] Backup files: `~/.config/unifistock/config.backup.*.json`

**Verify Paths** (Root/System Mode - if testing as root):
- [ ] Config directory: `/etc/unifistock/`
- [ ] Config file: `/etc/unifistock/config.json`
- [ ] Backup files: `/etc/unifistock/config.backup.*.json`

#### Test 4.2: Credential Directory
```bash
dotnet test UnifiStockTracker-CSharp.sln --filter "CredentialProviderTests"
```

**Verify Paths**:
- [ ] Credential file: `~/.config/unifistock/credentials.enc.json` (user mode)
- [ ] OR: `/etc/unifistock/credentials.enc.json` (root mode)
- [ ] File permissions: `600` (read/write for owner only)

**Note**: This file is only created in production when using encrypted file storage, not during unit tests which use temporary directories.

---

### Test Group 5: Build & Compilation

#### Test 5.1: Clean Build
```bash
dotnet clean UnifiStockTracker-CSharp.sln
dotnet build UnifiStockTracker-CSharp.sln --configuration Release
```

- [ ] **0 errors**
- [ ] **0 warnings**
- [ ] Conditional compilation works (`#if WINDOWS` blocks excluded on Linux)

#### Test 5.2: All Unit Tests
```bash
dotnet test UnifiStockTracker-CSharp.sln --configuration Release
```

**Expected Results**:
- [ ] **65 tests passed** ✅
- [ ] **6 tests skipped** (integration tests)
- [ ] **0 tests failed** ❌

---

## Known Issues & Workarounds

### Issue: ❌ CRITICAL - Encryption Key Stored With Encrypted Data
**Symptom**: Encrypted credentials file provides no real security

**Root Cause**: 
- AES-256 encryption key is stored in the same file as encrypted data
- File format: `[key][iv][ciphertext]` - key is extractable
- Anyone with read access can decrypt credentials

**Security Impact**: 
- **HIGH** - File permissions (600) are the ONLY protection
- Not suitable for production secrets on shared/multi-user systems
- Headless servers have no better alternative in Phase 1

**Workaround for Production**:
```bash
# Option 1: Use environment variables (RECOMMENDED)
export UNIFI_STOCK_TRACKER_STORAGE="environment-variables"
export UNIFI_API_KEY="your-api-key"
export UNIFI_API_SECRET="your-api-secret"

# Option 2: Restrict file system access (minimal protection)
chmod 600 ~/.config/unifistock/credentials.enc.json
# Ensure only root or app user can read directory
chmod 700 ~/.config/unifistock/
```

**Phase 2 Fix Required**:
1. Implement PBKDF2 key derivation from user passphrase
2. Store only salt, never the encryption key
3. Integrate with Linux Kernel Keyring for headless systems
4. Support systemd credentials for service deployments

---

### Issue: Native Secret Service Not Implemented (Phase 1 Limitation)
**Symptom**: Credentials stored in encrypted file instead of GNOME Keyring/KDE Wallet

**Status**: **PLANNED FOR PHASE 2**

**Current Behavior**:
- `LinuxSecretService` provider falls back to `EncryptedFileCredentialProvider`
- Credentials stored at `~/.config/unifistock/credentials.enc.json`
- Uses AES-256-CBC encryption
- Functional but less secure than native keyring

**Phase 2 Enhancement**:
- Implement DBus Secret Service API integration
- Use `Tmds.DBus` NuGet package
- Support GNOME Keyring, KDE Wallet, pass, and other Secret Service providers
- Requires `libsecret-1-0` (Ubuntu/Debian) or `libsecret` (Fedora/RHEL)

**Distribution Support** (Phase 2):
| Distribution | Secret Service | Package | Compatibility |
|--------------|----------------|---------|---------------|
| Ubuntu 20.04+ | GNOME Keyring | Built-in | ✅ Excellent |
| Debian 11+ | GNOME Keyring | `gnome-keyring` | ✅ Excellent |
| Fedora 35+ | GNOME Keyring | Built-in | ✅ Excellent |
| RHEL/CentOS 8+ | GNOME Keyring | `gnome-keyring` | ✅ Good |
| openSUSE Leap/Tumbleweed | GNOME Keyring or KWallet | `gnome-keyring` / `kwallet` | ✅ Excellent |
| Kubuntu/KDE Neon | KDE Wallet | Built-in | ✅ Excellent |
| Arch Linux | GNOME Keyring or KWallet | `gnome-keyring` / `kwallet` | ✅ Good |
| Linux Mint | GNOME Keyring | Built-in | ✅ Excellent |
| Pop!_OS | GNOME Keyring | Built-in | ✅ Excellent |
| Elementary OS | GNOME Keyring | Built-in | ✅ Excellent |

### Issue: File Permissions on Shared Systems
**Symptom**: Other users on multi-user Linux systems cannot read credential files

**Expected Behavior**: This is CORRECT - files should be `600` (owner-only access)

**Workaround**: None needed - this is secure by design

### Issue: SELinux Restrictions (RHEL/Fedora/CentOS)
**Symptom**: Permission denied errors even with correct file permissions

**Workaround**:
```bash
# Check SELinux status
getenforce

# If Enforcing, check audit log
sudo ausearch -m avc -ts recent

# Temporarily set to Permissive (for testing only)
sudo setenforce 0

# Re-run tests
dotnet test UnifiStockTracker-CSharp.sln

# Re-enable SELinux
sudo setenforce 1

# Create custom SELinux policy if needed (Phase 2)
```

---

## Test Results Summary

**Date**: _______________  
**Linux Distribution**: _______________  
**Desktop Environment**: GNOME / KDE / XFCE / Other: _______  
**Tester**: _______________

| Test Group | Tests Passed | Tests Failed | Notes |
|------------|--------------|--------------|-------|
| Configuration Provider | __ / 19 | __ | |
| Credential Storage (Encrypted File) | __ / 46 | __ | |
| Encrypted File Provider | __ / 5 | __ | |
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
- [ ] Terminal output of successful test runs
- [ ] File permissions (`ls -la ~/.config/unifistock/`)
- [ ] Any error messages or crashes

**Attach logs**:
- [ ] `dotnet test` output (full)
- [ ] `journalctl` logs (if applicable)
- [ ] SELinux audit logs (RHEL/Fedora/CentOS)

---

## Sign-Off

**Phase 1 Linux Testing**: ✅ PASS / ❌ FAIL  

**Blocker Issues**: __ (number)  
**Non-Blocker Issues**: __ (number)  

**Ready for Production on Linux** (with encrypted file storage): YES / NO  

**Tester Signature**: _______________  
**Date**: _______________

---

## Next Steps After Linux Testing

1. **If all tests pass**:
   - Update `TEST_RESULTS.md` with Linux results
   - Commit changes to repository
   - Document Phase 1 completion

2. **If critical failures**:
   - Document issues in GitHub Issues
   - Fix Linux-specific bugs
   - Re-run test suite
   - Update `LinuxSecretService.cs` if needed

3. **Phase 2 Preparation - Native Secret Service Integration**:
   - Add `Tmds.DBus` NuGet package
   - Implement DBus Secret Service API in `LinuxSecretService.cs`
   - Test on Ubuntu (GNOME Keyring)
   - Test on Kubuntu (KDE Wallet)
   - Test on Fedora (GNOME Keyring)
   - Test on Arch Linux (user-installed keyring)
   - Handle headless/server environments gracefully
   - Create integration tests for each supported keyring

4. **Known Phase 2 Requirements**:
   - `libsecret-1-0` (Ubuntu/Debian) or `libsecret` (Fedora/RHEL) must be installed
   - DBus session bus must be running
   - Keyring daemon must be running (gnome-keyring-daemon or kwalletd)
   - Handle SSH sessions and headless environments (unlock keyring automatically or fall back to encrypted file)
