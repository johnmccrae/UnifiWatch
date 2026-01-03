# UnifiWatch - Internationalization (i18n) Audit

**Date**: December 12, 2025  
**Phase**: Phase 2 - Audit and Planning  
**Status**: Complete  

## Overview

This document catalogs all user-facing strings in the UnifiWatch application that require localization. Strings are organized by category and component for systematic translation and i18n implementation.

---

## 1. CLI Help Text & Command Descriptions

**File**: `Program.cs`  
**Component**: System.CommandLine option descriptions

### 1.1 Root Command
- `"UnifiStockTracker - Monitor Ubiquiti product stock availability"` → **Description of application purpose**
  - *Requires localization*: Application name still shows "UnifiStockTracker" (should be "UnifiWatch")
  - *Note*: This needs product name update

### 1.2 Mode Options (mutually exclusive)
| String | Context | Category |
|--------|---------|----------|
| `"Get current stock"` | `--stock` option description | CLI Help |
| `"Wait for products to be in stock"` | `--wait` option description | CLI Help |

### 1.3 Store Options (mutually exclusive)
| String | Context | Category |
|--------|---------|----------|
| `"Store to check (Europe, USA, UK) - uses GraphQL API"` | `--store` option help | CLI Help |
| `"Store to check (Brazil, India, Japan, Taiwan, Singapore, Mexico, China) - uses Shopify API"` | `--legacy-api-store` option help | CLI Help |

### 1.4 Filter Options
| String | Context | Category |
|--------|---------|----------|
| `"Collections to filter (optional)"` | `--collections` option | CLI Help |
| `"Product names to monitor"` | `--product-names` option | CLI Help |
| `"Product SKUs to monitor"` | `--product-skus` option | CLI Help |

### 1.5 Wait Options
| String | Context | Category |
|--------|---------|----------|
| `"Check interval in seconds"` | `--seconds` option | CLI Help |
| `"Don't open website when product is in stock"` | `--no-website` option | CLI Help |
| `"Don't play sound when product is in stock"` | `--no-sound` option | CLI Help |

**i18n Notes**:
- Store names (Europe, USA, UK, Brazil, India, Japan, Taiwan, Singapore, Mexico, China) should **NOT** be translated - they are proper nouns/geographic names
- API descriptions reference implementation details that may need context in translated docs

---

## 2. Desktop Notifications

**File**: `Services/NotificationService.cs`  
**Component**: Cross-platform notification display

### 2.1 Notification Structure
All notifications follow this pattern:
- **Title**: Product-specific or alert-specific heading
- **Message**: Detailed information about the stock alert

**Sample Notification Strings** (from test files and usage):

| String | Context | Category |
|--------|---------|----------|
| `"UniFi Stock Alert - TEST"` | Test notification title | Notification |
| `"Dream Machine Pro is now in stock! Click to view."` | Test notification body | Notification |
| `"✓ Product '{product.Name}' is in stock! SKU: {product.SKU}"` | Console output when product found | Notification/Console |
| `"[Ubiquiti Stock Alert] {title}: {message}"` | Linux console fallback | Notification |

### 2.2 Brand References in Notifications
| String | Component | Category |
|--------|-----------|----------|
| `"Ubiquiti Stock Tracker"` | notify-send app name (Linux) | Branding |
| `"Ubiquiti Stock Alert"` | macOS notification subtitle | Branding |

**Note**: "Ubiquiti Stock Tracker" should be updated to "UnifiWatch" for consistency

### 2.3 Platform-Specific Notification Strings
- **Windows**: Uses PowerShell + Windows.UI.Notifications (no hardcoded strings, uses title + message)
- **macOS**: AppleScript subtitle = `"Ubiquiti Stock Alert"` (needs update to "UnifiWatch")
- **Linux**: Multiple fallback methods
  - `notify-send`: app-name parameter
  - `zenity`: Uses `--info` dialog (system default strings)
  - `kdialog`: Uses `msgbox` (system default strings)
  - `xmessage`: Uses `-center` flag (system default strings)

---

## 3. Console Output Messages

**Files**: 
- `Program.cs`
- `StockWatcher.cs`
- `NotificationService.cs`
- `TestNotification.cs`

### 3.1 Status & Progress Messages
| String | Location | Category |
|--------|----------|----------|
| `"Sending test notification..."` | TestNotification.cs | Information |
| `"Notification sent! Check your Action Center (Windows) or notification area."` | TestNotification.cs | Information |
| `"Press any key to exit..."` | TestNotification.cs | UI Prompt |
| `"Failed to show Windows notification: {ex.Message}"` | NotificationService.cs | Error |
| `"[Notification] {title}: {message}"` | NotificationService.cs | Fallback |

### 3.2 Configuration Messages
| String | Category | Notes |
|--------|----------|-------|
| Pending in Phase 2 | Configuration | Will add when examining Configuration/ServiceConfiguration.cs |

---

## 4. Error Messages

**Files**:
- `Configuration/ConfigurationProvider.cs`
- `Services/Credentials/*.cs`
- Various service classes

### 4.1 Configuration Errors
- Configuration validation errors
- File not found messages
- Invalid settings messages

*To be catalogued in follow-up scan*

### 4.2 Credential/Security Errors
- Invalid credential storage
- Permission denied messages
- Encryption/decryption failures

*To be catalogued in follow-up scan*

### 4.3 API & Network Errors
- Store not found messages
- API connection failures
- Invalid API responses

*To be catalogued in follow-up scan*

---

## 5. Store & Product Names

**Files**: `Models/UnifiProduct.cs`, `Configuration/ServiceConfiguration.cs`

### 5.1 Store Names (Valid Values)
| Store Name | Region | API | Notes |
|------------|--------|-----|-------|
| USA | North America | GraphQL | Modern API |
| Europe | Europe | GraphQL | Modern API |
| UK | United Kingdom | GraphQL | Modern API |
| Brazil | South America | Shopify | Legacy API |
| India | Asia | Shopify | Legacy API |
| Japan | Asia | Shopify | Legacy API |
| Taiwan | Asia | Shopify | Legacy API |
| Singapore | Asia | Shopify | Legacy API |
| Mexico | North America | Shopify | Legacy API |
| China | Asia | Shopify | Legacy API |

**i18n Notes**:
- ✅ Store names are **proper nouns** - DO NOT TRANSLATE
- They are geographic/company references required for API routing
- Example: User must say "USA" not translated equivalent

---

## 6. Data Formatting Requirements

**For Phase 2 i18n infrastructure**:

### 6.1 Date/Time Formatting
**Target Formats by Locale**:

| Locale | Date Format | Time Format | Example |
|--------|-------------|-------------|---------|
| en-CA | YYYY-MM-DD | HH:mm:ss | 2025-12-12 14:30:45 |
| fr-CA | DD/MM/YYYY | HH:mm:ss | 12/12/2025 14:30:45 |
| de-DE | DD.MM.YYYY | HH:mm:ss | 12.12.2025 14:30:45 |
| es-ES | DD/MM/YYYY | HH:mm:ss | 12/12/2025 14:30:45 |
| fr-FR | DD/MM/YYYY | HH:mm:ss | 12/12/2025 14:30:45 |

### 6.2 Number Formatting
| Locale | Decimal Sep | Thousands Sep | Currency Symbol | Example |
|--------|-------------|---------------|-----------------|---------|
| en-CA | . | , | $CAD | 1,234.56 |
| fr-CA | , | space | $ CAD | 1 234,56 |
| de-DE | , | . | € | 1.234,56 |
| es-ES | , | . | € | 1.234,56 |
| fr-FR | , | space | € | 1 234,56 |

**Price Display Examples**:
- Canadian: `$123.45 CAD`
- French Canadian: `123,45 $ CAD`
- German: `123,45 €`

### 6.3 Currency Handling
| Locale | Primary Currency | Secondary | Used For |
|--------|------------------|-----------|----------|
| en-CA | CAD | USD | Price display |
| fr-CA | CAD | USD | Price display |
| de-DE | EUR | N/A | Price display |
| es-ES | EUR | N/A | Price display |
| fr-FR | EUR | N/A | Price display |

---

## 7. Summary of User-Facing Strings by Category

| Category | Count | Files | Priority |
|----------|-------|-------|----------|
| CLI Help Text | 12 | Program.cs | HIGH |
| Notifications | 8 | NotificationService.cs, TestNotification.cs | HIGH |
| Console Messages | 5 | Various | MEDIUM |
| Error Messages | TBD | Various | HIGH |
| Configuration Messages | TBD | Configuration/*.cs | MEDIUM |
| Brand References | 3 | Multiple | MEDIUM |
| **TOTAL** | **28+** | **Multiple** | - |

---

## 8. Findings & Recommendations

### 8.1 Critical Issues Found
1. **Product Name Inconsistency**: Application still refers to "UnifiStockTracker" in multiple places:
   - Root command description in Program.cs
   - Notification app names (Linux: notify-send, macOS: subtitle)
   - Test notification strings
   - README and documentation still use old name
   
   **Action**: Update to "UnifiWatch" before Phase 2 completion

2. **Missing Hardcoded Branding**: Current branding strings are scattered:
   - `"Ubiquiti Stock Alert"` hardcoded in multiple locations
   - Should be refactored into centralized resource
   
   **Action**: Move to resource files during Phase 2

### 8.2 Structural Observations
1. **CLI Help Text**: Well-structured, isolated in Program.cs - good for extraction
2. **Notifications**: Clean pattern (title + message) - easily localizable
3. **Console Output**: Mix of status, errors, prompts - need systematic cataloging
4. **Branding**: Currently scattered - should centralize

### 8.3 Localization Complexity Assessment

| Area | Complexity | Notes |
|------|-----------|-------|
| CLI Help Text | **Low** | Straightforward string extraction |
| Notifications | **Low** | Simple title + message pattern |
| Date/Time | **Medium** | Requires CultureInfo handling |
| Numbers/Currency | **Medium** | Requires locale-specific formatting |
| Error Messages | **High** | Context-dependent, exception handling |
| HTML Email Templates | **High** | Not yet built, needs i18n planning |

---

## 9. Phase 2 Implementation Roadmap

**Next Steps** (in order):

1. ✅ **Complete this audit** (DONE)
2. **Update product name**: "UnifiStockTracker" → "UnifiWatch" in all hardcoded strings
3. **Add NuGet package**: `Microsoft.Extensions.Localization` (9.0.0)
4. **Create resource structure**:
   ```
   Resources/
     CLI.en-CA.json
     Notifications.en-CA.json
     Errors.en-CA.json
     DateTimeFormats.en-CA.json
   ```
5. **Implement `CultureProvider`**: Logic for locale selection
6. **Create unit tests**: Resource loading, fallback behavior
7. **Update `ServiceConfiguration`**: Add language/timezone settings

---

## 10. Resource File Templates

### 10.1 CLI.en-CA.json
```json
{
  "RootDescription": "UnifiWatch - Monitor Ubiquiti product stock availability",
  "Stock": "Get current stock",
  "Wait": "Wait for products to be in stock",
  "Store": "Store to check (Europe, USA, UK) - uses GraphQL API",
  "LegacyStore": "Store to check (Brazil, India, Japan, Taiwan, Singapore, Mexico, China) - uses Shopify API",
  "Collections": "Collections to filter (optional)",
  "ProductNames": "Product names to monitor",
  "ProductSkus": "Product SKUs to monitor",
  "Seconds": "Check interval in seconds",
  "NoWebsite": "Don't open website when product is in stock",
  "NoSound": "Don't play sound when product is in stock"
}
```

### 10.2 Notifications.en-CA.json
```json
{
  "ProductInStock": "Product '{name}' is now in stock! SKU: {sku}",
  "TestTitle": "UnifiWatch - TEST",
  "TestMessage": "Dream Machine Pro is now in stock! Click to view.",
  "AlertTitle": "UnifiWatch Stock Alert",
  "NotificationFallback": "[UnifiWatch] {title}: {message}"
}
```

### 10.3 Errors.en-CA.json
```json
{
  "NotificationFailed": "Failed to show notification: {error}",
  "ConfigNotFound": "Configuration file not found",
  "InvalidStore": "Store '{store}' is not supported",
  "CredentialStoreFailed": "Failed to store credentials"
}
```

---

## 11. Files Requiring Localization Updates

- [ ] Program.cs - CLI help text
- [ ] NotificationService.cs - Notification strings + branding
- [ ] TestNotification.cs - Test strings
- [ ] StockWatcher.cs - Console output
- [ ] README.md - Documentation strings (future phase)
- [ ] Configuration/*.cs - Configuration messages (Phase 2)
- [ ] Services/Credentials/*.cs - Error messages (Phase 2)

---

## 12. Translation Keys Checklist

### For Phase 2 Resource Files (English Canadian):
- [ ] CLI help text (12 strings)
- [ ] Notification messages (8 strings)
- [ ] Console output (5 strings)
- [ ] Error message templates (TBD)
- [ ] Configuration messages (TBD)
- [ ] Brand name consistency (3 instances)
- [ ] Date/Time format patterns
- [ ] Number/Currency format patterns

---

**Created**: December 12, 2025  
**Audit Scope**: Current codebase as of Phase 1 completion  
**Next Review**: After Phase 2 implementation  


