# SMS Provider Integration

This guide explains how to enable SMS notifications for UnifiWatch. The current implementation ships with Twilio support; AWS SNS and Vonage placeholders are noted for future expansion.

## Quick checklist
- Enable SMS and list recipients in E.164 format (+15551234567).
- Choose provider (`twilio` today).
- Configure the sender/from phone number.
- Store provider credentials using the configured `AuthTokenKeyName` (default `sms:twilio:auth-token`).
- Keep messages within the 160-character limit; the service will shorten when allowed.

## Configuration example (config.json)
```json
{
  "notifications": {
    "sms": {
      "enabled": true,
      "recipients": ["+15551234567", "+442071838750"],
      "serviceType": "twilio",
      "twilioAccountSid": "ACxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx",
      "fromPhoneNumber": "+15557654321",
      "authTokenKeyName": "sms:twilio:auth-token",
      "maxMessageLength": 160,
      "allowMessageShortening": true
    }
  }
}
```

## Twilio setup
1. Set `serviceType` to `twilio`.
2. Set `twilioAccountSid` to your Twilio Account SID.
3. Set `fromPhoneNumber` to a Twilio-verified/sending number (E.164 format).
4. Store the auth token under the key in `authTokenKeyName` (default `sms:twilio:auth-token`) using your configured credential provider (e.g., environment variable, Windows Credential Manager, macOS Keychain, Linux Secret Service, or encrypted file).
5. Add one or more E.164 phone numbers to `recipients`.
6. Keep `maxMessageLength` at 160 unless your carrier plan supports concatenation.

## AWS SNS (placeholder)
- Planned values: `serviceType: "sns"`, AWS access key/secret stored under `authTokenKeyName`, region and topic/phone number settings.
- Implementation pending; configuration shape will mirror Twilio (recipients, max length, shortening toggle).

## Vonage/Nexmo (placeholder)
- Planned values: `serviceType: "vonage"`, API key/secret stored under `authTokenKeyName`, `fromPhoneNumber` for the sender ID, and `recipients` as E.164 numbers.
- Implementation pending; behavior will match Twilio with the same length checks and localization prefixes.

## Verifying your setup
- Run `dotnet test` to execute SMS unit tests (includes localization prefixes and shortening behavior).
- Trigger a stock alert with `--wait` and confirm SMS delivery; logs will indicate send success or error details.


