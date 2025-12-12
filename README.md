# UnifiWatch - C# Edition

## What is This?

UnifiWatch is a tool that automatically checks if Ubiquiti networking products are in stock at their online stores. Instead of manually refreshing the Ubiquiti store website hoping your desired product comes back in stock, this program does it for you automatically.

This is a C# rewrite of the original PowerShell module created by [Evotec](https://github.com/EvotecIT) of Poland. The C# version provides better cross-platform support and performance while maintaining all the functionality of the original.

Think of it as a personal shopping assistant that watches the store 24/7 and alerts you the moment your product becomes available.

## Why Would I Use This?

Ubiquiti products (like UniFi WiFi access points, security cameras, switches, and routers) are often sold out due to high demand. This tool helps you:

- **Save Time**: No more constantly checking the website manually
- **Never Miss a Restock**: Get notified immediately when products become available
- **Monitor Multiple Products**: Watch several products at once
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
`
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
`

**Shopify REST API:**
`
GET https://br.store.ui.com/collections/unifi-protect/products.json
`

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
- **Microsoft.Toolkit.Uwp.Notifications** (7.1.3) - Windows Toast Notifications (Windows only)

### Notification System

The application provides rich, branded notifications across all platforms:

**Windows**: 
- Uses Windows Toast Notifications with Ubiquiti branding
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

**Cross-Platform Features:**
- All notifications include the title and detailed product information
- Windows notifications include interactive arguments for potential future click handling
- Consistent branding across all platforms
- Automatic fallback to console output if native notifications fail

## Troubleshooting

**Error: "You must specify either --stock or --wait"**
- You need to choose a mode. Add either `--stock` or `--wait` to your command.

**Error: "You must specify either --store or --legacy-api-store"**
- You need to specify which store. Add either `--store USA` or `--legacy-api-store Brazil`.

**Error: "Cannot specify both --stock and --wait"**
- Only use one mode at a time.

**No products found:**
- Check that the store name is correct (case-sensitive)
- Try without filters first to see all products
- Some stores may have no products (Brazil store appears empty in testing)

**Linux/macOS: "Permission denied"**
- Make the executable file executable: `chmod +x UnifiWatch`

## Credits

- Original PowerShell module by [EvotecIT/UnifiWatch](https://github.com/EvotecIT/UnifiWatch)
- C# rewrite maintains the same API monitoring approach
- API discovery and implementation patterns from original project

## License

This project maintains the same open-source spirit as the original PowerShell module.
