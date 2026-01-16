# Email and SMS Notification Setup Guide

This guide explains how to configure email (SMTP) and SMS (Twilio) notifications for UnifiWatch. Both notification channels are **optional** - you can enable one, both, or neither.

---

## 📧 Email Notifications (SMTP)

Email notifications allow UnifiWatch to send product availability alerts to one or more email addresses using any SMTP server (Gmail, Outlook, custom SMTP, etc.).

### What You Need

1. **SMTP Server Details**:
   - SMTP server hostname (e.g., `smtp.gmail.com`)
   - SMTP port (usually `587` for TLS or `465` for SSL)
   - Whether to use SSL/TLS (recommended: `true`)

2. **Email Credentials**:
   - Your email address (from address)
   - SMTP password or app-specific password
   - Recipient email address(es)

### Step 1: Store Your SMTP Password Securely

UnifiWatch uses OS-native credential storage for security. Use the credential provider to store your SMTP password:

**Windows (PowerShell)**:
```powershell
# Store SMTP password in Windows Credential Manager
# Read password securely (will not echo to screen)
$password = Read-Host "Enter your SMTP password" -AsSecureString

# Store the credential in Windows Credential Manager
New-StoredCredential -Target "smtp:password" `
    -UserName "your-email@example.com" `
    -SecurePassword $password

Write-Host "SMTP password stored successfully in Windows Credential Manager"

# Verify it was stored
Get-StoredCredential -Target "smtp:password"
```

**macOS**:
```bash
# Store in Keychain
security add-generic-password -a "UnifiWatch" -s "smtp:password" -w "your-smtp-password"
```

**Linux**:
```bash
# Uses encrypted file storage (automatic)
# Credentials are encrypted using PBKDF2 with machine-specific key
# Create the credential storage directory and store your SMTP password

mkdir -p ~/.local/share/unifiwatch/credentials

# Encrypt and store the SMTP password
# You will be prompted for the password (will not echo to screen)
read -s SMTP_PASSWORD
export UNIFIWATCH_SMTP_PASSWORD=$SMTP_PASSWORD
unifiwatch --store-credential smtp:password $SMTP_PASSWORD

# Or use environment variable approach:
export UNIFIWATCH_SMTP_PASSWORD="your-smtp-password"
unifiwatch --store-credential smtp:password "$UNIFIWATCH_SMTP_PASSWORD"
```

### Step 2: Configure Email Settings

**Option A: Via config.json** (recommended):

Edit `%APPDATA%\UnifiWatch\config.json` (Windows) or `~/.config/unifiwatch/config.json` (macOS/Linux):

```json
{
  "Service": {
    "ServiceName": "UnifiWatch",
    "CheckIntervalSeconds": 60
  },
  "EmailNotifications": {
    "Enabled": true,
    "SmtpServer": "smtp.gmail.com",
    "SmtpPort": 587,
    "UseSsl": true,
    "FromAddress": "your-email@gmail.com",
    "FromName": "UnifiWatch Alert",
    "ToAddresses": [
      "recipient1@example.com",
      "recipient2@example.com"
    ],
    "CredentialKey": "smtp:password"
  }
}
```

**Option B: Environment Variables** (for CI/CD or automation):

```bash
# Windows (PowerShell)
$env:UNIFIWATCH_EMAIL_ENABLED = "true"
$env:UNIFIWATCH_SMTP_SERVER = "smtp.gmail.com"
$env:UNIFIWATCH_SMTP_PORT = "587"
$env:UNIFIWATCH_SMTP_SSL = "true"
$env:UNIFIWATCH_SMTP_FROM = "your-email@gmail.com"
$env:UNIFIWATCH_SMTP_TO = "recipient@example.com"
$env:UNIFIWATCH_SMTP_PASSWORD = "your-app-password"

# Linux/macOS
export UNIFIWATCH_EMAIL_ENABLED=true
export UNIFIWATCH_SMTP_SERVER=smtp.gmail.com
export UNIFIWATCH_SMTP_PORT=587
export UNIFIWATCH_SMTP_SSL=true
export UNIFIWATCH_SMTP_FROM=your-email@gmail.com
export UNIFIWATCH_SMTP_TO=recipient@example.com
export UNIFIWATCH_SMTP_PASSWORD=your-app-password
```

### Gmail-Specific Setup

If using Gmail, you **must** use an App Password (not your regular Google password):

1. Go to [Google Account Security](https://myaccount.google.com/security)
2. Enable 2-Step Verification (required for app passwords)
3. Go to [App Passwords](https://myaccount.google.com/apppasswords)
4. Generate a new app password for "Mail"
5. Use this 16-character password (no spaces) in your credential storage

**Gmail Settings**:
- SMTP Server: `smtp.gmail.com`
- Port: `587`
- SSL/TLS: `true`

### Outlook/Office 365 Setup

**Outlook.com Settings**:
- SMTP Server: `smtp-mail.outlook.com`
- Port: `587`
- SSL/TLS: `true`

**Office 365 Settings**:
- SMTP Server: `smtp.office365.com`
- Port: `587`
- SSL/TLS: `true`

### Testing Email Configuration

Once configured, test your email setup with:

```bash
# Test email notifications (command coming in Phase 6)
unifiwatch --test-notifications --email-only
```

---

## 📱 SMS Notifications (Twilio)

SMS notifications send text message alerts to mobile phones using Twilio's SMS API.

### What You Need

1. **Twilio Account** (free trial available):
   - Create account at [https://www.twilio.com/try-twilio](https://www.twilio.com/try-twilio)
   - Free trial includes $15.50 credit (~500 SMS in the US)

2. **Twilio Credentials**:
   - Account SID (starts with `AC...`)
   - Auth Token (found in Twilio Console)
   - Twilio Phone Number (from your Twilio dashboard)

3. **Recipient Phone Number**:
   - Must be in E.164 format: `+1234567890` (includes country code)
   - For US: `+1` + 10-digit number
   - For Canada: `+1` + 10-digit number
   - For Germany: `+49` + number
   - For Spain: `+34` + number
   - For France: `+33` + number

### Step 1: Set Up Twilio Account

1. **Sign up**: Go to [Twilio](https://www.twilio.com/try-twilio)
2. **Verify your phone number**: Required for trial accounts
3. **Get a Twilio number**:
   - Navigate to Phone Numbers → Manage → Buy a number
   - Choose a number with SMS capability
   - Note: Free trial numbers can only send to verified numbers

4. **Find your credentials**:
   - Go to Twilio Console Dashboard
   - Copy your **Account SID** (e.g., `AC1234567890abcdef1234567890abcdef`)
   - Copy your **Auth Token** (click "Show" to reveal)
   - Copy your **Twilio Phone Number** (e.g., `+15551234567`)

### Step 2: Store Twilio Auth Token Securely

**Windows (PowerShell)**:
```powershell
# Store Twilio auth token in Windows Credential Manager
# Read securely without echoing to screen
$token = Read-Host "Enter your Twilio Auth Token" -AsSecureString

# Store the credential in Windows Credential Manager
New-StoredCredential -Target "sms:twilio:auth-token" `
    -UserName "UnifiWatch" `
    -SecurePassword $token

Write-Host "Twilio auth token stored successfully in Windows Credential Manager"

# Verify it was stored
Get-StoredCredential -Target "sms:twilio:auth-token"
```

**macOS**:
```bash
# Store in Keychain
security add-generic-password -a "UnifiWatch" -s "sms:twilio:auth-token" -w "your-twilio-auth-token"
```

**Linux**:
```bash
# Uses encrypted file storage (automatic)
# Create the credential storage directory and store your Twilio auth token

mkdir -p ~/.local/share/unifiwatch/credentials

# Store Twilio auth token securely (prompts for input, no echo)
# Option 1: Using read command (no echo to screen)
read -s TWILIO_TOKEN
export UNIFIWATCH_TWILIO_TOKEN=$TWILIO_TOKEN
unifiwatch --store-credential sms:twilio:auth-token $TWILIO_TOKEN
unset TWILIO_TOKEN

# Option 2: From environment variable
export UNIFIWATCH_TWILIO_TOKEN="your-auth-token"
unifiwatch --store-credential sms:twilio:auth-token "$UNIFIWATCH_TWILIO_TOKEN"
```

### Step 3: Configure SMS Settings

**Option A: Via config.json** (recommended):

Edit `%APPDATA%\UnifiWatch\config.json` (Windows) or `~/.config/unifiwatch/config.json` (macOS/Linux):

```json
{
  "Service": {
    "ServiceName": "UnifiWatch",
    "CheckIntervalSeconds": 60
  },
  "SmsNotifications": {
    "Enabled": true,
    "ServiceType": "twilio",
    "TwilioAccountSid": "AC1234567890abcdef1234567890abcdef",
    "FromPhoneNumber": "+15551234567",
    "ToPhoneNumbers": [
      "+12125551234",
      "+13105555678"
    ],
    "AuthTokenKeyName": "sms:twilio:auth-token",
    "MaxMessageLength": 160,
    "AllowMessageShortening": true
  }
}
```

**Option B: Environment Variables**:

```bash
# Windows (PowerShell)
$env:UNIFIWATCH_SMS_ENABLED = "true"
$env:UNIFIWATCH_SMS_PROVIDER = "twilio"
$env:UNIFIWATCH_TWILIO_SID = "AC1234567890abcdef1234567890abcdef"
$env:UNIFIWATCH_TWILIO_FROM = "+15551234567"
$env:UNIFIWATCH_TWILIO_TO = "+12125551234"
$env:UNIFIWATCH_TWILIO_TOKEN = "your-auth-token"

# Linux/macOS
export UNIFIWATCH_SMS_ENABLED=true
export UNIFIWATCH_SMS_PROVIDER=twilio
export UNIFIWATCH_TWILIO_SID=AC1234567890abcdef1234567890abcdef
export UNIFIWATCH_TWILIO_FROM=+15551234567
export UNIFIWATCH_TWILIO_TO=+12125551234
export UNIFIWATCH_TWILIO_TOKEN=your-auth-token
```

### Configuration Options Explained

- **`Enabled`**: Set to `true` to enable SMS notifications
- **`ServiceType`**: SMS provider (`"twilio"` currently supported, AWS SNS and Vonage coming later)
- **`TwilioAccountSid`**: Your Twilio Account SID (starts with `AC`)
- **`FromPhoneNumber`**: Your Twilio phone number in E.164 format (`+15551234567`)
- **`ToPhoneNumbers`**: Array of recipient phone numbers in E.164 format
- **`AuthTokenKeyName`**: Credential storage key for Twilio auth token (default: `"sms:twilio:auth-token"`)
- **`MaxMessageLength`**: SMS character limit (default: `160`, standard SMS length)
- **`AllowMessageShortening`**: Auto-truncate long messages to fit SMS limit (default: `true`)

### Phone Number Validation

UnifiWatch automatically validates and normalizes phone numbers to E.164 format. These formats are all valid:

- **E.164 (preferred)**: `+12125551234`
- **US with formatting**: `(212) 555-1234` → normalized to `+12125551234`
- **US with dashes**: `212-555-1234` → normalized to `+12125551234`
- **Plain digits**: `2125551234` → normalized to `+12125551234`

❌ **Invalid formats** (will be rejected):
- Too short: `123`
- Letters: `abc-def-ghij`
- Empty: ``

### Message Length and Shortening

SMS messages are limited to **160 characters**. UnifiWatch handles this automatically:

1. **Short messages** (≤160 chars): Sent as-is
2. **Long messages** (>160 chars): 
   - If `AllowMessageShortening = true`: Automatically truncated to 157 chars + `"..."` 
   - Shortening is smart: breaks at word boundaries when possible
   - If `AllowMessageShortening = false`: Message send will fail

**Example**:
```
Original (~185 chars):
"[IN STOCK] Good news! UniFi Dream Machine Pro with Ultra High Performance and Advanced Security Features (UDM-PRO-ULTRA-ADVANCED-2024) at United States of America - Official Ubiquiti Store. Price: $379.00"

Shortened (≤160 chars):
"[IN STOCK] Good news! UniFi Dream Machine Pro (UDM-PRO-ULTRA-ADVANCED-2024) at USA - Ubiquiti Store..."
```

### Twilio Free Trial Limitations

- **Verified numbers only**: Can only send to phone numbers you've verified in Twilio Console
- **Trial watermark**: Messages include "Sent from your Twilio trial account" prefix
- **Credit limit**: $15.50 credit (~500 SMS in US, varies by country)
- **Upgrade**: Remove limitations by adding payment method to Twilio account

To verify a number during trial:
1. Go to Twilio Console → Phone Numbers → Verified Caller IDs
2. Click "+ Add a new Caller ID"
3. Enter phone number, verify via SMS code

### Testing SMS Configuration

```bash
# Test SMS notifications (command coming in Phase 6)
unifiwatch --test-notifications --sms-only
```

---

## 🔔 Combined Configuration Example

Enable both email and SMS notifications:

```json
{
  "Service": {
    "ServiceName": "UnifiWatch",
    "CheckIntervalSeconds": 60,
    "Language": "en-CA",
    "TimeZone": "America/Toronto"
  },
  "EmailNotifications": {
    "Enabled": true,
    "SmtpServer": "smtp.gmail.com",
    "SmtpPort": 587,
    "UseSsl": true,
    "FromAddress": "alerts@example.com",
    "FromName": "UnifiWatch Stock Alert",
    "ToAddresses": [
      "admin@example.com",
      "team@example.com"
    ],
    "CredentialKey": "smtp:password"
  },
  "SmsNotifications": {
    "Enabled": true,
    "ServiceType": "twilio",
    "TwilioAccountSid": "AC1234567890abcdef1234567890abcdef",
    "FromPhoneNumber": "+15551234567",
    "ToPhoneNumbers": [
      "+12125551234"
    ],
    "AuthTokenKeyName": "sms:twilio:auth-token",
    "MaxMessageLength": 160,
    "AllowMessageShortening": true
  },
  "Monitoring": {
    "Store": "USA",
    "ProductSkus": ["UDM-PRO", "UDR"],
    "NotifyOnInStock": true,
    "NotifyOnOutOfStock": false
  }
}
```

---

## 🌍 Multi-Language Support

UnifiWatch supports notifications in multiple languages. Email and SMS messages are automatically translated based on your configured language:

- **English (Canada)**: `en-CA` (default)
- **French (Canada)**: `fr-CA`
- **French (France)**: `fr-FR`
- **German**: `de-DE`
- **Spanish**: `es-ES`
- **Italian**: `it-IT`

Set language in config:
```json
{
  "Service": {
    "Language": "de-DE"  // German notifications
  }
}
```

Or via CLI flag:
```bash
unifiwatch --wait --store USA --language de-DE
```

**Example notification in German**:
- Subject: `✓ Dream Machine Pro Verfügbar`
- Body: `Gute Nachrichten! Dream Machine Pro (UDM-PRO) bei USA. Preis: 379,00 €. Jetzt prüfen!`

---

## 🔧 Troubleshooting

### Email Not Sending

**Check credentials**:
```bash
# Verify SMTP password is stored
# Windows: Check Credential Manager (credential name: smtp:password)
# macOS: security find-generic-password -s "smtp:password"
# Linux: Check ~/.local/share/unifiwatch/credentials/ directory
```

**Common issues**:
- ❌ **"Authentication failed"**: Wrong password or need app-specific password (Gmail)
- ❌ **"Connection refused"**: Wrong SMTP server or port
- ❌ **"SSL/TLS error"**: Try toggling `UseSsl` setting
- ❌ **"Relay not permitted"**: `FromAddress` doesn't match SMTP account

**Enable debug logging** (add to config):
```json
{
  "Logging": {
    "LogLevel": {
      "UnifiWatch.Services.Notifications": "Debug"
    }
  }
}
```

### SMS Not Sending

**Check Twilio credentials**:
```bash
# Test Twilio credentials manually
curl -X POST https://api.twilio.com/2010-04-01/Accounts/YOUR_ACCOUNT_SID/Messages.json \
  --data-urlencode "To=+12125551234" \
  --data-urlencode "From=+15551234567" \
  --data-urlencode "Body=Test from UnifiWatch" \
  -u YOUR_ACCOUNT_SID:YOUR_AUTH_TOKEN
```

**Common issues**:
- ❌ **"Invalid phone number"**: Must use E.164 format (`+1234567890`)
- ❌ **"Unverified number"**: Twilio trial accounts can only send to verified numbers
- ❌ **"Insufficient funds"**: Trial credit exhausted, need to upgrade account
- ❌ **"21608 error"**: `From` number not owned by your Twilio account
- ❌ **"Authentication failed"**: Wrong Account SID or Auth Token

**View Twilio logs**:
1. Go to Twilio Console → Monitor → Logs → Messaging
2. Check recent message attempts and error details

### Phone Number Format Issues

UnifiWatch accepts various phone formats but some may fail validation:

✅ **Valid**:
- `+12125551234` (E.164)
- `(212) 555-1234` (US formatted)
- `212-555-1234` (US dashed)
- `2125551234` (plain digits, assumes +1)

❌ **Invalid**:
- `123` (too short)
- `abc-def-ghij` (contains letters)
- Missing country code for international numbers

**Fix**: Always include country code for international numbers:
- Canada/US: `+1` prefix
- Germany: `+49` prefix
- Spain: `+34` prefix
- France: `+33` prefix

---

## 📝 Security Best Practices

1. **Never commit credentials to git**:
   - Add `config.json` to `.gitignore`
   - Use environment variables for CI/CD
   - Store passwords in OS credential managers

2. **Use app-specific passwords**:
   - Gmail: Generate app password (not regular password)
   - Outlook: Use app password for 2FA accounts

3. **Rotate credentials regularly**:
   - Change SMTP passwords every 90 days
   - Regenerate Twilio auth tokens if compromised

4. **Limit recipient lists**:
   - Only send to necessary recipients
   - Avoid distributing credentials

5. **Use encrypted connections**:
   - Always set `UseSsl = true` for SMTP
   - Twilio uses HTTPS automatically

---

## 🚀 Future Provider Support

**Coming in Phase 3b extensions**:

### AWS SNS (SMS)
- Cost-effective SMS via Amazon SNS
- Configuration: AWS Access Key, Secret Key, Region
- No phone number purchase needed

### Vonage (SMS)
- Alternative SMS provider
- Configuration: API Key, API Secret, From number

### Azure Communication Services (Email + SMS)
- Microsoft Azure-based notifications
- Enterprise-grade reliability
- Configuration: Connection string, From address/number

See `BUILD_PLAN.md` Phase 3b for implementation timeline.

---

## 📚 Additional Resources

- **Twilio Documentation**: [https://www.twilio.com/docs/sms](https://www.twilio.com/docs/sms)
- **Gmail SMTP Guide**: [https://support.google.com/mail/answer/7126229](https://support.google.com/mail/answer/7126229)
- **E.164 Phone Format**: [https://www.twilio.com/docs/glossary/what-e164](https://www.twilio.com/docs/glossary/what-e164)
- **UnifiWatch GitHub**: [https://github.com/johnmccrae/UnifiWatch](https://github.com/johnmccrae/UnifiWatch)

---

**Questions or issues?** Open an issue on GitHub or check the troubleshooting section above.
