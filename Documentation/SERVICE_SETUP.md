# UnifiWatch Service Mode - Setup Guide

## Overview

UnifiWatch can run as a background service on Windows, macOS, and Linux. This guide provides platform-specific instructions for installing and configuring the service on each platform.

## Prerequisites for All Platforms

1. **UnifiWatch installed** - Download or build from source
2. **Configuration completed** - Run `UnifiWatch --configure` to set up:
   - Store selection (USA, Europe, UK, Brazil, India, Japan, Taiwan, Singapore, Mexico, China)
   - Products to monitor
   - Check interval (minimum 30 seconds)
   - Notification preferences (email, SMS, or both)
3. **Network access** - Service needs internet access to Ubiquiti API
4. **Permissions** - Sufficient permissions to install system service

---

## Windows Service Setup

### Prerequisites

- Windows 10, Windows 11, Windows Server 2019+
- Administrator command prompt (right-click cmd.exe → "Run as administrator")
- Windows Credential Manager enabled (default on most Windows installations)

### Installation Steps

1. **Navigate to UnifiWatch directory**:

   ```cmd
   cd C:\Path\To\UnifiWatch
   ```

2. **Configure the application** (if not already done):

   ```cmd
   UnifiWatch.exe --configure
   ```

   This launches the interactive configuration wizard. Follow prompts to:
   - Select your Ubiquiti store
   - Choose products to monitor
   - Set check interval
   - Configure notifications

3. **Install the service**:

   ```cmd
   UnifiWatch.exe --install-service
   ```

   Expected output:

   ```

   ✓ Service 'UnifiWatch' installed successfully
   Service will start automatically on next boot
   ```

4. **Verify installation**:

   ```cmd
   UnifiWatch.exe --service-status
   ```

   Or check in Services.msc:
   - Press `Win + R`, type `services.msc`, press Enter
   - Find "UnifiWatch" in the list
   - Status should show "Running"
   - Startup type should be "Automatic"

5. **View credentials in Credential Manager**:
   - Press `Win + R`, type `credential manager`, press Enter
   - Click "Windows Credentials"
   - Find entries starting with "UnifiWatch:" (e.g., "UnifiWatch:email-smtp")
   - Passwords are stored securely via Windows Credential Manager (DPAPI)

### Daily Operations

**Start/Stop service**:

```cmd
UnifiWatch.exe --start-service      # Start the service
UnifiWatch.exe --stop-service       # Stop the service
```

**Check service status**:

```cmd
UnifiWatch.exe --service-status
```

**View logs**:

Windows Event Viewer:

- Press `Win + R`, type `eventvwr.msc`, press Enter
- Navigate to: Applications and Services Logs → UnifiWatch
- All service logs appear here

**Update configuration**:

```cmd
UnifiWatch.exe --configure
```

Service will detect config changes and reload automatically.

**Uninstall service**:

```cmd
UnifiWatch.exe --uninstall-service
```

---

## Linux Service Setup

### Prerequisites

- Ubuntu 22.04+ / Debian 12+ / Fedora 37+ / Rocky Linux 9+
- Root or sudo access
- systemd (standard on modern Linux distributions)
- Optional: secret-service daemon (GNOME Keyring or KDE Wallet for credential storage)

### Installation Steps

1. **Download or build UnifiWatch**:

   ```bash
   # Option A: Download pre-built Linux binary
   wget https://github.com/johnmccrae/UnifiWatch/releases/download/v1.0.0/UnifiWatch-linux-x64
   chmod +x UnifiWatch-linux-x64
   
   # Option B: Build from source
   git clone https://github.com/johnmccrae/UnifiWatch.git
   cd UnifiWatch
   dotnet publish -r linux-x64 -c Release
   # Binary: bin/Release/net9.0/linux-x64/UnifiWatch
   ```

2. **Configure the application**:

   ```bash
   sudo ./UnifiWatch --configure
   ```

   Follow interactive prompts (same as Windows setup).

3. **Install the service**:

   ```bash
   sudo ./UnifiWatch --install-service
   ```

   Expected output:

   ```bash
   ✓ Service 'unifiwatch' installed successfully
   systemd unit file created at: /etc/systemd/system/unifiwatch.service
   ```

4. **Enable and start the service**:

   ```bash
   sudo systemctl enable unifiwatch
   sudo systemctl start unifiwatch
   ```

5. **Verify installation**:

   ```bash
   sudo systemctl status unifiwatch
   ```

   Expected output:

   ```bash
   ● unifiwatch.service - UnifiWatch Stock Monitor
        Loaded: loaded (/etc/systemd/system/unifiwatch.service; enabled; vendor preset: enabled)
        Active: active (running) since [timestamp]
   ```

6. **Verify credentials storage**:
   Credentials are stored encrypted in: `~/.config/UnifiWatch/credentials/`

   ```bash
   ls -la ~/.config/UnifiWatch/credentials/
   # Files should be readable only by you (permissions 600)
   ```

### Daily Operations

**Start/Stop service**:

```bash
sudo systemctl start unifiwatch       # Start the service
sudo systemctl stop unifiwatch        # Stop the service
sudo systemctl restart unifiwatch     # Restart the service
```

**Check service status**:

```bash
sudo systemctl status unifiwatch
```

**View logs in real-time**:

```bash
sudo journalctl -u unifiwatch -f
```

**View last 50 log lines**:

```bash
sudo journalctl -u unifiwatch -n 50
```

**Update configuration**:

```bash
sudo ./UnifiWatch --configure
```

Service will detect and reload automatically.

**Disable and uninstall service**:

```bash
sudo systemctl disable unifiwatch
sudo systemctl stop unifiwatch
sudo ./UnifiWatch --uninstall-service
```

### Secret Service (Optional - Credential Storage)

For desktop Linux environments with GNOME Keyring or KDE Wallet:

1. **Install secret-service daemon** (if not installed):

   ```bash
   # Ubuntu/Debian
   sudo apt-get install gnome-keyring
   
   # Fedora
   sudo dnf install gnome-keyring
   ```

2. **Configure credential storage in wizard**:
   When running `sudo ./UnifiWatch --configure`, the app will attempt to use secret-service if available.

   Note: If secret-service fails, the app falls back to encrypted file storage (secure PBKDF2 encryption).

---

## macOS Service Setup

### Prerequisites

- macOS 11.0+ (Big Sur or later)
- Full disk access for the application (may prompt on first run)
- Keychain access (automatic)

### Installation Steps

1. **Build UnifiWatch for macOS**:

   ```bash
   # Clone and build
   git clone https://github.com/johnmccrae/UnifiWatch.git
   cd UnifiWatch
   dotnet publish -r osx-x64 -c Release
   # Binary: bin/Release/net9.0/osx-x64/UnifiWatch
   ```

2. **Configure the application**:

   ```bash
   ./UnifiWatch --configure
   ```

   Follow interactive prompts (same as Windows setup).

3. **Install the service**:

   ```bash
   ./UnifiWatch --install-service
   ```

   Expected output:

   ```
   ✓ Service 'com.unifiwatch' installed successfully
   launchd plist created at: ~/Library/LaunchAgents/com.unifiwatch.plist
   ```

4. **Load the service** (automatic after install, but can manually trigger):

   ```bash
   launchctl load ~/Library/LaunchAgents/com.unifiwatch.plist
   ```

5. **Verify installation**:

   ```bash
   launchctl list | grep unifiwatch
   ```

   Expected output: Service PID and label shown if running.

6. **Verify Keychain credential storage**:

   ```bash
   # Open Keychain Access app
   open /Applications/Utilities/Keychain\ Access.app
   ```

   Look for entries: "UnifiWatch" in Keychain. Passwords are stored securely via macOS Keychain.

### Daily Operations

**Start/Stop service**:

```bash
launchctl load ~/Library/LaunchAgents/com.unifiwatch.plist      # Start
launchctl unload ~/Library/LaunchAgents/com.unifiwatch.plist    # Stop
```

Or use convenience commands:

```bash
./UnifiWatch --start-service
./UnifiWatch --stop-service
```

**Check service status**:

```bash
launchctl list | grep unifiwatch
./UnifiWatch --service-status
```

**View logs**:

```bash
# Real-time logs
tail -f ~/Library/Logs/UnifiWatch/service.log

# Or use Console app
open /Applications/Utilities/Console.app
# Search for "UnifiWatch"
```

**Update configuration**:

```bash
./UnifiWatch --configure
```

Service will detect and reload automatically.

**Disable and uninstall service**:

```bash
launchctl unload ~/Library/LaunchAgents/com.unifiwatch.plist
./UnifiWatch --uninstall-service
```

---

## Testing the Service

### Verify Service is Running

**All Platforms**:

```bash
UnifiWatch --service-status
```

Expected output should show:

- Service name
- Status: Running
- Startup type: Automatic
- Process ID
- Last start time

### Test Notifications

Ensure notifications are configured and working:

```bash
UnifiWatch --test-notifications
```

This sends test notifications to all enabled channels:

- **Desktop**: Toast notification appears
- **Email**: Test email sent to configured recipients
- **SMS**: Test SMS sent to configured phone numbers

### Check Logs

**Windows**:

```cmd
# Event Viewer logs
eventvwr.msc
# Navigate to: Applications and Services Logs → UnifiWatch
```

**Linux**:

```bash
sudo journalctl -u unifiwatch -f
```

**macOS**:

```bash
tail -f ~/Library/Logs/UnifiWatch/service.log
```

---

## Troubleshooting

### Service Won't Start

**Windows**:

1. Check Event Viewer: Applications and Services Logs → UnifiWatch
2. Verify admin permissions
3. Try reinstalling: `UnifiWatch.exe --uninstall-service` then `--install-service`

**Linux**:

```bash
# Check systemd errors
sudo journalctl -u unifiwatch -n 50
# Check permissions
ls -la /etc/systemd/system/unifiwatch.service
```

**macOS**:

```bash
# Check plist syntax
plutil -lint ~/Library/LaunchAgents/com.unifiwatch.plist
# Check logs
tail -f ~/Library/Logs/UnifiWatch/service.log
```

### Notifications Not Sending

1. Test manually:

   ```bash
   UnifiWatch --test-notifications
   ```

2. Check email configuration:

   ```bash
   UnifiWatch --show-config
   # Verify SMTP settings are correct
   ```

3. For SMS (Twilio):
   - Verify account SID and auth token
   - Check phone numbers are in E.164 format (e.g., +1-555-0123)
   - Verify Twilio account has funds/credits

4. Check service logs (see "Check Logs" section above)

### Credential Storage Issues

**Windows**:

- Verify Windows Credential Manager is working:
  - `credential manager` → Windows Credentials
  - Look for "UnifiWatch:" entries

**Linux**:

- Check encrypted file permissions:

  ```bash
  ls -la ~/.config/UnifiWatch/credentials/
  # Should show 600 (rwx------)
  ```

**macOS**:

- Verify Keychain entries:

  ```bash
  open /Applications/Utilities/Keychain\ Access.app
  # Search for "UnifiWatch"
  ```

### High CPU or Memory Usage

1. Check current check interval:

   ```bash
   UnifiWatch --show-config
   # Look for "CheckInterval"
   ```

2. Increase check interval (minimum 30 seconds, default 60):

   ```bash
   UnifiWatch --configure
   # When prompted, increase interval to 300+ seconds
   ```

3. Monitor resource usage:
   - Windows: Task Manager → Performance tab
   - Linux: `top` or `htop`
   - macOS: Activity Monitor

---

## Uninstalling the Service

### Windows

```cmd
UnifiWatch.exe --uninstall-service
```

Manually remove credentials:

- Open Credential Manager
- Delete all "UnifiWatch:" entries

### Linux

```bash
sudo systemctl stop unifiwatch
sudo systemctl disable unifiwatch
sudo ./UnifiWatch --uninstall-service
rm -rf ~/.config/UnifiWatch
```

### macOS

```bash
./UnifiWatch --uninstall-service
rm -rf ~/Library/LaunchAgents/com.unifiwatch.plist
rm -rf ~/Library/Logs/UnifiWatch
```

Also remove from Keychain (optional):

- Open Keychain Access app
- Delete "UnifiWatch" entries

---

## Advanced Configuration

### Custom Log Location

Edit the service configuration file:

**Windows** (Services.msc):

- Right-click "UnifiWatch" → Properties
- Modify "Executable path" if needed

**Linux** (`/etc/systemd/system/unifiwatch.service`):

```ini
[Service]
StandardOutput=journal
StandardError=journal
```

**macOS** (`~/Library/LaunchAgents/com.unifiwatch.plist`):

```xml
<key>StandardOutPath</key>
<string>/var/log/unifiwatch.log</string>
```

### Running Multiple Instances

To run multiple UnifiWatch instances with different configurations:

**Windows**:

- Install with different service names:

  ```powershell
  UnifiWatch.exe --install-service --service-name "UnifiWatch-Store1"
  ```

**Linux**:

- Create separate systemd units (advanced)

**macOS**:

- Create multiple LaunchAgent plists with different labels

---

## Support & Issues

For issues or questions:
1. Check logs (see "Check Logs" section)
2. Review troubleshooting section above
3. Open GitHub Issue: https://github.com/johnmccrae/UnifiWatch/issues
4. Include:
   - Operating system and version
   - UnifiWatch version (`UnifiWatch --version`)
   - Relevant log excerpts
   - Steps to reproduce


