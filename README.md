# UnifiWatch - C# Edition

## What is This?

UnifiWatch is a tool that automatically checks if Ubiquiti networking products are in stock at their online stores. Instead of manually refreshing the Ubiquiti store website hoping your desired product comes back in stock, this program does it for you automatically.

This is a C# rewrite of the original PowerShell module [UnifiStockTracker](https://github.com/EvotecIT/UnifiStockTracker) created by [Evotec](https://github.com/EvotecIT/UnifiStockTracker) (Przemysław Kłys). The C# version provides better cross-platform support and performance while maintaining all the functionality of the original.

Think of it as a personal shopping assistant that watches the store 24/7 and alerts you the moment your product becomes available.

## Why Would I Use This?

Ubiquiti products (like UniFi WiFi access points, security cameras, switches, and routers) are often sold out due to high demand. This tool helps you:

- **Save Time**: No more constantly checking the website manually
- **Never Miss a Restock**: Get notified immediately when products become available via email, SMS, or desktop notifications
- **Monitor Multiple Products**: Watch several products at once across different stores
- **Multiple Notification Options**: Choose email (SMTP or OAuth), SMS, Discord, or desktop alerts
- **Run as a Service**: 24/7 background monitoring on Windows, macOS, or Linux
- **Open the Store Automatically**: When stock is found, it opens the product page in your browser
- **Get Alerts**: Hear a beep notification when stock is available (Windows only)

## How Does It Work?

The program connects to Ubiquiti's online store systems using two different methods:

### 1. GraphQL API (US/EU/UK stores)

- **Endpoint**: `https://ecomm.svc.ui.com/graphql`
- **Method**: POST
- **Query**: `GetProductsForLandingPagePro`
- **Features**:
  - Fetches products in paginated batches (250 items per request)
  - Returns structured product data with variants
  - Checks `variant.status == "AVAILABLE"` for stock availability
- **Supported Stores**: USA, Europe, UK
- **Command**: Use `--store` option

### 2. Shopify REST API (Other stores)

- **URL Pattern**: `{store_url}/collections/{collection}/products.json`
- **Method**: GET
- **Examples**:
  - `https://br.store.ui.com/collections/unifi-protect/products.json`
  - `https://store-ui.in/collections/unifi-network-wireless/products.json`
- **Features**:
  - Standard Shopify products JSON API
  - Checks `variant.available` boolean for stock status
- **Supported Stores**: Brazil, India, Japan, Taiwan, Singapore, Mexico, China
- **Command**: Use `--legacy-api-store` option

## Prerequisites

- .NET 9.0 SDK or later
- Windows, Linux, or macOS

## Building the Application

### For Windows

1. Open PowerShell
2. Navigate to the program folder:

   ```powershell
   cd UnifiWatch
   ```

3. Build as a standalone executable:

   ```powershell
   dotnet publish -c Release -r win-x64 --self-contained -p:PublishSingleFile=true
   ```

The executable will be created at: `bin\Release\net9.0\win-x64\publish\UnifiWatch.exe`

### For Linux

1. Open a terminal
2. Navigate to the program folder:

   ```bash
   cd UnifiWatch
   ```

3. Build as a standalone executable:

   ```bash
   dotnet publish -c Release -r linux-x64 --self-contained -p:PublishSingleFile=true
   ```

The executable will be created at: `bin/Release/net9.0/linux-x64/publish/UnifiWatch`

You may need to make it executable:

```bash
chmod +x bin/Release/net9.0/linux-x64/publish/UnifiWatch
```

### For macOS

1. Open Terminal
2. Navigate to the program folder:

   ```bash
   cd UnifiWatch
   ```

3. Build as a standalone executable:

   ```bash
   dotnet publish -c Release -r osx-x64 --self-contained -p:PublishSingleFile=true
   ```

The executable will be created at: `bin/Release/net9.0/osx-x64/publish/UnifiWatch`

You may need to make it executable:

```bash
chmod +x bin/Release/net9.0/osx-x64/publish/UnifiWatch
```

**Note:** You can run the executables from anywhere after building, or copy them to a location in your PATH for system-wide access.

## Running the Application

### Using the Standalone Executable

**Windows (PowerShell):**

```powershell
.\UnifiWatch.exe --stock --store USA
```

**Linux/macOS (Bash/Zsh):**

```bash
./UnifiWatch --stock --store USA
```

### Using dotnet run (without publishing)

If you haven't published the executable yet, you can run directly with:

**All platforms:**

```bash
sudo dotnet run --project UnifiWatch.csproj -- --stock --store USA
```

### Command Structure

The tool uses option-based commands instead of subcommands:

**Mode Options (choose one):**

- `--stock` - Get current stock availability
- `--wait` - Monitor products and alert when in stock

**Store Options (choose one):**

- `--store <name>` - For GraphQL API stores (USA, Europe, UK)
- `--legacy-api-store <name>` - For Shopify API stores (Brazil, India, Japan, Taiwan, Singapore, Mexico, China)

**Filter Options (optional):**

- `--collections <name>` - Filter by collection names
- `--product-names <pattern>` - Filter by product name patterns
- `--product-skus <sku>` - Filter by product SKUs

**Wait Options (for `--wait` mode only):**

- `--seconds <number>` - Check interval in seconds (default: 60)
- `--no-website` - Don't open browser when product found
- `--no-sound` - Don't play alert sound when product found

### Examples

**Check all stock in USA store:**

Windows (PowerShell):

```powershell
.\UnifiWatch.exe --stock --store USA
```

Linux/macOS:

```bash
./UnifiWatch --stock --store USA
```

**Check stock in Brazil store (Shopify API):**

Windows (PowerShell):

```powershell
.\UnifiWatch.exe --stock --legacy-api-store Brazil
```

Linux/macOS:

```bash
./UnifiWatch --stock --legacy-api-store Brazil
```

**Monitor for Dream Machine in USA store:**

Windows (PowerShell):

```powershell
.\UnifiWatch.exe --wait --store USA --product-names "Dream Machine"
```

Linux/macOS:

```bash
./UnifiWatch --wait --store USA --product-names "Dream Machine"
```

**Monitor specific SKU in Europe:**

Windows (PowerShell):

```powershell
.\UnifiWatch.exe --wait --store Europe --product-skus "UDM-Pro"
```

Linux/macOS:

```bash
./UnifiWatch --wait --store Europe --product-skus "UDM-Pro"
```

**Wait for stock in Japan with custom interval:**

Windows (PowerShell):

```powershell
.\UnifiWatch.exe --wait --legacy-api-store Japan --product-names "UniFi" --seconds 120
```

Linux/macOS:

```bash
./UnifiWatch --wait --legacy-api-store Japan --product-names "UniFi" --seconds 120
```

**Silent monitoring (no browser, no sound):**

Windows (PowerShell):

```powershell
.\UnifiWatch.exe --wait --store UK --product-names "Camera" --no-website --no-sound
```

Linux/macOS:

```bash
./UnifiWatch --wait --store UK --product-names "Camera" --no-website --no-sound
```

## Supported Stores

### GraphQL API Stores (use `--store`)

- **USA** (`--store USA`)
- **Europe** (`--store Europe`)
- **UK** (`--store UK`)

### Shopify API Stores (use `--legacy-api-store`)

- **Brazil** (`--legacy-api-store Brazil`)
- **India** (`--legacy-api-store India`)
- **Japan** (`--legacy-api-store Japan`)
- **Taiwan** (`--legacy-api-store Taiwan`)
- **Singapore** (`--legacy-api-store Singapore`)
- **Mexico** (`--legacy-api-store Mexico`)
- **China** (`--legacy-api-store China`)

## What Happens When Stock is Found?

When the `--wait` command finds products in stock:

1. **Native Notification**: Shows a system notification with product details
   - **Windows**: Windows Toast Notification appears in Action Center
   - **macOS**: Notification Center alert with product name and details
   - **Linux**: Desktop notification via notify-send (if available)
2. **Console Output**: Lists all matching products that are available
3. **Browser Opens**: Automatically opens the product page (unless `--no-website` is used)
4. **Alert Sound**: Plays a beep notification on Windows (unless `--no-sound` is used)

## Technical Details

### API Implementation

**GraphQL API:**

```json
POST https://ecomm.svc.ui.com/graphql
Content-Type: application/json

{
  "query": "query GetProductsForLandingPagePro(...)",
  "variables": {
    "pageNumber": 1,
    "itemsPerPage": 250,
    "collectionSlugs": ["dream-machine", "camera-security-compact-poe-wired"]
  }
}
```

**Shopify REST API:**

```json
GET https://br.store.ui.com/collections/unifi-protect/products.json
```

### Project Structure

- `Models/` - Data models for products and API responses
  - `UnifiProduct.cs` - Common product representation
  - `GraphQLModels.cs` - GraphQL API request/response types
  - `LegacyModels.cs` - Shopify API response types
- `Services/` - API interaction services
  - `unifiwatchService.cs` - GraphQL API implementation
  - `unifiwatchLegacyService.cs` - Shopify API implementation
  - `StockWatcher.cs` - Stock monitoring with notifications
- `Configuration/` - Store URLs and collection mappings
  - `StoreConfiguration.cs` - Store endpoints and collection data
- `Program.cs` - CLI entry point using System.CommandLine

### Dependencies

- **System.CommandLine** (2.0.0-beta4) - Modern command-line parsing
- **System.Text.Json** (9.0.0) - JSON serialization
- **Microsoft.Extensions.Http** (9.0.0) - HTTP client factory
- **Windows.UI.Notifications** (via PowerShell) - Windows Toast Notifications (Windows only, no NuGet dependency)

### Notification System

The application provides rich, branded notifications across all platforms:

**Windows**:

- Uses Windows Toast Notifications via PowerShell (works in interactive and service modes)
- Features the official Ubiquiti logo in the notification
- Notifications appear in the Action Center and persist until dismissed
- Can include multiple products in a single alert
- Supports long-duration display for better visibility

**macOS**:

- Uses AppleScript to trigger native Notification Center alerts
- Includes "Ubiquiti Stock Alert" subtitle for branding
- Notifications appear in the top-right corner
- Follows system notification settings and do-not-disturb preferences

**Linux**:

- Uses `notify-send` with Ubiquiti branding and network icon
- Falls back to console output if notify-send is not available
- Compatible with most desktop environments (GNOME, KDE, XFCE)

**Cross-Platform Features**:

- All notifications include the title and detailed product information
- Windows notifications include interactive arguments for potential future click handling
- Consistent branding across all platforms
- Automatic fallback to console output if native notifications fail

## Troubleshooting

### Error: "You must specify either --stock or --wait"

You need to choose a mode. Add either `--stock` or `--wait` to your command.

### Error: "You must specify either --store or --legacy-api-store"

You need to specify which store. Add either `--store USA` or `--legacy-api-store Brazil`.

### Error: "Cannot specify both --stock and --wait"

Only use one mode at a time.

### No products found

- Check that the store name is correct (case-sensitive)
- Try without filters first to see all products
- Some stores may have no products (Brazil store appears empty in testing)

### Linux/macOS: "Permission denied"

Make the executable file executable: `chmod +x UnifiWatch`

## Service Mode

UnifiWatch supports both CLI mode (manual commands) and Service Mode (background monitoring). Service Mode runs as a system service on Windows, Linux, or macOS, continuously monitoring for stock availability and sending notifications.

### Quick Start

**Setup and start the service:**

Windows (PowerShell, Administrator):

```powershell
.\UnifiWatch.exe --install-service
# Service starts automatically - check Windows Services (services.msc)
```

Linux/macOS (Terminal):

```bash
sudo ./UnifiWatch --install-service
# Service starts automatically - check systemd (systemctl status unifiwatch)
```

**Configure notifications and products:**

Windows (PowerShell):

```powershell
.\UnifiWatch.exe --configure
# Interactive wizard: email, SMS, Discord, products, check interval
```

Linux/macOS (Terminal):

```bash
./UnifiWatch --configure
# Interactive wizard: email, SMS, Discord, products, check interval
```

**View current configuration:**

Windows (PowerShell):

```powershell
.\UnifiWatch.exe --show-config
```

Linux/macOS (Terminal):

```bash
./UnifiWatch --show-config
```

### Features

- **24/7 Background Monitoring**: Service runs continuously checking stock
- **Multiple Notification Channels**: 
  - Email (SMTP or Microsoft Graph OAuth with shared mailbox support)
  - SMS (Twilio)
  - Desktop notifications (native OS integration)
  - Discord webhooks
- **Flexible Email Authentication**:
  - **SMTP**: Traditional username/password authentication
  - **OAuth 2.0**: Microsoft Graph API with client credentials flow for shared mailboxes
- **Interactive Configuration Wizard**: Easy setup with guided prompts for all settings
- **Secure Credential Storage**:
  - Windows: Windows Credential Manager (DPAPI encryption)
  - macOS: Keychain
  - Linux: Secret Service or encrypted file storage
- **Multi-Language Support**: English, French, German, Spanish, Italian, Portuguese (Brazil)
- **Cross-Platform**: Windows, Linux (systemd), macOS (launchd)
- **Automatic Updates**: Updates configuration without service restart
- **Detailed Logging**: View service logs for debugging and monitoring

### Comprehensive Documentation

For detailed service setup, configuration, and troubleshooting:

- **[SERVICE_SETUP.md](SERVICE_SETUP.md)** - Platform-specific installation and operation guides
- **[SECURITY.md](SECURITY.md)** - Credential storage and security best practices
- **[Example Configurations](examples/)** - Ready-to-use config templates

### Example Configurations

Ready-made configuration examples in `examples/` directory:

- `config.minimal.json` - Desktop notifications only
- `config.email-only.json` - Email notifications with SMTP
- `config.sms-twilio.json` - SMS notifications with Twilio
- `config.all-channels.json` - All notification types enabled

## Localization

UnifiWatch supports the following languages:
- **English** (en-CA)
- **French** (fr-CA, fr-FR)
- **German** (de-DE)
- **Spanish** (es-ES)
- **Italian** (it-IT)
- **Portuguese (Brazil)** (pt-BR)

Resource files live under `Resources/` and are JSON per culture and category: `CLI.<culture>.json`, `Notifications.<culture>.json`, `Errors.<culture>.json`.
- Fallback order is culture name → two-letter language code → `en-CA`. Missing keys return the key name.
- To add a new locale:
  - Copy the `en-CA` files as a template and translate the values.
  - Keep the keys unchanged; they are referenced in code.
  - Include the new files in `Resources/` (the project copies `*.json` on build).
- **Parity Testing**: Run `dotnet test --filter LocalizationParityTests` to ensure all locale files have matching key sets (baseline: `en-CA`). This prevents missing translations and inconsistent resource keys across locales.
- **Fallback & Robustness**: Run `dotnet test --filter ResourceLocalizerFallbackTests` to verify graceful handling of malformed JSON files and proper localization of all messages.
- Testing: run `dotnet test` to ensure resource JSON is valid and look for localized output in CLI flows (`--wait` warnings, product lists, notifications).

## Acknowledgments

This project is derived from [UnifiStockTracker](https://github.com/EvotecIT/UnifiStockTracker), a PowerShell module originally created by [EvotecIT](https://github.com/EvotecIT) (Przemysław Kłys). The C# rewrite adapts the original API monitoring strategies and command-line interface design while expanding functionality with email/SMS notifications, OAuth support, and multi-platform deployment. Both projects are distributed under the MIT License.

## License

This project is licensed under the MIT License. See the [LICENSE](LICENSE) file for details.

Both UnifiWatch and the original UnifiStockTracker are open-source projects distributed under the MIT License (Copyright © 2022 Evotec), permitting free use, modification, and distribution with proper attribution.

