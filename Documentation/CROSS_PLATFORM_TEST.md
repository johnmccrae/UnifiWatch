# Cross-Platform Integration Testing Guide

## Overview

This guide provides comprehensive testing procedures to verify UnifiWatch behaves identically across Windows, Linux, and macOS platforms. Complete these tests after individual platform testing is finished.

---

## Prerequisites

- [ ] Windows deployment testing completed (WINDOWS_END_USER_GUIDE.md)
- [ ] Linux deployment testing completed (LINUX_END_USER_GUIDE.md)
- [ ] macOS deployment testing completed (MACOS_END_USER_GUIDE.md)
- [ ] All three platforms available for simultaneous testing
- [ ] Identical test email account configured on all platforms
- [ ] Identical product configured for monitoring

**Test Environment**:

| Platform | Version | .NET Version | Test Date |
|----------|---------|--------------|-----------|
| Windows | ________ | ____________ | _________ |
| Linux | ________ | ____________ | _________ |
| macOS | ________ | ____________ | _________ |

---

## Test 1: Identical Configuration Setup

### Objective
Verify the same configuration file works correctly on all three platforms.

### Steps

1. Create baseline config file on Windows:
   ```powershell
   .\UnifiWatch.exe --configure
   ```

2. Configure with:
   - Product ID: `UDM-SE`
   - Product Name: `UniFi Dream Machine SE`
   - Check Interval: `60` seconds
   - Email enabled: `yes` (same SMTP settings)
   - SMS enabled: `no`
   - Desktop notifications: `yes`

3. Copy config content (not credentials) to Linux:
   ```bash
   # On Windows, view config:
   cat $env:APPDATA\UnifiWatch\config.json
   
   # On Linux, create identical config:
   mkdir -p ~/.config/UnifiWatch
   nano ~/.config/UnifiWatch/config.json
   # Paste identical JSON content
   ```

4. Copy config content to macOS:
   ```bash
   # On macOS:
   mkdir -p ~/Library/Application\ Support/UnifiWatch
   nano ~/Library/Application\ Support/UnifiWatch/config.json
   # Paste identical JSON content
   ```

5. Configure credentials separately on each platform:
   ```bash
   # Windows:
   .\UnifiWatch.exe --configure
   
   # Linux:
   ./UnifiWatch --configure
   
   # macOS:
   ./UnifiWatch --configure
   ```

6. Verify configuration identical on all platforms:
   ```bash
   # Windows:
   .\UnifiWatch.exe --show-config
   
   # Linux:
   ./UnifiWatch --show-config
   
   # macOS:
   ./UnifiWatch --show-config
   ```

### Expected Results

- [ ] Configuration file structure identical across platforms
- [ ] Product monitoring settings match exactly
- [ ] Notification settings match exactly
- [ ] Credentials stored securely on each platform (different storage methods)
- [ ] `--show-config` output identical (except platform-specific paths)

### Actual Results

```
Windows config matches: [ ] Yes  [ ] No
Linux config matches: [ ] Yes  [ ] No
macOS config matches: [ ] Yes  [ ] No

Differences found:


```

---

## Test 2: Simultaneous Service Execution

### Objective
Verify all three platforms can run the service simultaneously without conflicts.

### Steps

1. Start service on Windows:
   ```powershell
   Start-Service -Name "UnifiWatch"
   ```

2. Start service on Linux:
   ```bash
   sudo systemctl start unifiwatch
   ```

3. Start service on macOS:
   ```bash
   launchctl load ~/Library/LaunchAgents/com.unifiwatch.plist
   ```

4. Wait for at least 3 check cycles (3 minutes with 60-second interval)

5. Verify all services running:
   ```bash
   # Windows:
   Get-Service -Name "UnifiWatch"
   
   # Linux:
   sudo systemctl status unifiwatch
   
   # macOS:
   launchctl list | grep com.unifiwatch
   ```

6. Check logs on all platforms for errors

7. Verify email notifications (should receive 3 emails per check if product in stock)

### Expected Results

- [ ] All three services start successfully
- [ ] All three services remain running for 3+ cycles
- [ ] No conflicts or API rate limiting errors
- [ ] Each platform sends independent notifications
- [ ] Identical stock data reported on all platforms

### Actual Results

```
Windows service running: [ ] Yes  [ ] No
Linux service running: [ ] Yes  [ ] No
macOS service running: [ ] Yes  [ ] No

Emails received per check: _______

Stock data identical: [ ] Yes  [ ] No

Differences:


```

---

## Test 3: API Response Consistency

### Objective
Verify all platforms receive identical API responses from Ubiquiti.

### Steps

1. Enable detailed logging on all platforms (if available)

2. Trigger simultaneous stock checks:
   - Windows: Restart service
   - Linux: Restart service
   - macOS: Restart service

3. Wait for next check cycle to complete

4. Extract stock check results from logs:
   ```bash
   # Windows:
   Get-Content "$env:APPDATA\UnifiWatch\logs\*.log" -Tail 20
   
   # Linux:
   sudo journalctl -u unifiwatch -n 20
   
   # macOS:
   tail -n 20 ~/Library/Logs/UnifiWatch/service.log
   ```

5. Compare stock data:
   - Product availability (in stock / out of stock)
   - Product price
   - Product variants
   - API response timestamps

### Expected Results

- [ ] Stock availability identical across platforms
- [ ] Product prices identical
- [ ] Product variants identical
- [ ] API response times similar (< 5 second difference)
- [ ] No platform reports errors while others succeed

### Actual Results

```
Stock availability:
- Windows: ___________
- Linux: _____________
- macOS: _____________

Product price:
- Windows: $ _________
- Linux: $ ___________
- macOS: $ ___________

API response times:
- Windows: _______ ms
- Linux: _________ ms
- macOS: _________ ms

Data consistency: [ ] Pass  [ ] Fail
```

---

## Test 4: Notification Delivery Consistency

### Objective
Verify notification delivery behavior is identical across platforms.

### Steps

1. Configure identical notification settings on all platforms:
   - Email: enabled
   - Desktop: enabled
   - SMS: disabled

2. Monitor for stock availability change (or use test notification)

3. When notification triggered, record:
   - Time received on each platform
   - Email subject line
   - Email body content
   - Desktop notification text
   - Any errors

4. Compare notifications:
   ```
   Platform | Email Time | Desktop Notification | Errors
   ---------|------------|---------------------|-------
   Windows  | __________ | __________________ | ______
   Linux    | __________ | __________________ | ______
   macOS    | __________ | __________________ | ______
   ```

### Expected Results

- [ ] Email notifications identical in content
- [ ] Email arrival times within 1 minute of each other
- [ ] Desktop notification text identical
- [ ] All platforms send notifications (no platform fails)
- [ ] No duplicate notifications sent

### Actual Results

```
Email content identical: [ ] Yes  [ ] No

Email timing variance: __________ seconds

Desktop notifications identical: [ ] Yes  [ ] No

All platforms delivered: [ ] Yes  [ ] No

Differences:


```

---

## Test 5: Configuration Reload Behavior

### Objective
Verify config reload works identically on all platforms.

### Steps

1. Start services on all platforms

2. Simultaneously update config on all platforms:
   - Change check interval from `60` to `120` seconds

3. Save changes:
   ```bash
   # Windows:
   notepad $env:APPDATA\UnifiWatch\config.json
   
   # Linux:
   nano ~/.config/UnifiWatch/config.json
   
   # macOS:
   nano ~/Library/Application\ Support/UnifiWatch/config.json
   ```

4. Wait 2-3 minutes

5. Monitor logs for config reload detection:
   ```bash
   # Windows:
   Get-Content "$env:APPDATA\UnifiWatch\logs\*.log" -Tail 10
   
   # Linux:
   sudo journalctl -u unifiwatch -n 10
   
   # macOS:
   tail -n 10 ~/Library/Logs/UnifiWatch/service.log
   ```

6. Verify new interval takes effect on all platforms

### Expected Results

- [ ] All platforms detect config change
- [ ] Reload happens without service restart
- [ ] New interval takes effect within 1-2 minutes
- [ ] All platforms log config reload event
- [ ] Behavior identical across platforms

### Actual Results

```
Config reload detected:
- Windows: [ ] Yes  [ ] No
- Linux: [ ] Yes  [ ] No
- macOS: [ ] Yes  [ ] No

Reload timing:
- Windows: _______ seconds
- Linux: _________ seconds
- macOS: _________ seconds

New interval active:
- Windows: [ ] Yes  [ ] No
- Linux: [ ] Yes  [ ] No
- macOS: [ ] Yes  [ ] No
```

---

## Test 6: Error Handling Consistency

### Objective
Verify error conditions handled identically on all platforms.

### Steps

1. Test Case: **Invalid SMTP credentials**
   - Update config with incorrect email password on all platforms
   - Restart services
   - Wait for email notification attempt
   - Record error messages

2. Test Case: **No internet connection**
   - Disable network on all platforms
   - Wait for next check cycle
   - Record error messages
   - Re-enable network
   - Verify recovery

3. Test Case: **Corrupted config file**
   - Corrupt config JSON on all platforms
   - Restart services
   - Record error messages

4. Compare error messages across platforms

### Expected Results

- [ ] Error messages identical in content
- [ ] Error severity levels consistent
- [ ] Services handle errors gracefully (no crashes)
- [ ] Recovery behavior identical
- [ ] Error logging format consistent

### Actual Results

```
Invalid credentials error:
- Windows: ________________________________________
- Linux: __________________________________________
- macOS: __________________________________________

No internet error:
- Windows: ________________________________________
- Linux: __________________________________________
- macOS: __________________________________________

Corrupted config error:
- Windows: ________________________________________
- Linux: __________________________________________
- macOS: __________________________________________

Error consistency: [ ] Pass  [ ] Fail
```

---

## Test 7: Performance Comparison

### Objective
Compare resource usage across platforms to ensure no platform has performance issues.

### Steps

1. Run services on all platforms for 30 minutes

2. Measure CPU usage:
   ```bash
   # Windows:
   Get-Counter "\Process(UnifiWatch)\% Processor Time" -SampleInterval 5 -MaxSamples 10
   
   # Linux:
   top -b -n 10 -d 5 -p $(pgrep UnifiWatch) | grep UnifiWatch
   
   # macOS:
   top -l 10 -s 5 -pid $(pgrep UnifiWatch) | grep UnifiWatch
   ```

3. Measure memory usage:
   ```bash
   # Windows:
   Get-Process -Name "UnifiWatch" | Select-Object WorkingSet
   
   # Linux:
   ps -p $(pgrep UnifiWatch) -o rss=
   
   # macOS:
   ps -p $(pgrep UnifiWatch) -o rss=
   ```

4. Monitor for memory leaks (measure every 5 minutes for 30 minutes)

5. Compare results

### Expected Results

- [ ] CPU usage similar across platforms (< 10% difference)
- [ ] Memory usage similar across platforms (< 50 MB difference)
- [ ] No memory leaks on any platform
- [ ] Performance acceptable on all platforms (< 5% CPU average)
- [ ] No platform significantly outperforms or underperforms others

### Actual Results

```
Average CPU Usage:
- Windows: _______ %
- Linux: _________ %
- macOS: _________ %

Average Memory Usage (RSS):
- Windows: _______ MB
- Linux: _________ MB
- macOS: _________ MB

Memory Trend (over 30 min):
- Windows: [ ] Stable  [ ] Increasing  [ ] Decreasing
- Linux: [ ] Stable  [ ] Increasing  [ ] Decreasing
- macOS: [ ] Stable  [ ] Increasing  [ ] Decreasing

Performance consistency: [ ] Pass  [ ] Fail

Outliers:


```

---

## Test 8: Credential Storage Security

### Objective
Verify credentials stored securely on all platforms using platform-appropriate methods.

### Steps

1. Store identical SMTP credentials on all platforms

2. Verify credential storage methods:
   ```bash
   # Windows:
   # Check Windows Credential Manager
   control /name Microsoft.CredentialManager
   
   # Linux:
   # Check secret-service keyring OR encrypted files
   ls -la ~/.config/UnifiWatch/credentials/
   
   # macOS:
   # Check Keychain
   open -a "Keychain Access"
   ```

3. Verify file permissions (where applicable):
   ```bash
   # Windows (config file):
   icacls "$env:APPDATA\UnifiWatch\config.json"
   
   # Linux:
   stat -c "%a %n" ~/.config/UnifiWatch/config.json
   stat -c "%a %n" ~/.config/UnifiWatch/credentials/* 2>/dev/null
   
   # macOS:
   stat -f "%Sp %N" ~/Library/Application\ Support/UnifiWatch/config.json
   ```

4. Attempt to read credentials as different user (should fail)

5. Verify credentials not in config file (only platform-secure storage)

### Expected Results

- [ ] Windows: Credentials in Credential Manager (DPAPI encrypted)
- [ ] Linux: Credentials in secret-service OR encrypted files (PBKDF2)
- [ ] macOS: Credentials in Keychain
- [ ] No plaintext credentials in config files
- [ ] File permissions restrictive (600 for files, 700 for directories)
- [ ] Other users cannot access credentials

### Actual Results

```
Credential storage:
- Windows: [ ] Credential Manager  [ ] File (FAIL)
- Linux: [ ] secret-service  [ ] encrypted-file  [ ] plaintext (FAIL)
- macOS: [ ] Keychain  [ ] File (FAIL)

File permissions:
- Windows config: ______________________
- Linux config: ________________________
- Linux credentials: ___________________
- macOS config: ________________________

Credentials in config files: [ ] Yes (FAIL)  [ ] No (PASS)

Security consistent: [ ] Pass  [ ] Fail
```

---

## Test 9: Service Lifecycle Consistency

### Objective
Verify service installation, start, stop, restart, and uninstall work identically.

### Steps

1. Uninstall services on all platforms

2. Reinstall on all platforms:
   ```bash
   # Windows:
   .\UnifiWatch.exe --install-service
   
   # Linux:
   sudo ./UnifiWatch --install-service
   
   # macOS:
   ./UnifiWatch --install-service
   ```

3. Start services

4. Stop services

5. Restart services

6. Uninstall services

7. Record timing and behavior for each step on each platform

### Expected Results

- [ ] Install completes successfully on all platforms
- [ ] Start/stop/restart behavior consistent
- [ ] Service status commands work on all platforms
- [ ] Uninstall cleanup identical (preserves config, removes service)
- [ ] No platform has errors while others succeed

### Actual Results

```
Installation:
- Windows: [ ] Success  [ ] Fail  (Time: _____ sec)
- Linux: [ ] Success  [ ] Fail  (Time: _____ sec)
- macOS: [ ] Success  [ ] Fail  (Time: _____ sec)

Start/Stop/Restart:
- Windows: [ ] Consistent  [ ] Inconsistent
- Linux: [ ] Consistent  [ ] Inconsistent
- macOS: [ ] Consistent  [ ] Inconsistent

Uninstall cleanup:
- Windows: [ ] Config preserved  [ ] Config deleted
- Linux: [ ] Config preserved  [ ] Config deleted
- macOS: [ ] Config preserved  [ ] Config deleted

Lifecycle consistency: [ ] Pass  [ ] Fail
```

---

## Test 10: Documentation Accuracy

### Objective
Verify platform-specific documentation (SERVICE_SETUP.md, deployment test guides) is accurate.

### Steps

1. Follow Windows deployment test guide (WINDOWS_END_USER_GUIDE.md)
   - Record any errors or inaccuracies

2. Follow Linux deployment test guide (LINUX_END_USER_GUIDE.md)
   - Record any errors or inaccuracies

3. Follow macOS deployment test guide (MACOS_END_USER_GUIDE.md)
   - Record any errors or inaccuracies

4. Verify SERVICE_SETUP.md instructions work on all platforms

5. Verify SECURITY.md recommendations apply to all platforms

### Expected Results

- [ ] All documented commands work as written
- [ ] File paths are accurate
- [ ] Screenshots/examples match actual behavior (if included)
- [ ] No missing steps or unclear instructions
- [ ] Platform-specific sections accurate

### Actual Results

```
Windows documentation:
- Accuracy: [ ] Excellent  [ ] Good  [ ] Needs improvement
- Issues found: ________________________________

Linux documentation:
- Accuracy: [ ] Excellent  [ ] Good  [ ] Needs improvement
- Issues found: ________________________________

macOS documentation:
- Accuracy: [ ] Excellent  [ ] Good  [ ] Needs improvement
- Issues found: ________________________________

Documentation consistency: [ ] Pass  [ ] Fail
```

---

## Compatibility Matrix

### Platform Compatibility Summary

| Feature | Windows | Linux | macOS | Notes |
|---------|---------|-------|-------|-------|
| Service Installation | ☐ Pass ☐ Fail | ☐ Pass ☐ Fail | ☐ Pass ☐ Fail | |
| Configuration Wizard | ☐ Pass ☐ Fail | ☐ Pass ☐ Fail | ☐ Pass ☐ Fail | |
| Credential Storage | ☐ Pass ☐ Fail | ☐ Pass ☐ Fail | ☐ Pass ☐ Fail | |
| Email Notifications | ☐ Pass ☐ Fail | ☐ Pass ☐ Fail | ☐ Pass ☐ Fail | |
| Desktop Notifications | ☐ Pass ☐ Fail | ☐ Pass ☐ Fail ☐ N/A | ☐ Pass ☐ Fail | |
| Configuration Reload | ☐ Pass ☐ Fail | ☐ Pass ☐ Fail | ☐ Pass ☐ Fail | |
| Service Auto-Restart | ☐ Pass ☐ Fail | ☐ Pass ☐ Fail | ☐ Pass ☐ Fail | |
| Logging | ☐ Pass ☐ Fail | ☐ Pass ☐ Fail | ☐ Pass ☐ Fail | |
| Error Handling | ☐ Pass ☐ Fail | ☐ Pass ☐ Fail | ☐ Pass ☐ Fail | |
| Performance | ☐ Pass ☐ Fail | ☐ Pass ☐ Fail | ☐ Pass ☐ Fail | |
| File Permissions | ☐ Pass ☐ Fail | ☐ Pass ☐ Fail | ☐ Pass ☐ Fail | |
| Uninstall/Cleanup | ☐ Pass ☐ Fail | ☐ Pass ☐ Fail | ☐ Pass ☐ Fail | |

**Overall Compatibility Score**: _____ / 36 tests passed

---

## Critical Cross-Platform Issues

1. _________________________________________________________________

2. _________________________________________________________________

3. _________________________________________________________________

---

## Platform-Specific Issues

### Windows

1. _________________________________________________________________

2. _________________________________________________________________

### Linux

1. _________________________________________________________________

2. _________________________________________________________________

### macOS

1. _________________________________________________________________

2. _________________________________________________________________

---

## Recommendations

### Code Changes

1. _________________________________________________________________

2. _________________________________________________________________

### Documentation Updates

1. _________________________________________________________________

2. _________________________________________________________________

### Future Testing

1. _________________________________________________________________

2. _________________________________________________________________

---

## Sign-Off

### Test Summary

**Total Tests**: 10  
**Tests Passed**: _____ / 10  
**Tests Failed**: _____ / 10

**Platforms Tested**:
- [ ] Windows 10/11
- [ ] Linux (Ubuntu/Fedora/Debian)
- [ ] macOS (Big Sur or later)

**Cross-Platform Consistency**: [ ] Excellent  [ ] Good  [ ] Needs Improvement  [ ] Poor

**Ready for Production**: [ ] Yes  [ ] No  [ ] With Fixes

---

**Lead Tester**: ____________________  
**Test Date**: ____________________  
**Overall Status**: ☐ Approved for Release  ☐ Requires Platform-Specific Fixes  ☐ Requires Cross-Platform Fixes


