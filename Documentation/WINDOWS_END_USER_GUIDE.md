# Windows End User Guide

**Audience**: QA, support, and deployment engineers validating the Windows service in real environments

**How to use this guide**: Run each command and confirm the expected behavior occurs. You do not need to fill in blanks or record outputs unless you want evidence for a defect. If a step fails, capture the error message and continue to the next relevant test.

## Overview

This guide provides a comprehensive manual testing checklist for UnifiWatch service mode on Windows. Complete all tests to verify proper installation, configuration, notification delivery, and service management.

---

## Prerequisites

- [ ] Windows 10 or Windows 11 (version 1809 or later)
- [ ] .NET 9.0 Runtime installed
- [ ] PowerShell 5.1 or later
- [ ] Administrator privileges for service installation
- [ ] Internet connection for API access

---

## Test 1: Build and Publish

### Steps

1. Open PowerShell (Administrator)

2. Clone the repository:

   ```powershell
   cd C:\localrepo
   git clone https://github.com/johnmccrae/UnifiWatch.git
   cd UnifiWatch
   ```

   **Note**: If you already have the repository cloned, skip to step 3.

3. Build release version:

   ```powershell
   dotnet publish -c Release -r win-x64 --self-contained -p:PublishSingleFile=true
   ```

4. Verify executable created:

   ```powershell
   Test-Path "bin\Release\net9.0\win-x64\publish\UnifiWatch.exe"
   ```

### Expected Results

- [ ] Build completes with 0 errors
- [ ] `UnifiWatch.exe` exists in publish directory
- [ ] File size is reasonable (> 50 MB for self-contained)




---

## Test 2: Service Installation

### Steps

1. Navigate to publish directory:

   ```powershell
   cd bin\Release\net9.0\win-x64\publish
   ```

2. Install service:

   ```powershell
   .\UnifiWatch.exe --install-service
   ```

3. Verify service installed:

   ```powershell
   Get-Service -Name "UnifiWatch" -ErrorAction SilentlyContinue
   ```

### Expected Results

- [ ] Service installs without errors
- [ ] Service appears in `Get-Service` output
- [ ] Service visible in Services.msc
- [ ] Service Description: "Monitors Ubiquiti product stock availability and sends notifications"
- [ ] Startup Type: Automatic
- [ ] Service Status: Running (or Stopped if not auto-started)




---

## Test 3: Configuration Wizard & Credential Storage

**Note**: This test stores credentials securely in Windows Credential Manager. Credentials are NOT stored in the config file (security best practice).

### Steps

1. Install CredentialManager module (required for credential verification):

   ```powershell
   if (-not (Get-Module -ListAvailable -Name CredentialManager)) {
       Install-Module -Name CredentialManager -Force -Scope CurrentUser
   }
   ```

2. Run configuration wizard:

   ```powershell
   .\UnifiWatch.exe --configure
   ```

3. Follow prompts and configure:
   - **Product ID**: `UDM-SE` (or any valid product ID)
   - **Product Name**: `UniFi Dream Machine SE`
   - **Check Interval**: `60` seconds
   - **Email Notifications**: `yes`
     - **Authentication Method**:
       - **Option 1: SMTP (Traditional)**
         - SMTP Server: `smtp.gmail.com`
         - SMTP Port: `587`
         - Use TLS: `yes`
         - From Address: (your email)
         - Recipients: (your email)
         - SMTP Username: (your email)
         - **SMTP Password**: (app password - stored securely in Credential Manager)
       - **Option 2: OAuth 2.0 (Microsoft Graph)**
         - Requires Azure AD application registration with Mail.Send permission
         - Azure AD Tenant ID: (your tenant GUID)
         - Application (Client) ID: (your app ID)
         - Mailbox Email: (shared mailbox or service account email)
         - Client Secret: (stored securely in Credential Manager)
   - **SMS Notifications**: `no` (or configure Twilio if available)
   - **Desktop Notifications**: `yes`

   **Important**: When the wizard prompts for passwords/secrets, they are automatically stored securely in Windows Credential Manager. Do not skip this step.

4. Verify configuration saved:

   ```powershell
   .\UnifiWatch.exe --show-config
   ```

5. Verify credentials stored in Credential Manager:

   ```powershell
   # List UnifiWatch credentials (email and OAuth)
   Get-StoredCredential -Target "UnifiWatch:email-smtp"   # For SMTP
   Get-StoredCredential -Target "UnifiWatch:email-oauth"  # For OAuth
   ```

   **Expected output**: Should show credentials like `UnifiWatch:email-smtp` or `UnifiWatch:email-oauth`

6. Retrieve and verify the stored credential works:

   ```powershell
   # For SMTP:
   $credential = Get-StoredCredential -Target "UnifiWatch:email-smtp"
   $credential.UserName
   $credential.Password  # This will be the app password you entered

   # For OAuth:
   $credential = Get-StoredCredential -Target "UnifiWatch:email-oauth"
   $credential.Password  # This will be the client secret you entered
   ```

### Expected Results

- [ ] Wizard completes without errors
- [ ] Configuration file created at `%APPDATA%\UnifiWatch\config.json`
- [ ] `--show-config` displays saved configuration
- [ ] Sensitive data (passwords/secrets) redacted in `--show-config` output (shows `***REDACTED***` instead)
- [ ] `Get-StoredCredential -Target "UnifiWatch*"` returns credential object(s)
- [ ] Credential entry named "UnifiWatch:email-smtp" or "UnifiWatch:email-oauth" exists and is retrievable
- [ ] Retrieved credential contains correct username/password or client secret




---

## Test 4: Service Start and Status

### Steps

1. Start the service:

   ```powershell
   Start-Service -Name "UnifiWatch"
   ```

2. Check service status:

   ```powershell
   Get-Service -Name "UnifiWatch"
   ```

3. Wait 10 seconds, then check status again:

   ```powershell
   Start-Sleep -Seconds 10
   Get-Service -Name "UnifiWatch"
   ```

4. Verify service is running:

   ```powershell
   Get-Process -Name "UnifiWatch" -ErrorAction SilentlyContinue
   ```

### Expected Results

- [ ] Service starts without errors
- [ ] Service status shows "Running"
- [ ] Process appears in `Get-Process` output
- [ ] Service remains running after 10 seconds (no crash)




---

## Test 5: Windows Event Viewer Logs

### Steps

1. Open Event Viewer:

   ```powershell
   eventvwr
   ```

2. Navigate to:
   - **Windows Logs** → **Application**

3. Filter by source "UnifiWatch" or search for recent events

4. Look for entries:
   - Service start message
   - Configuration loaded message
   - Stock check started message
   - Any error or warning messages

### Expected Results

- [ ] Service start event logged
- [ ] Configuration loaded successfully
- [ ] No critical errors
- [ ] Stock monitoring started




---

## Test 6: Desktop Notifications

**Note**: Desktop notifications use PowerShell to invoke Windows.UI.Notifications APIs. They work in interactive mode but may not display in Windows Service mode (Session 0 isolation). Email and SMS notifications are recommended for service deployments.

**Testing Approach**: Desktop notifications only trigger when a product changes from out-of-stock to in-stock. To test this reliably:

### Steps

1. Run UnifiWatch in wait mode monitoring a specific product:

   ```powershell
   .\UnifiWatch.exe --wait --store USA --product-skus UDM-SE --seconds 30
   ```

2. Wait for the stock check cycle (30 seconds in this example)

3. When a product is detected as in-stock, verify toast notification appears

4. Check Windows Action Center for notification history

5. Press Ctrl+C to stop monitoring

### Expected Results

- [ ] Desktop toast notification appears when product becomes available
- [ ] Notification shows product name and status
- [ ] Notification includes Ubiquiti branding/logo (if applicable)
- [ ] Notification persists in Action Center
- [ ] Clicking notification (if interactive) opens product page

**Alternative**: If no products are currently in stock, this test can be skipped. Email and SMS notifications (Tests 7-8) are more reliable for service deployments.




---

## Test 7: Email Notifications

### Steps

1. Verify email configured in config:

   ```powershell
   .\UnifiWatch.exe --show-config
   ```
   
   Check that email section shows either:
   - SMTP configuration with server, port, from address, and recipients, **OR**
   - OAuth configuration with tenant ID, client ID, and mailbox email

2. Send a test email:

   ```powershell
   .\UnifiWatch.exe --test-email
   ```
   
   When using OAuth, you should see info messages like:
   - "OAuth token acquired, expires in X seconds"
   - "Email sent successfully to..."

3. Check email inbox for notification

4. Verify email contents:
   - Subject line mentions "UnifiWatch" or product name
   - Body contains product details (name, price, URL)
   - From address matches configured address (SMTP) or mailbox email (OAuth)
   - TLS encryption used (check email headers)

### Expected Results

- [ ] Email received successfully
- [ ] Subject line clear and informative
- [ ] Body contains product information
- [ ] No credential exposure in email
- [ ] Email formatted properly (HTML or plain text)




---

## Test 8: Configuration Reload

### Steps

1. While service is running, modify config:

   ```powershell
   notepad "$env:APPDATA\UnifiWatch\config.json"
   ```

2. Change check interval from `60` to `120` seconds

3. Save and close file

4. Wait 2-3 minutes

5. Check Event Viewer for config reload message

6. Verify new interval takes effect (monitor logs for next check)

### Expected Results

- [ ] Service detects config file change
- [ ] Configuration reloaded without service restart
- [ ] New interval takes effect
- [ ] No service interruption
- [ ] Event logged for config reload




---

## Test 9: Service Stop and Restart

### Steps

1. Stop the service:

   ```powershell
   Stop-Service -Name "UnifiWatch"
   ```

2. Verify service stopped:

   ```powershell
   Get-Service -Name "UnifiWatch"
   ```

3. Wait 5 seconds

4. Restart the service:

   ```powershell
   Start-Service -Name "UnifiWatch"
   ```

5. Verify service running again:

   ```powershell
   Get-Service -Name "UnifiWatch"
   ```

### Expected Results

- [ ] Service stops cleanly (no errors)
- [ ] Status shows "Stopped"
- [ ] Service starts successfully after restart
- [ ] Configuration reloaded on restart
- [ ] Monitoring resumes after restart




---

## Test 10: Credential Update

### Steps

1. Update email password/secret via wizard:

   ```powershell
   .\UnifiWatch.exe --configure
   ```

2. When prompted for email password (SMTP) or client secret (OAuth), enter new value (or same value)

3. Verify credential updated in Credential Manager:

   ```powershell
   # Install CredentialManager module if not already installed
   if (-not (Get-Module -ListAvailable -Name CredentialManager)) {
       Install-Module -Name CredentialManager -Force -Scope CurrentUser
       Import-Module CredentialManager
   }
   
   # List UnifiWatch credentials (SMTP and/or OAuth)
   Get-StoredCredential -Target "UnifiWatch*"
   ```

4. Check `UnifiWatch:email-smtp` or `UnifiWatch:email-oauth` entry appears in the list

5. Restart service to pick up new credentials:

   ```powershell
   Restart-Service -Name "UnifiWatch"
   ```

6. Verify service starts successfully with updated credentials

### Expected Results

- [ ] Credential update completes without errors
- [ ] Credential Manager shows updated entry
- [ ] Service restarts successfully
- [ ] Email notifications still work with new credentials




---

## Test 11: Log File Creation

### Steps

1. Check log directory exists:

   ```powershell
   Test-Path "$env:APPDATA\UnifiWatch\logs"
   ```

2. List log files:

   ```powershell
   Get-ChildItem "$env:APPDATA\UnifiWatch\logs"
   ```

3. View latest log:

   ```powershell
   Get-Content "$env:APPDATA\UnifiWatch\logs\*.log" -Tail 20
   ```

4. Verify log entries:
   - Service start timestamp
   - Configuration loaded
   - Stock checks performed
   - Notification attempts

### Expected Results

- [ ] Log directory exists
- [ ] Log files created (e.g., `service-YYYYMMDD.log`)
- [ ] Log entries are timestamped
- [ ] Logs contain service lifecycle events
- [ ] No sensitive data (passwords) in logs




---

## Test 12: Service Crash Recovery

### Steps

1. Ensure service is running

2. Simulate crash by killing process:

   ```powershell
   Stop-Process -Name "UnifiWatch" -Force
   ```

3. Wait 10 seconds

4. Check service status:

   ```powershell
   Get-Service -Name "UnifiWatch"
   ```

5. Check if service auto-restarted

6. Review Event Viewer for crash/restart logs

### Expected Results

- [ ] Service detects process termination
- [ ] Service attempts auto-restart (if configured)
- [ ] Service restarts successfully (or remains stopped based on configuration)
- [ ] Event logged for unexpected termination




---

## Test 13: Service Uninstall

### Steps

1. Stop the service:

   ```powershell
   Stop-Service -Name "UnifiWatch"
   ```

2. Uninstall service:

   ```powershell
   .\UnifiWatch.exe --uninstall-service
   ```

3. Verify service removed:

   ```powershell
   Get-Service -Name "UnifiWatch" -ErrorAction SilentlyContinue
   ```

4. Check Services.msc to confirm service not listed

5. Verify config files remain (not deleted):

   ```powershell
   Test-Path "$env:APPDATA\UnifiWatch\config.json"
   ```

6. Verify credentials remain in Credential Manager:

   ```powershell
   # Install CredentialManager module if not already installed
   if (-not (Get-Module -ListAvailable -Name CredentialManager)) {
       Install-Module -Name CredentialManager -Force -Scope CurrentUser
   }
   
   # List UnifiWatch credentials
   Get-StoredCredential -Target "UnifiWatch*"
   ```

### Expected Results

- [ ] Service uninstalls without errors
- [ ] Service no longer appears in `Get-Service`
- [ ] Service not visible in Services.msc
- [ ] Configuration files preserved (not deleted)
- [ ] Credentials preserved in Credential Manager
- [ ] Logs preserved




---

## Test 14: Clean Reinstall

### Steps

1. Reinstall service:

   ```powershell
   .\UnifiWatch.exe --install-service
   ```

2. Verify service uses existing configuration:

   ```powershell
   Get-Service -Name "UnifiWatch"
   ```

3. Start service:

   ```powershell
   Start-Service -Name "UnifiWatch"
   ```

4. Verify service resumes monitoring with previous config

5. Verify credentials still work (email notifications)

### Expected Results

- [ ] Service reinstalls successfully
- [ ] Service picks up existing configuration automatically
- [ ] Service starts without needing reconfiguration
- [ ] Credentials still accessible from Credential Manager
- [ ] Monitoring resumes immediately




---

## Test 15: Edge Case - No Internet Connection

### Steps

1. Ensure service is running

2. Disable network adapter:

   ```powershell
   # Identify adapter
   Get-NetAdapter
   
   # Disable adapter (replace with your adapter name)
   Disable-NetAdapter -Name "Ethernet" -Confirm:$false
   ```

3. Wait for next stock check cycle

4. Check logs for error handling:

   ```powershell
   Get-Content "$env:APPDATA\UnifiWatch\logs\*.log" -Tail 20
   ```

5. Re-enable network:

   ```powershell
   Enable-NetAdapter -Name "Ethernet" -Confirm:$false
   ```

6. Verify service recovers and resumes checks

### Expected Results

- [ ] Service handles network error gracefully
- [ ] Error logged (not crash)
- [ ] Service remains running during network outage
- [ ] Service resumes checks when network restored
- [ ] Retry logic works as expected




---

## Test 16: Edge Case - Invalid Credentials

### Steps

1. Update config with invalid email password:

   ```powershell
   .\UnifiWatch.exe --configure
   ```

2. Enter incorrect SMTP password

3. Restart service

4. Wait for email notification attempt

5. Check logs for authentication error

6. Verify service continues running (doesn't crash)

### Expected Results

- [ ] Service logs authentication error
- [ ] Error message is clear and actionable
- [ ] Service continues running (not terminated)
- [ ] Desktop notifications still work
- [ ] Service retries on next check




---

## Test 17: Edge Case - Config File Corruption

### Steps

1. Stop service:

   ```powershell
   Stop-Service -Name "UnifiWatch"
   ```

2. Corrupt config file:

   ```powershell
   "{ invalid json" | Out-File "$env:APPDATA\UnifiWatch\config.json"
   ```

3. Attempt to start service:

   ```powershell
   Start-Service -Name "UnifiWatch"
   ```

4. Check Event Viewer for error

5. Restore valid config:

   ```powershell
   Copy-Item "$env:APPDATA\UnifiWatch\config.backup.json" "$env:APPDATA\UnifiWatch\config.json"
   ```

6. Start service again

### Expected Results

- [ ] Service detects corrupted config
- [ ] Clear error message logged
- [ ] Service fails to start (expected behavior)
- [ ] Backup config available
- [ ] Service starts successfully with restored config




---

## Test 18: Performance and Resource Usage

### Steps

1. Ensure service running for at least 5 minutes

2. Monitor CPU usage:

   ```powershell
   Get-Counter "\Process(UnifiWatch)\% Processor Time" -SampleInterval 1 -MaxSamples 10
   ```

3. Monitor memory usage:

   ```powershell
   Get-Process -Name "UnifiWatch" | Select-Object Name, CPU, WorkingSet, VirtualMemorySize
   ```

4. Monitor for 5-10 stock check cycles

5. Check for memory leaks (increasing memory over time)

### Expected Results

- [ ] CPU usage < 5% average (idle between checks)
- [ ] Memory usage stable (< 200 MB)
- [ ] No memory leaks (memory doesn't continuously increase)
- [ ] Disk I/O minimal (only during config changes)




---

## Summary

### Test Results Overview

| Test # | Test Name | Status | Notes |
|--------|-----------|--------|-------|
| 1 | Build and Publish | ☐ Pass ☐ Fail | |
| 2 | Service Installation | ☐ Pass ☐ Fail | |
| 3 | Configuration Wizard | ☐ Pass ☐ Fail | |
| 4 | Service Start/Status | ☐ Pass ☐ Fail | |
| 5 | Event Viewer Logs | ☐ Pass ☐ Fail | |
| 6 | Desktop Notifications | ☐ Pass ☐ Fail | |
| 7 | Email Notifications | ☐ Pass ☐ Fail | |
| 8 | Configuration Reload | ☐ Pass ☐ Fail | |
| 9 | Stop/Restart | ☐ Pass ☐ Fail | |
| 10 | Credential Update | ☐ Pass ☐ Fail | |
| 11 | Log File Creation | ☐ Pass ☐ Fail | |
| 12 | Crash Recovery | ☐ Pass ☐ Fail | |
| 13 | Service Uninstall | ☐ Pass ☐ Fail | |
| 14 | Clean Reinstall | ☐ Pass ☐ Fail | |
| 15 | No Internet | ☐ Pass ☐ Fail | |
| 16 | Invalid Credentials | ☐ Pass ☐ Fail | |
| 17 | Config Corruption | ☐ Pass ☐ Fail | |
| 18 | Performance | ☐ Pass ☐ Fail | |

**Total Pass**: _____ / 18  
**Total Fail**: _____ / 18

### Critical Issues Found

1. _________________________________________________________________

2. _________________________________________________________________

3. _________________________________________________________________

### Non-Critical Issues

1. _________________________________________________________________

2. _________________________________________________________________

### Recommendations

1. _________________________________________________________________

2. _________________________________________________________________

### Sign-Off

**Tester Name**: ____________________  
**Date**: ____________________  
**Windows Version**: ____________________  
**Overall Status**: ☐ Approved for Release  ☐ Needs Fixes





