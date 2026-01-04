# macOS End User Guide

**Audience**: QA, support, and deployment engineers validating the macOS launchd service

**How to use this guide**: Run each command and confirm the expected behavior. No need to fill blanks unless you're capturing evidence for a defect.

## Overview

This guide provides a comprehensive manual testing checklist for UnifiWatch service mode on macOS. Complete all tests to verify proper installation, configuration, notification delivery, and service management using launchd.

---

## Prerequisites

- [ ] macOS 11 Big Sur or later
- [ ] .NET 9.0 Runtime installed
- [ ] Terminal access
- [ ] Internet connection for API access

**Test Environment**:
- macOS Version: ____________________
- .NET Version: ____________________
- Hardware: ____________________ (Intel / Apple Silicon)
- Test Date: ____________________

---

## Test 1: Build and Publish

### Steps

1. Open Terminal

2. Clone the repository:

   ```bash
   cd ~
   git clone https://github.com/johnmccrae/UnifiWatch.git
   cd UnifiWatch
   ```

   **Note**: If you already have the repository cloned, navigate to it: `cd ~/UnifiWatch`

3. Build release version:
   ```bash
   dotnet publish -c Release -r osx-x64 --self-contained -p:PublishSingleFile=true
   ```

4. Verify executable created:
   ```bash
   ls -lh bin/Release/net9.0/osx-x64/publish/UnifiWatch
   ```

5. Make executable:
   ```bash
   chmod +x bin/Release/net9.0/osx-x64/publish/UnifiWatch
   ```

### Expected Results

- [ ] Build completes with 0 errors
- [ ] `UnifiWatch` executable exists in publish directory
- [ ] File has execute permissions
- [ ] File size is reasonable (> 50 MB for self-contained)

---

## Test 2: Service Installation

### Steps

1. Navigate to publish directory:
   ```bash
   cd bin/Release/net9.0/osx-x64/publish
   ```

2. Install service (user-level launchd):
   ```bash
   ./UnifiWatch --install-service
   ```

3. Verify launchd plist file created:
   ```bash
   ls -la ~/Library/LaunchAgents/com.unifiwatch.plist
   ```

4. View plist contents:
   ```bash
   cat ~/Library/LaunchAgents/com.unifiwatch.plist
   ```

5. Verify service loaded:
   ```bash
   launchctl list | grep com.unifiwatch
   ```

### Expected Results

- [ ] Service installs without errors
- [ ] Plist file created at `~/Library/LaunchAgents/com.unifiwatch.plist`
- [ ] Plist contains correct executable path
- [ ] Service shows in `launchctl list` output
- [ ] Service configured to run at load (`RunAtLoad = true`)

---

## Test 3: Configuration Wizard

### Steps

1. Run configuration wizard:
   ```bash
   ./UnifiWatch --configure
   ```

2. Follow prompts and configure:
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
         - SMTP Password: (app password - stored securely in Keychain)
       - **Option 2: OAuth 2.0 (Microsoft Graph)**
         - Requires Azure AD application with Mail.Send permission
         - Azure AD Tenant ID: (your tenant GUID)
         - Application (Client) ID: (your app ID)
         - Mailbox Email: (shared mailbox or service account email)
         - Client Secret: (stored securely in Keychain)
   - **SMS Notifications**: `no` (or configure Twilio if available)
   - **Desktop Notifications**: `yes`

3. Verify configuration saved:
   ```bash
   ./UnifiWatch --show-config
   ```

4. Check config file location:
   ```bash
   cat ~/Library/Application\ Support/UnifiWatch/config.json
   ```

5. Verify credentials stored in Keychain:
   ```bash
   # Open Keychain Access
   open -a "Keychain Access"
   ```

6. In Keychain Access, search for "UnifiWatch"

7. Verify file permissions:
   ```bash
   stat -f "%Sp %N" ~/Library/Application\ Support/UnifiWatch/config.json
   ```

### Expected Results

- [ ] Wizard completes without errors
- [ ] Configuration file created at `~/Library/Application Support/UnifiWatch/config.json`
- [ ] Config file permissions: `-rw-------` (600)
- [ ] `--show-config` displays saved configuration
- [ ] Sensitive data (passwords/secrets) redacted in `--show-config` output
- [ ] Credentials visible in Keychain Access app
- [ ] Keychain entry named "UnifiWatch:email-smtp" or "UnifiWatch:email-oauth"

---

## Test 4: Service Start and Status

### Steps

1. Start the service (load into launchd):
   ```bash
   launchctl load ~/Library/LaunchAgents/com.unifiwatch.plist
   ```

2. Check service status:
   ```bash
   launchctl list | grep com.unifiwatch
   ```

3. Wait 10 seconds, then check status again:
   ```bash
   sleep 10
   launchctl list | grep com.unifiwatch
   ```

4. Verify process is running:
   ```bash
   ps aux | grep UnifiWatch
   ```

### Expected Results

- [ ] Service loads without errors
- [ ] Service appears in `launchctl list` output
- [ ] Status shows PID (not "-" which indicates not running)
- [ ] Process appears in `ps aux` output
- [ ] Service remains running after 10 seconds (no crash)

---

## Test 5: Log File Viewing

### Steps

1. Check log directory exists:
   ```bash
   ls -la ~/Library/Logs/UnifiWatch/
   ```

2. View latest log file:
   ```bash
   tail -f ~/Library/Logs/UnifiWatch/service.log
   ```

3. Look for entries:
   - Service start message
   - Configuration loaded message
   - Stock check started message
   - Any error or warning messages

4. Alternative: Use Console app:
   ```bash
   open -a Console
   ```

5. In Console app:
   - Navigate to User Reports or Log Reports
   - Filter/search for "UnifiWatch"

### Expected Results

- [ ] Log directory exists
- [ ] Log file created (e.g., `service.log`)
- [ ] Log entries are timestamped
- [ ] Logs contain service lifecycle events
- [ ] No sensitive data (passwords) in logs
- [ ] Logs visible in Console app

---

## Test 6: Desktop Notifications

### Steps

1. Ensure service is running and configured for desktop notifications

2. Wait for next stock check cycle (default: 60 seconds)

3. If product is in stock, verify notification appears in Notification Center

4. Check Notification Center for notification history

### Expected Results

- [ ] Desktop notification appears
- [ ] Notification shows product name and status
- [ ] Notification uses macOS Notification Center
- [ ] Notification includes "Ubiquiti Stock Alert" subtitle (if applicable)
- [ ] Notification visible in Notification Center history

---

## Test 7: Email Notifications

### Steps

1. Verify email configured in config:
   ```bash
   ./UnifiWatch --show-config
   ```
   
   Check that email section shows either:
   - SMTP configuration with server, port, from address, and recipients, **OR**
   - OAuth configuration with tenant ID, client ID, and mailbox email

2. Send a test email:
   ```bash
   ./UnifiWatch --test-email
   ```

3. Check email inbox for notification

4. Verify email contents:
   - Subject line mentions "UnifiWatch" or product name
   - Body contains product details (name, price, URL)
   - From address matches configured address
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
   ```bash
   nano ~/Library/Application\ Support/UnifiWatch/config.json
   ```

2. Change check interval from `60` to `120` seconds

3. Save and close file

4. Wait 2-3 minutes

5. Check logs for config reload message:
   ```bash
   tail -f ~/Library/Logs/UnifiWatch/service.log
   ```

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

1. Stop the service (unload from launchd):
   ```bash
   launchctl unload ~/Library/LaunchAgents/com.unifiwatch.plist
   ```

2. Verify service stopped:
   ```bash
   launchctl list | grep com.unifiwatch
   ps aux | grep UnifiWatch
   ```

3. Wait 5 seconds

4. Restart the service:
   ```bash
   launchctl load ~/Library/LaunchAgents/com.unifiwatch.plist
   ```

5. Verify service running again:
   ```bash
   launchctl list | grep com.unifiwatch
   ```

### Expected Results

- [ ] Service stops cleanly (no errors)
- [ ] Service not in `launchctl list` after unload
- [ ] Process terminates (not in `ps aux`)
- [ ] Service starts successfully after reload
- [ ] Configuration reloaded on restart
- [ ] Monitoring resumes after restart

---

## Test 10: Keychain Credential Access

### Steps

1. Open Keychain Access app:
   ```bash
   open -a "Keychain Access"
   ```

2. Search for "UnifiWatch" in the search box

3. Double-click the "UnifiWatch:email-smtp" or "UnifiWatch:email-oauth" entry

4. Click "Show password" checkbox

5. Enter your macOS user password when prompted

6. Verify password/secret is stored correctly

7. Check "Access Control" tab to see which apps can access the credential

### Expected Results

- [ ] Credential entry visible in Keychain Access
- [ ] Entry stored in "login" keychain
- [ ] Password can be revealed with user authentication
- [ ] UnifiWatch app has access to credential
- [ ] Password matches configured SMTP password

---

## Test 11: Credential Update

### Steps

1. Update email password via wizard:
   ```bash
   ./UnifiWatch --configure
   ```

2. When prompted for email password, enter new value (or same value)

3. Verify credential updated in Keychain Access:
   ```bash
   open -a "Keychain Access"
   ```

4. Check timestamp on "UnifiWatch:email-smtp" entry (should show recent modification)

5. Restart service to pick up new credentials:
   ```bash
   launchctl unload ~/Library/LaunchAgents/com.unifiwatch.plist
   launchctl load ~/Library/LaunchAgents/com.unifiwatch.plist
   ```

6. Verify service starts successfully with updated credentials

### Expected Results

- [ ] Credential update completes without errors
- [ ] Keychain entry shows updated timestamp
- [ ] Service restarts successfully
- [ ] Email notifications still work with new credentials

---

## Test 12: Service Auto-Restart on Crash

### Steps

1. Ensure service is running

2. Find process ID:
   ```bash
   ps aux | grep UnifiWatch | grep -v grep
   ```

3. Simulate crash by killing process:
   ```bash
   kill -9 <PID>
   ```

4. Wait 10 seconds

5. Check if service restarted:
   ```bash
   launchctl list | grep com.unifiwatch
   ps aux | grep UnifiWatch
   ```

6. Check logs for crash/restart entries:
   ```bash
   tail -n 30 ~/Library/Logs/UnifiWatch/service.log
   ```

### Expected Results

- [ ] launchd detects process termination
- [ ] Service auto-restarts (based on `KeepAlive` configuration)
- [ ] Service running again after ~5-10 seconds
- [ ] Event logged for unexpected termination
- [ ] Event logged for auto-restart

---

## Test 13: Service Uninstall

### Steps

1. Unload the service:
   ```bash
   launchctl unload ~/Library/LaunchAgents/com.unifiwatch.plist
   ```

2. Uninstall service (remove plist):
   ```bash
   ./UnifiWatch --uninstall-service
   ```

3. Verify plist file removed:
   ```bash
   ls -la ~/Library/LaunchAgents/com.unifiwatch.plist
   ```

4. Verify service not listed:
   ```bash
   launchctl list | grep com.unifiwatch
   ```

5. Verify config files remain (not deleted):
   ```bash
   ls -la ~/Library/Application\ Support/UnifiWatch/config.json
   ```

6. Verify credentials remain in Keychain:
   ```bash
   open -a "Keychain Access"
   # Search for "UnifiWatch"
   ```

### Expected Results

- [ ] Service uninstalls without errors
- [ ] Plist file removed from `~/Library/LaunchAgents/`
- [ ] Service no longer appears in `launchctl list`
- [ ] Configuration files preserved (not deleted)
- [ ] Credentials preserved in Keychain
- [ ] Logs preserved

---

## Test 14: Clean Reinstall

### Steps

1. Reinstall service:
   ```bash
   ./UnifiWatch --install-service
   ```

2. Verify plist file recreated:
   ```bash
   ls -la ~/Library/LaunchAgents/com.unifiwatch.plist
   ```

3. Load service:
   ```bash
   launchctl load ~/Library/LaunchAgents/com.unifiwatch.plist
   ```

4. Verify service uses existing configuration:
   ```bash
   launchctl list | grep com.unifiwatch
   tail -f ~/Library/Logs/UnifiWatch/service.log
   ```

5. Verify credentials still work (email notifications)

### Expected Results

- [ ] Service reinstalls successfully
- [ ] Plist file recreated
- [ ] Service picks up existing configuration automatically
- [ ] Service starts without needing reconfiguration
- [ ] Credentials still accessible from Keychain
- [ ] Monitoring resumes immediately

---

## Test 15: Edge Case - No Internet Connection

### Steps

1. Ensure service is running

2. Disable Wi-Fi or disconnect Ethernet:
   ```bash
   # Via System Settings > Network, or:
   networksetup -setairportpower en0 off
   ```

3. Wait for next stock check cycle

4. Check logs for error handling:
   ```bash
   tail -f ~/Library/Logs/UnifiWatch/service.log
   ```

5. Re-enable network:
   ```bash
   networksetup -setairportpower en0 on
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
   ```bash
   ./UnifiWatch --configure
   ```

2. Enter incorrect SMTP password

3. Restart service:
   ```bash
   launchctl unload ~/Library/LaunchAgents/com.unifiwatch.plist
   launchctl load ~/Library/LaunchAgents/com.unifiwatch.plist
   ```

4. Wait for email notification attempt

5. Check logs for authentication error:
   ```bash
   tail -f ~/Library/Logs/UnifiWatch/service.log
   ```

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

1. Unload service:
   ```bash
   launchctl unload ~/Library/LaunchAgents/com.unifiwatch.plist
   ```

2. Backup config:
   ```bash
   cp ~/Library/Application\ Support/UnifiWatch/config.json ~/Library/Application\ Support/UnifiWatch/config.backup.json
   ```

3. Corrupt config file:
   ```bash
   echo "{ invalid json" > ~/Library/Application\ Support/UnifiWatch/config.json
   ```

4. Attempt to start service:
   ```bash
   launchctl load ~/Library/LaunchAgents/com.unifiwatch.plist
   ```

5. Check logs for error:
   ```bash
   tail -f ~/Library/Logs/UnifiWatch/service.log
   ```

6. Restore valid config:
   ```bash
   cp ~/Library/Application\ Support/UnifiWatch/config.backup.json ~/Library/Application\ Support/UnifiWatch/config.json
   ```

7. Start service again:
   ```bash
   launchctl unload ~/Library/LaunchAgents/com.unifiwatch.plist
   launchctl load ~/Library/LaunchAgents/com.unifiwatch.plist
   ```

### Expected Results

- [ ] Service detects corrupted config
- [ ] Clear error message logged
- [ ] Service fails to start (expected behavior)
- [ ] Service starts successfully with restored config

---

## Test 18: Performance and Resource Usage

### Steps

1. Ensure service running for at least 5 minutes

2. Monitor CPU usage:
   ```bash
   top -pid $(pgrep UnifiWatch)
   ```

3. Monitor memory usage:
   ```bash
   ps aux | grep UnifiWatch
   ```

4. Use Activity Monitor for detailed view:
   ```bash
   open -a "Activity Monitor"
   ```

5. In Activity Monitor, search for "UnifiWatch" and observe:
   - CPU usage
   - Memory usage
   - Energy impact
   - Network activity

6. Monitor for 5-10 stock check cycles

### Expected Results

- [ ] CPU usage < 5% average (idle between checks)
- [ ] Memory usage stable (< 200 MB)
- [ ] No memory leaks (memory doesn't continuously increase)
- [ ] Energy Impact: Low
- [ ] Network activity minimal (only during checks)

---

## Test 19: File Permissions Security

### Steps

1. Verify config directory permissions:
   ```bash
   stat -f "%Sp %Su %Sg %N" ~/Library/Application\ Support/UnifiWatch
   ```

2. Verify config file permissions:
   ```bash
   stat -f "%Sp %Su %Sg %N" ~/Library/Application\ Support/UnifiWatch/config.json
   ```

3. Attempt to read config as different user (should fail):
   ```bash
   sudo -u nobody cat ~/Library/Application\ Support/UnifiWatch/config.json
   ```

4. Verify log file permissions:
   ```bash
   stat -f "%Sp %Su %Sg %N" ~/Library/Logs/UnifiWatch/service.log
   ```

### Expected Results

- [ ] Config directory: `drwx------` (700)
- [ ] Config file: `-rw-------` (600)
- [ ] Log file: `-rw-------` (600)
- [ ] Owner: current user
- [ ] Group: staff (or current user's primary group)
- [ ] Other users cannot read files (permission denied)

---

## Test 20: System Reboot Persistence

### Steps

1. Ensure service is loaded and running

2. Verify service will start on login:
   ```bash
   cat ~/Library/LaunchAgents/com.unifiwatch.plist | grep RunAtLoad
   ```

3. Reboot the system:
   ```bash
   sudo reboot
   ```

4. After reboot and login, check if service started automatically:
   ```bash
   launchctl list | grep com.unifiwatch
   ps aux | grep UnifiWatch
   ```

5. Check logs for startup after reboot:
   ```bash
   tail -f ~/Library/Logs/UnifiWatch/service.log
   ```

### Expected Results

- [ ] Service configured to start at login (`RunAtLoad = true`)
- [ ] Service starts automatically after reboot
- [ ] Service appears in `launchctl list` after login
- [ ] Monitoring resumes automatically
- [ ] No manual intervention required

---

## Summary

### Test Results Overview

| Test # | Test Name | Status | Notes |
|--------|-----------|--------|-------|
| 1 | Build and Publish | ☐ Pass ☐ Fail | |
| 2 | Service Installation | ☐ Pass ☐ Fail | |
| 3 | Configuration Wizard | ☐ Pass ☐ Fail | |
| 4 | Service Start/Status | ☐ Pass ☐ Fail | |
| 5 | Log File Viewing | ☐ Pass ☐ Fail | |
| 6 | Desktop Notifications | ☐ Pass ☐ Fail | |
| 7 | Email Notifications | ☐ Pass ☐ Fail | |
| 8 | Configuration Reload | ☐ Pass ☐ Fail | |
| 9 | Stop/Restart | ☐ Pass ☐ Fail | |
| 10 | Keychain Access | ☐ Pass ☐ Fail | |
| 11 | Credential Update | ☐ Pass ☐ Fail | |
| 12 | Crash Recovery | ☐ Pass ☐ Fail | |
| 13 | Service Uninstall | ☐ Pass ☐ Fail | |
| 14 | Clean Reinstall | ☐ Pass ☐ Fail | |
| 15 | No Internet | ☐ Pass ☐ Fail | |
| 16 | Invalid Credentials | ☐ Pass ☐ Fail | |
| 17 | Config Corruption | ☐ Pass ☐ Fail | |
| 18 | Performance | ☐ Pass ☐ Fail | |
| 19 | File Permissions | ☐ Pass ☐ Fail | |
| 20 | Reboot Persistence | ☐ Pass ☐ Fail | |

**Total Pass**: _____ / 20  
**Total Fail**: _____ / 20

### macOS-Specific Notes

**Hardware**: [ ] Intel  [ ] Apple Silicon

**Keychain**: [ ] Local  [ ] iCloud Keychain

**launchd Type**: [ ] User Agent  [ ] System Daemon

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
**macOS Version**: ____________________  
**Overall Status**: ☐ Approved for Release  ☐ Needs Fixes



