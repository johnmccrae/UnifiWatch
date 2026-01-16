# Linux End User Guide

**Audience**: QA, support, and deployment engineers validating the Linux systemd service

**How to use this guide**: Run each command and confirm the expected behavior. No need to fill blanks unless you're capturing evidence for a defect.

## Overview

This guide provides a comprehensive manual testing checklist for UnifiWatch service mode on Linux. Complete all tests to verify proper installation, configuration, notification delivery, and service management using systemd.

---

## Prerequisites

- [ ] Linux distribution (Ubuntu 20.04+, Fedora 35+, Debian 11+, or equivalent)
- [ ] .NET 9.0 Runtime installed
- [ ] systemd init system
- [ ] sudo/root access for service installation
- [ ] Internet connection for API access

**Optional**:
- [ ] GNOME Keyring or KDE Wallet (for secret-service credential storage)
- [ ] Desktop environment (for desktop notifications)

**Test Environment**:
- Distribution: ____________________
- Kernel Version: ____________________
- .NET Version: ____________________
- systemd Version: ____________________
- Desktop Environment: ____________________ (or "headless")
- Test Date: ____________________

---

## Test 1: Build and Publish

### Steps

1. Open terminal

2. Clone the repository:

   ```bash
   cd ~
   git clone https://github.com/johnmccrae/UnifiWatch.git
   cd UnifiWatch
   ```

   **Note**: If you already have the repository cloned, navigate to it: `cd ~/UnifiWatch`

3. Build release version:
   ```bash
   dotnet publish -c Release -r linux-x64 --self-contained -p:PublishSingleFile=true
   ```

4. Verify executable created:
   ```bash
   ls -lh bin/Release/net9.0/linux-x64/publish/UnifiWatch
   ```

5. Make executable:
   ```bash
   chmod +x bin/Release/net9.0/linux-x64/publish/UnifiWatch
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
   cd bin/Release/net9.0/linux-x64/publish
   ```

2. Install service with sudo:
   ```bash
   sudo ./UnifiWatch --install-service
   ```

3. Verify systemd unit file created:
   ```bash
   sudo systemctl status unifiwatch
   ```

4. Check unit file contents:
   ```bash
   sudo cat /etc/systemd/system/unifiwatch.service
   ```

5. Verify service enabled:
   ```bash
   sudo systemctl is-enabled unifiwatch
   ```

### Expected Results

- [ ] Service installs without errors
- [ ] Unit file created at `/etc/systemd/system/unifiwatch.service`
- [ ] Service shows in `systemctl status` output
- [ ] Service enabled for auto-start
- [ ] Unit file contains correct executable path
- [ ] Unit file specifies restart policy (e.g., `Restart=always`)

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
         - SMTP Password: (app password - stored securely)
       - **Option 2: OAuth 2.0 (Microsoft Graph)**
         - Requires Azure AD application with Mail.Send permission
         - Azure AD Tenant ID: (your tenant GUID)
         - Application (Client) ID: (your app ID)
         - Mailbox Email: (shared mailbox or service account email)
         - Client Secret: (stored securely)
   - **SMS Notifications**: `no` (or configure Twilio if available)
   - **Desktop Notifications**: `yes` (if desktop environment available)

3. Verify configuration saved:
   ```bash
   ./UnifiWatch --show-config
   ```

4. Check config file location:
   ```bash
   cat ~/.config/UnifiWatch/config.json
   ```

5. Check credentials storage:
   ```bash
   # If secret-service available:
   ls -la ~/.local/share/keyrings/
   
   # If encrypted file fallback:
   ls -la ~/.config/UnifiWatch/credentials/
   ```

6. Verify file permissions:
   ```bash
   stat -c "%a %n" ~/.config/UnifiWatch/config.json
   stat -c "%a %n" ~/.config/UnifiWatch/credentials/* 2>/dev/null || echo "No credential files"
   ```

### Expected Results

- [ ] Wizard completes without errors
- [ ] Configuration file created at `~/.config/UnifiWatch/config.json`
- [ ] Config file permissions: `600` (readable only by user)
- [ ] `--show-config` displays saved configuration
- [ ] Sensitive data (passwords) redacted in `--show-config` output
- [ ] Credentials stored in secret-service (keyring) OR encrypted files
- [ ] Credential files have `600` permissions (if using encrypted fallback)

---

## Test 4: Service Start and Status

### Steps

1. Start the service:
   ```bash
   sudo systemctl start unifiwatch
   ```

2. Check service status:
   ```bash
   sudo systemctl status unifiwatch
   ```

3. Wait 10 seconds, then check status again:
   ```bash
   sleep 10
   sudo systemctl status unifiwatch
   ```

4. Verify process is running:
   ```bash
   ps aux | grep UnifiWatch
   ```

### Expected Results

- [ ] Service starts without errors
- [ ] Status shows "active (running)"
- [ ] Process appears in `ps aux` output
- [ ] Service remains running after 10 seconds (no crash)
- [ ] PID shown in status output

---

## Test 5: systemd Journal Logs

### Steps

1. View service logs:
   ```bash
   sudo journalctl -u unifiwatch -f
   ```

2. Look for entries:
   - Service start message
   - Configuration loaded message
   - Stock check started message
   - Any error or warning messages

3. View last 50 lines:
   ```bash
   sudo journalctl -u unifiwatch -n 50
   ```

4. Filter by priority (errors only):
   ```bash
   sudo journalctl -u unifiwatch -p err
   ```

### Expected Results

- [ ] Service start event logged
- [ ] Configuration loaded successfully
- [ ] No critical errors
- [ ] Stock monitoring started
- [ ] Timestamps on all entries

---

## Test 6: Desktop Notifications (Desktop Environment Only)

### Steps

1. Ensure service is running and desktop environment present

2. Verify `notify-send` available:
   ```bash
   which notify-send
   ```

3. Wait for next stock check cycle (default: 60 seconds)

4. If product is in stock, verify desktop notification appears

### Expected Results

- [ ] Desktop notification appears (if desktop environment available)
- [ ] Notification shows product name and status
- [ ] Notification includes appropriate icon
- [ ] Notification disappears after timeout or manual dismiss

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
   nano ~/.config/UnifiWatch/config.json
   ```

2. Change check interval from `60` to `120` seconds

3. Save and close file

4. Wait 2-3 minutes

5. Check journal for config reload message:
   ```bash
   sudo journalctl -u unifiwatch -n 20
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

1. Stop the service:
   ```bash
   sudo systemctl stop unifiwatch
   ```

2. Verify service stopped:
   ```bash
   sudo systemctl status unifiwatch
   ```

3. Wait 5 seconds

4. Restart the service:
   ```bash
   sudo systemctl start unifiwatch
   ```

5. Verify service running again:
   ```bash
   sudo systemctl status unifiwatch
   ```

### Expected Results

- [ ] Service stops cleanly (no errors)
- [ ] Status shows "inactive (dead)"
- [ ] Service starts successfully after restart
- [ ] Configuration reloaded on restart
- [ ] Monitoring resumes after restart

---

## Test 10: Credential Update

### Steps

1. Update email password/secret via wizard:
   ```bash
   ./UnifiWatch --configure
   ```

2. When prompted for email password (SMTP) or client secret (OAuth), enter new value (or same value)

3. Verify credential updated:
   ```bash
   # If using secret-service, credentials are in keyring
   # If using encrypted files:
   ls -l ~/.config/UnifiWatch/credentials/
   stat -c "%a %n" ~/.config/UnifiWatch/credentials/*
   ```

4. Restart service to pick up new credentials:
   ```bash
   sudo systemctl restart unifiwatch
   ```

5. Verify service starts successfully with updated credentials

### Expected Results

- [ ] Credential update completes without errors
- [ ] Credential files/keyring entries updated
- [ ] File permissions remain `600` (if using encrypted files)
- [ ] Service restarts successfully
- [ ] Email notifications still work with new credentials

---

## Test 11: Log File Creation (Optional)

### Steps

1. Check if application creates log files (in addition to journald):
   ```bash
   ls -la ~/.config/UnifiWatch/logs/
   ```

2. If log directory exists, view latest log:
   ```bash
   tail -f ~/.config/UnifiWatch/logs/*.log
   ```

3. Verify log entries:
   - Service start timestamp
   - Configuration loaded
   - Stock checks performed
   - Notification attempts

4. Check log file permissions:
   ```bash
   stat -c "%a %n" ~/.config/UnifiWatch/logs/*.log
   ```

### Expected Results

- [ ] Log directory exists (if application creates files)
- [ ] Log files created (e.g., `service-YYYYMMDD.log`)
- [ ] Log entries are timestamped
- [ ] Logs contain service lifecycle events
- [ ] No sensitive data (passwords) in logs
- [ ] Log file permissions: `600`

---

## Test 12: Service Auto-Restart on Failure

### Steps

1. Ensure service is running

2. Simulate crash by killing process:
   ```bash
   sudo pkill -9 UnifiWatch
   ```

3. Wait 10 seconds

4. Check service status:
   ```bash
   sudo systemctl status unifiwatch
   ```

5. Check journal for crash/restart logs:
   ```bash
   sudo journalctl -u unifiwatch -n 30
   ```

### Expected Results

- [ ] systemd detects process termination
- [ ] Service auto-restarts (based on `Restart=always` policy)
- [ ] Service running again after ~5-10 seconds
- [ ] Event logged for unexpected termination
- [ ] Event logged for auto-restart

---

## Test 13: Service Disable and Enable

### Steps

1. Disable service from auto-start:
   ```bash
   sudo systemctl disable unifiwatch
   ```

2. Verify disabled:
   ```bash
   sudo systemctl is-enabled unifiwatch
   ```

3. Reboot system (optional):
   ```bash
   sudo reboot
   ```

4. After reboot, check if service started:
   ```bash
   sudo systemctl status unifiwatch
   ```

5. Re-enable service:
   ```bash
   sudo systemctl enable unifiwatch
   ```

6. Start service:
   ```bash
   sudo systemctl start unifiwatch
   ```

### Expected Results

- [ ] Service disables without errors
- [ ] `is-enabled` shows "disabled"
- [ ] Service does not start on reboot (when disabled)
- [ ] Service re-enables successfully
- [ ] Service starts after re-enabling

---

## Test 14: Service Uninstall

### Steps

1. Stop the service:
   ```bash
   sudo systemctl stop unifiwatch
   ```

2. Uninstall service:
   ```bash
   sudo ./UnifiWatch --uninstall-service
   ```

3. Verify unit file removed:
   ```bash
   ls -la /etc/systemd/system/unifiwatch.service
   ```

4. Verify service not listed:
   ```bash
   sudo systemctl status unifiwatch
   ```

5. Verify config files remain (not deleted):
   ```bash
   ls -la ~/.config/UnifiWatch/config.json
   ```

6. Verify credentials remain:
   ```bash
   ls -la ~/.config/UnifiWatch/credentials/
   ```

### Expected Results

- [ ] Service uninstalls without errors
- [ ] Unit file removed from `/etc/systemd/system/`
- [ ] Service no longer appears in `systemctl status`
- [ ] Configuration files preserved (not deleted)
- [ ] Credentials preserved (keyring or encrypted files)
- [ ] Logs preserved

---

## Test 15: Clean Reinstall

### Steps

1. Reinstall service:
   ```bash
   sudo ./UnifiWatch --install-service
   ```

2. Verify service installed:
   ```bash
   sudo systemctl status unifiwatch
   ```

3. Start service:
   ```bash
   sudo systemctl start unifiwatch
   ```

4. Verify service uses existing configuration:
   ```bash
   sudo journalctl -u unifiwatch -n 20
   ```

5. Verify credentials still work (email notifications)

### Expected Results

- [ ] Service reinstalls successfully
- [ ] Service picks up existing configuration automatically
- [ ] Service starts without needing reconfiguration
- [ ] Credentials still accessible (keyring or encrypted files)
- [ ] Monitoring resumes immediately

---

## Test 16: Edge Case - No Internet Connection

### Steps

1. Ensure service is running

2. Disable network interface:
   ```bash
   # Identify interface
   ip link show
   
   # Disable interface (replace with your interface name)
   sudo ip link set eth0 down
   ```

3. Wait for next stock check cycle

4. Check journal for error handling:
   ```bash
   sudo journalctl -u unifiwatch -n 20
   ```

5. Re-enable network:
   ```bash
   sudo ip link set eth0 up
   ```

6. Verify service recovers and resumes checks

### Expected Results

- [ ] Service handles network error gracefully
- [ ] Error logged (not crash)
- [ ] Service remains running during network outage
- [ ] Service resumes checks when network restored
- [ ] Retry logic works as expected

---

## Test 17: Edge Case - Invalid Credentials

### Steps

1. Update config with invalid email password:
   ```bash
   ./UnifiWatch --configure
   ```

2. Enter incorrect SMTP password

3. Restart service:
   ```bash
   sudo systemctl restart unifiwatch
   ```

4. Wait for email notification attempt

5. Check journal for authentication error:
   ```bash
   sudo journalctl -u unifiwatch -n 30
   ```

6. Verify service continues running (doesn't crash)

### Expected Results

- [ ] Service logs authentication error
- [ ] Error message is clear and actionable
- [ ] Service continues running (not terminated)
- [ ] Desktop notifications still work (if applicable)
- [ ] Service retries on next check

---

## Test 18: Edge Case - Config File Corruption

### Steps

1. Stop service:
   ```bash
   sudo systemctl stop unifiwatch
   ```

2. Backup config:
   ```bash
   cp ~/.config/UnifiWatch/config.json ~/.config/UnifiWatch/config.backup.json
   ```

3. Corrupt config file:
   ```bash
   echo "{ invalid json" > ~/.config/UnifiWatch/config.json
   ```

4. Attempt to start service:
   ```bash
   sudo systemctl start unifiwatch
   ```

5. Check journal for error:
   ```bash
   sudo journalctl -u unifiwatch -n 20
   ```

6. Restore valid config:
   ```bash
   cp ~/.config/UnifiWatch/config.backup.json ~/.config/UnifiWatch/config.json
   ```

7. Start service again:
   ```bash
   sudo systemctl start unifiwatch
   ```

### Expected Results

- [ ] Service detects corrupted config
- [ ] Clear error message logged in journal
- [ ] Service fails to start (expected behavior)
- [ ] Service starts successfully with restored config

---

## Test 19: Performance and Resource Usage

### Steps

1. Ensure service running for at least 5 minutes

2. Monitor CPU usage:
   ```bash
   top -p $(pgrep UnifiWatch)
   ```

3. Monitor memory usage:
   ```bash
   ps aux | grep UnifiWatch
   ```

4. Monitor for 5-10 stock check cycles

5. Check for memory leaks:
   ```bash
   # Sample memory every 30 seconds for 5 minutes
   for i in {1..10}; do
     ps -p $(pgrep UnifiWatch) -o rss=
     sleep 30
   done
   ```

### Expected Results

- [ ] CPU usage < 5% average (idle between checks)
- [ ] Memory usage stable (< 200 MB RSS)
- [ ] No memory leaks (memory doesn't continuously increase)
- [ ] Disk I/O minimal (only during config changes)

---

## Test 20: File Permissions Security

### Steps

1. Verify config directory permissions:
   ```bash
   stat -c "%a %U %G %n" ~/.config/UnifiWatch
   ```

2. Verify config file permissions:
   ```bash
   stat -c "%a %U %G %n" ~/.config/UnifiWatch/config.json
   ```

3. Verify credential file/directory permissions (if using encrypted files):
   ```bash
   stat -c "%a %U %G %n" ~/.config/UnifiWatch/credentials
   stat -c "%a %U %G %n" ~/.config/UnifiWatch/credentials/*
   ```

4. Attempt to read config as different user (should fail):
   ```bash
   sudo -u nobody cat ~/.config/UnifiWatch/config.json
   ```

### Expected Results

- [ ] Config directory: `700` (drwx------)
- [ ] Config file: `600` (-rw-------)
- [ ] Credentials directory: `700` (drwx------) [if applicable]
- [ ] Credential files: `600` (-rw-------) [if applicable]
- [ ] Owner: current user
- [ ] Group: current user's primary group
- [ ] Other users cannot read files (permission denied)

---

## Summary

### Test Results Overview

| Test # | Test Name | Status | Notes |
|--------|-----------|--------|-------|
| 1 | Build and Publish | ☐ Pass ☐ Fail | |
| 2 | Service Installation | ☐ Pass ☐ Fail | |
| 3 | Configuration Wizard | ☐ Pass ☐ Fail | |
| 4 | Service Start/Status | ☐ Pass ☐ Fail | |
| 5 | journalctl Logs | ☐ Pass ☐ Fail | |
| 6 | Desktop Notifications | ☐ Pass ☐ Fail ☐ N/A | |
| 7 | Email Notifications | ☐ Pass ☐ Fail | |
| 8 | Configuration Reload | ☐ Pass ☐ Fail | |
| 9 | Stop/Restart | ☐ Pass ☐ Fail | |
| 10 | Credential Update | ☐ Pass ☐ Fail | |
| 11 | Log File Creation | ☐ Pass ☐ Fail ☐ N/A | |
| 12 | Auto-Restart on Failure | ☐ Pass ☐ Fail | |
| 13 | Disable/Enable | ☐ Pass ☐ Fail | |
| 14 | Service Uninstall | ☐ Pass ☐ Fail | |
| 15 | Clean Reinstall | ☐ Pass ☐ Fail | |
| 16 | No Internet | ☐ Pass ☐ Fail | |
| 17 | Invalid Credentials | ☐ Pass ☐ Fail | |
| 18 | Config Corruption | ☐ Pass ☐ Fail | |
| 19 | Performance | ☐ Pass ☐ Fail | |
| 20 | File Permissions | ☐ Pass ☐ Fail | |

**Total Pass**: _____ / 20  
**Total Fail**: _____ / 20

### Platform-Specific Notes

**Distribution**: ____________________

**Credential Storage Method**: [ ] secret-service  [ ] encrypted-file

**Desktop Environment**: ____________________ (or "headless")

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
**Linux Distribution**: ____________________  
**Overall Status**: ☐ Approved for Release  ☐ Needs Fixes

