# UnifiWatch Security Guide

## Overview

UnifiWatch handles sensitive information including API credentials, SMTP passwords, and SMS provider tokens. This guide explains how credentials are stored securely on each platform and best practices for maintaining security.

---

## Credential Storage Security

### Windows: Windows Credential Manager (DPAPI)

**How it works**:

- Credentials stored in Windows Credential Manager using **Data Protection API (DPAPI)**
- DPAPI encryption tied to Windows user account and machine
- No plaintext storage anywhere on disk
- Credentials protected even if hard drive is removed (must be decrypted on original machine/user)

**Security Properties**:

- ✅ **Strong encryption**: AES-256 via DPAPI
- ✅ **User-specific**: Only decryptable by logged-in Windows user
- ✅ **Machine-specific**: Only decryptable on this machine
- ✅ **Automatic**: No keys to manage
- ✅ **Hardware-backed** (optional): Can use TPM 2.0 for additional security

**Location**:

- `Control Panel → Credential Manager → Windows Credentials`
- View entries starting with "UnifiWatch:" (e.g., "UnifiWatch:email-smtp")

**Limitations**:

- Not available on Windows Home editions without workarounds
- Requires Windows Credential Manager service running (default)
- Credentials lost if Windows user account is deleted

**Best Practice**:

- Regularly backup Windows credentials using Credential Manager backup tool
- Restrict access to Windows account with strong password
- Enable Windows Hello or Windows Credential Guard (Windows Pro+)

---

### macOS: Keychain

**How it works**:

- Credentials stored in macOS Keychain (iCloud Keychain or Local Keychain)
- Encryption tied to macOS user account
- Integrated with macOS security framework

**Security Properties**:

- ✅ **AES-256 encryption**
- ✅ **User-specific**: Only decryptable by logged-in macOS user
- ✅ **Keychain lock**: Can be locked when account is locked
- ✅ **iCloud Keychain** (optional): Syncs securely across Apple devices
- ✅ **Automatic**: No keys to manage

**Location**:

- Applications → Utilities → Keychain Access
- Search for "UnifiWatch" entries
- Entries appear in "login" keychain

**Limitations**:

- Credentials synced to iCloud if enabled (ensures backup but adds internet dependency)
- Requires Keychain service running (default)
- May prompt user to grant access on first use

**Best Practice**:

- Use strong macOS user password
- Enable FileVault full-disk encryption
- Regularly backup macOS to Time Machine
- Review Keychain access permissions in System Settings → Security & Privacy

---

### Linux: Secret Service (D-Bus) with PBKDF2 Fallback

**How it works** (preferred):

- Credentials stored via Secret Service API (D-Bus)
- Integrated with GNOME Keyring or KDE Wallet
- Encryption managed by system daemon

**How it works** (fallback):

- If secret-service unavailable: Encrypted file storage
- Location: `~/.config/UnifiWatch/credentials/` (file permissions: 600)
- Encryption: **AES-256-CBC** with **PBKDF2 key derivation**
  - 100,000 iterations (OWASP recommended minimum)
  - 32-byte salt (random per credential)
  - Key derived from: machine ID + username + hostname (machine-specific)

**Security Properties** (Secret Service):

- ✅ **System daemon encryption**: Credentials never in app memory long-term
- ✅ **User-specific**: Only accessible by logged-in user
- ✅ **Keyring protection**: Keyring can be locked
- ✅ **D-Bus authentication**: Only authenticated services can access

**Security Properties** (Fallback PBKDF2):

- ✅ **AES-256 encryption**: Industry standard
- ✅ **PBKDF2 key derivation**: 100,000 iterations (strong against brute-force)
- ✅ **File permissions**: 600 (readable only by owner)
- ✅ **No plaintext**: Keys never stored in files
- ✅ **Machine-locked**: Keys derived from machine-specific IDs

**File Format** (PBKDF2 fallback):

```text
[Salt: 32 bytes] + [IV: 16 bytes] + [AES-256-CBC ciphertext]
Total overhead: 48 bytes + encrypted credential size
```

**Limitations**:

- Secret Service requires desktop environment (not available on headless servers)
- Fallback encryption requires proper file permissions (700 for directory, 600 for files)
- If machine ID changes, credentials become inaccessible (machine-locked by design)

**Best Practice**:

- **Desktop Linux**: Use GNOME Keyring or KDE Wallet (install `secret-service`)
- **Headless servers**: Use PBKDF2 encrypted file storage (secure by default)
- Set directory permissions: `chmod 700 ~/.config/UnifiWatch/`
- Set credential file permissions: `chmod 600 ~/.config/UnifiWatch/credentials/*`
- Restrict shell access: Only authorized system accounts can read
- Disable credentials file readable via SSH: Use key-based auth for service account

---

### Cross-Platform Fallback: Environment Variables

**For CI/CD or automation**:

- Set credentials via environment variables (least secure, use with caution)
- Format: `UNIFIWATCH_CREDENTIAL_{KEY}=value`
- Only use in secure environments (CI/CD with restricted access)
- Example:

  ```bash
  export UNIFIWATCH_CREDENTIAL_EMAIL_SMTP=my-password
  UnifiWatch --stock
  ```

**Security Properties**:

- ⚠️ **Visible in process list**: `ps aux` can expose variables
- ⚠️ **Visible in shell history**: May appear in `.bash_history`
- ⚠️ **No encryption**: Plaintext in memory
- ✅ Temporary: Cleared when process ends

**Only use when**:

- Running in secure CI/CD environment (restricted job access)
- No other credential storage available
- Short-lived credentials acceptable

---

## API Key & Password Security

### Best Practices

1. **Use application-specific passwords** (not personal passwords):
   - Gmail: Generate "App password" at https://myaccount.google.com/apppasswords
   - Microsoft/Office 365: Use "App password"
   - Twilio: Use separate API key (not account password)

2. **Rotate credentials regularly**:
   - Gmail App password: Every 90 days
   - Twilio API Key: Every 90 days
   - Email accounts: Every 180 days
   - SMS provider accounts: Per provider recommendations

3. **Monitor for unauthorized access**:
   - Gmail: Check "Connected apps & sites" at https://myaccount.google.com/connectedapps
   - Twilio: Review API key usage in Dashboard
   - Email provider: Check login history

4. **Use strong, unique passwords**:
   - Minimum 16 characters
   - Mix of uppercase, lowercase, numbers, symbols
   - Never reuse across services
   - Use password manager if managing multiple credentials

5. **Restrict permissions**:
   - Gmail SMTP: No inbox/contacts/settings access needed
   - Twilio: Only SMS sending permissions required
   - Email SMTP: Only send mail, no folder/calendar access

### Credential Rotation

**How to rotate credentials**:

1. Generate new credentials in external service (Gmail, Twilio, etc.)
2. Update UnifiWatch configuration:

   ```bash
   UnifiWatch --configure
   # When prompted for email password, enter new password
   # When prompted for SMS token, enter new token
   ```

3. Verify notifications work:

   ```bash
   UnifiWatch --test-notifications
   ```

4. Revoke old credentials in external service (Gmail, Twilio account)
5. Delete old credentials from Windows Credential Manager / Keychain / encrypted files

---

## Network Security

### SMTP (Email)

**TLS/SSL Encryption**:

- ✅ Always use TLS (SMTP over TLS, port 587)
- ⚠️ Never use plaintext SMTP (port 25 without TLS)
- ✅ Verify certificate (app validates server certificate by default)

**Recommended providers**:

- **Gmail**: SMTP with app password + TLS (port 587)
- **Office 365**: SMTP with app password + TLS
- **SendGrid**: API-based (more secure than SMTP)
- **AWS SES**: API-based with IAM credentials

**Example configuration**:

```json
{
  "Notifications": {
    "Email": {
      "SmtpServer": "smtp.gmail.com",
      "SmtpPort": 587,
      "UseTls": true,
      "FromAddress": "your-email@gmail.com"
    }
  }
}
```

### SMS (Twilio)

**Security**:

- ✅ Twilio API uses HTTPS with certificate pinning
- ✅ Account SID + Auth Token required (2-factor)
- ✅ Rate limiting on API (prevents abuse)

**Best practices**:

- Store Auth Token in credential manager (not config file)
- Use separate API key for UnifiWatch (not account password)
- Restrict phone numbers to whitelisted recipients
- Monitor Twilio console for unauthorized SMS sends
- Enable IP whitelisting in Twilio (if available)

**Example configuration**:

```json
{
  "Notifications": {
    "Sms": {
      "Provider": "twilio",
      "Recipients": ["+1-555-0123", "+1-555-0456"]
    }
  }
}
```

---

## File & Directory Permissions

### Configuration Files

**Recommended permissions**:

- Config directory: `700` (rwx------)
- Config file: `600` (rw-------)
- Log directory: `700` (rwx------)
- Log files: `600` (rw-------)

**Set permissions on Linux**:

```bash
chmod 700 ~/.config/UnifiWatch
chmod 600 ~/.config/UnifiWatch/config.json
chmod 700 ~/.config/UnifiWatch/credentials
chmod 600 ~/.config/UnifiWatch/credentials/*
```

**Why restrictive permissions**:

- Prevents other users from reading config
- Prevents other users from reading credentials
- Prevents accidental world-readable credential exposure
- Matches security best practice for sensitive files

### Windows

**File security**:

- Config files stored in user AppData (not accessible by other users)
- Use NTFS permissions if shared machine:

  ```powershell
  $UnifiWatchPath = "$env:APPDATA\UnifiWatch"
  $CurrentUser = [System.Security.Principal.WindowsIdentity]::GetCurrent().User
  
  # Get current ACL and remove inherited permissions
  $Acl = Get-Acl $UnifiWatchPath
  $Acl.SetAccessRuleProtection($true, $false)
  
  # Create access rule for current user (Full Control)
  $AccessRule = New-Object System.Security.AccessControl.FileSystemAccessRule(
    $CurrentUser,
    "FullControl",
    "ContainerInherit,ObjectInherit",
    "None",
    "Allow"
  )
  $Acl.AddAccessRule($AccessRule)
  
  # Apply the new ACL
  Set-Acl -Path $UnifiWatchPath -AclObject $Acl
  
  # Verify permissions
  Get-Acl $UnifiWatchPath | Format-List
  ```

### macOS

**File security**:

- Credentials in Keychain (protected by user account)
- Config in Library (protected by user account)
- Use FileVault full-disk encryption for additional protection

---

## Logging Security

### What's Logged

**Safe to log**:

- Stock availability changes
- Service start/stop events
- Configuration changes (without sensitive data)
- Notification attempts (success/failure)
- Error messages (generic)

**Never logged**:

- Passwords or API keys
- SMTP/SMS credentials
- Email addresses (optionally redacted)
- Phone numbers (optionally redacted)
- Detailed error messages with sensitive data

### Log File Protection

**Location**:

- Windows: `%APPDATA%\UnifiWatch\logs\`
- Linux: `~/.config/UnifiWatch/logs/` or `journalctl`
- macOS: `~/Library/Logs/UnifiWatch/`

**Permissions**:

- Set to `600` or `700` (readable only by user)
- Rotate logs monthly (prevent large files)
- Archive old logs securely
- Delete after 90 days (no sensitive data, but good practice)

**Set permissions on Windows (PowerShell)**:

```powershell
$LogPath = "$env:APPDATA\UnifiWatch\logs"
$CurrentUser = [System.Security.Principal.WindowsIdentity]::GetCurrent().User

# Get current ACL and remove inherited permissions
$Acl = Get-Acl $LogPath
$Acl.SetAccessRuleProtection($true, $false)

# Remove all existing access rules
$Acl.Access | ForEach-Object { $Acl.RemoveAccessRule($_) }

# Create access rule for current user (Full Control)
$AccessRule = New-Object System.Security.AccessControl.FileSystemAccessRule(
  $CurrentUser,
  "FullControl",
  "ContainerInherit,ObjectInherit",
  "None",
  "Allow"
)
$Acl.AddAccessRule($AccessRule)

# Apply the new ACL
Set-Acl -Path $LogPath -AclObject $Acl

# Verify permissions are set correctly
Get-Acl $LogPath | Format-List
```

**Set permissions on Linux**:

```bash
chmod 700 ~/.config/UnifiWatch/logs
chmod 600 ~/.config/UnifiWatch/logs/*
```

**Set permissions on macOS**:

```bash
chmod 700 ~/Library/Logs/UnifiWatch
chmod 600 ~/Library/Logs/UnifiWatch/*
```

**Best practice**:

- Review logs regularly for suspicious activity
- Monitor for repeated failed notifications
- Alert on unexpected configuration changes

---

## Incident Response

### If Credentials Are Compromised

1. **Immediately revoke compromised credentials**:

   ```bash
   # For Gmail: https://myaccount.google.com/apppasswords
   # For Twilio: https://www.twilio.com/console/account/api-keys
   # For email provider: Provider's account settings
   ```

2. **Generate new credentials**:
   - Create new app password / API key
   - Do NOT reuse old credentials

3. **Update UnifiWatch**:

   ```bash
   UnifiWatch --configure
   # Enter new credentials when prompted
   ```

4. **Verify service restarted** with new credentials:

   ```bash
   UnifiWatch --test-notifications
   ```

5. **Monitor for suspicious activity**:
   - Email provider: Check login history for unauthorized access
   - Twilio: Check SMS sending history
   - Check for unexpected notifications sent to wrong recipients

### If Config File Is Exposed

1. **On Windows**:
   - DPAPI-encrypted credentials are still secure
   - Regenerate in Credential Manager if concerned
   - Update config file to remove references to old credentials

2. **On Linux/macOS**:
   - PBKDF2 credentials remain encrypted (file alone is useless without key)
   - Still rotate credentials as precaution
   - Verify file permissions (600) to prevent future exposure

3. **On any platform**:
   - Update all credentials (email, SMS provider)
   - Change service account password
   - Review access logs for unauthorized changes

---

## Security Checklist

- [ ] Running latest UnifiWatch version
- [ ] Windows/macOS/Linux OS fully patched
- [ ] Using app-specific passwords (not personal passwords)
- [ ] SMTP configured with TLS (port 587)
- [ ] Credentials stored in platform secure storage (not config file)
- [ ] File permissions set correctly (600 for configs, 700 for directories)
- [ ] Service account has minimal required permissions
- [ ] Reviewed notification test sent to verify delivery
- [ ] Monitoring logs for errors
- [ ] Backups created of config (sensitive data redacted)
- [ ] Credential rotation schedule established (every 90 days)
- [ ] No credentials in shell history, scripts, or version control

---

## Known Limitations & Platform-Specific Security Considerations

### ⚠️ Linux/macOS Encrypted File Storage (Phase 1 Implementation)

**Current Status**: The PBKDF2 encrypted file provider in Phase 1 has been designed with machine-specific key derivation, providing reasonable security for most use cases.

**Security Model**:
- **Key Derivation**: PBKDF2 with 100,000 iterations
- **Salt**: 32-byte random salt (unique per credential)
- **Machine Binding**: Keys derived from machine ID + username + hostname
- **File Permissions**: 600 (owner read/write only)
- **Encryption**: AES-256-CBC

**Protection Level**:
- ✅ **Protected against casual inspection**: Credentials are encrypted, not plaintext
- ✅ **Protected by file permissions**: Only the user who created them can read the files
- ✅ **Machine-specific**: Keys cannot be used on different machines
- ⚠️ **Limited protection against privileged users**: Root/admin users can potentially access credentials
- ⚠️ **Relies on filesystem security**: Primary protection is Unix file permissions

**Comparison to Platform Keychains**:

| Feature | Windows (DPAPI) | macOS (Keychain) | Linux (PBKDF2 File) |
|---------|----------------|------------------|---------------------|
| Encryption | ✅ AES-256 | ✅ AES-256 | ✅ AES-256 |
| Key Storage | ✅ Windows managed | ✅ macOS managed | ⚠️ Derived from machine |
| User Protection | ✅ User account | ✅ User account | ✅ File permissions |
| Root/Admin Access | ✅ Protected | ✅ Protected | ⚠️ Potential access |
| Hardware Backing | ✅ TPM optional | ✅ Secure Enclave | ❌ Not available |

**Phase 2 Planned Enhancements** (Future Improvements):

1. **Linux Kernel Keyring Integration**:
   - Store encryption keys in Linux kernel keyring (`keyctl`)
   - Works without GUI or desktop environment
   - Better isolation from privileged users

2. **systemd Credentials Support**:
   - Use `systemd-creds` for encrypted credential storage
   - Integration with systemd services
   - Automatic credential lifecycle management

3. **Environment Variable Key Source**:
   - Allow encryption key via `UNIFIWATCH_ENCRYPTION_KEY` environment variable
   - Useful for Docker/container deployments
   - Document secure key management practices

4. **User Passphrase Option**:
   - Optional passphrase-based key derivation
   - Prompt for passphrase on first use
   - Store passphrase hash for validation

**Current Recommendation for Production Linux/macOS**:

1. **Desktop Environments**: Use Secret Service (GNOME Keyring/KDE Wallet) when available
2. **Headless Servers**: PBKDF2 encrypted file storage is acceptable with proper precautions:
   - Ensure file permissions are 600 (verify: `ls -l ~/.config/UnifiWatch/credentials/`)
   - Restrict system access (limit users with sudo privileges)
   - Run service as dedicated user account (not shared account)
   - Use network-level security (firewall, VPN) as additional layer
   - Consider using environment variables for CI/CD automation
3. **High-Security Environments**: Use dedicated secrets management (HashiCorp Vault, AWS Secrets Manager)

**Alternative: Secret Management Services**

For production deployments requiring enhanced security, consider:
- **HashiCorp Vault**: Industry-standard secrets management
- **AWS Secrets Manager**: Cloud-based credential storage
- **Azure Key Vault**: Azure-native secrets management
- **Environment Variables**: For containerized deployments (with proper container security)

---

## Additional Resources

- **Windows Credential Manager**: https://support.microsoft.com/en-us/windows/windows-credentials-86b4f1d6-6c1f-4dbb-801e-5eb6c10a020a
- **macOS Keychain**: https://support.apple.com/en-us/HT204085
- **GNOME Keyring**: https://wiki.gnome.org/Projects/GnomeKeyring
- **OWASP Password Storage**: https://cheatsheetseries.owasp.org/cheatsheets/Password_Storage_Cheat_Sheet.html
- **PBKDF2 Specification**: https://tools.ietf.org/html/rfc8018
- **Gmail App Passwords**: https://support.google.com/accounts/answer/185833
- **Twilio Security**: https://www.twilio.com/docs/general/security
- **Linux Kernel Keyring**: https://www.kernel.org/doc/html/latest/security/keys/core.html
- **systemd Credentials**: https://systemd.io/CREDENTIALS/


