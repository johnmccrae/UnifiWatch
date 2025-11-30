# UnifiStockTracker - C# Version

A C# rewrite of the PowerShell UnifiStockTracker module for monitoring Ubiquiti product stock availability.

## Stock Tracking Mechanism

This application uses **TWO different API mechanisms** to check Ubiquiti product stock:

### 1. Modern API (US/EU/UK stores) - GraphQL
- **Endpoint**: `https://ecomm.svc.ui.com/graphql`
- **Method**: POST
- **Query**: `GetProductsForLandingPagePro`
- **Features**: 
  - Fetches products in paginated batches (250 items per request)
  - Returns structured product data with variants
  - Checks `variant.status == "AVAILABLE"` for stock availability
- **Supported Stores**: USA, Europe, UK

### 2. Legacy API (Other stores) - Shopify REST
- **URL Pattern**: `{store_url}/collections/{collection}/products.json`
- **Method**: GET
- **Examples**:
  - `https://br.store.ui.com/collections/unifi-protect/products.json`
  - `https://store-ui.in/collections/unifi-network-wireless/products.json`
- **Features**:
  - Standard Shopify products JSON API
  - Checks `variant.available` boolean for stock status
- **Supported Stores**: Brazil, India, Japan, Taiwan, Singapore, Mexico, China

## Prerequisites

- .NET 8.0 SDK or later
- Windows, Linux, or macOS

## Building the Application

```bash
cd CSharp
dotnet restore
dotnet build
```

## Running the Application

### Get Current Stock (Modern Stores)

Get all products from USA store:
```bash
dotnet run -- get-stock --store USA
```

Get specific collections from Europe store:
```bash
dotnet run -- get-stock --store Europe --collections CameraSecurityDome360 CameraSecurityCompactPoEWired
```

Get products from UK store:
```bash
dotnet run -- get-stock --store UK --collections DreamMachine DreamRouter
```

### Get Current Stock (Legacy Stores)

Get all products from Brazil store:
```bash
dotnet run -- get-stock-legacy --store Brazil
```

Get specific collections from India store:
```bash
dotnet run -- get-stock-legacy --store India --collections Protect NetworkWifi
```

### Wait for Products to be in Stock (Modern Stores)

Monitor specific products by name in USA store:
```bash
dotnet run -- wait-stock --store USA --product-names "UniFi6 Mesh" "G4 Doorbell Pro" --seconds 60
```

Monitor by SKU in Europe store:
```bash
dotnet run -- wait-stock --store Europe --product-skus UDR-EU --seconds 60
```

Monitor without opening website or playing sound:
```bash
dotnet run -- wait-stock --store USA --product-names "Access Point AC Lite" --seconds 60 --no-website --no-sound
```

### Wait for Products to be in Stock (Legacy Stores)

Monitor products in Brazil store:
```bash
dotnet run -- wait-stock-legacy --store Brazil --product-names "UniFi6 Mesh" "Camera G4 Pro" --seconds 60
```

Monitor by SKU in Japan store:
```bash
dotnet run -- wait-stock-legacy --store Japan --product-skus UDR-JP --seconds 60
```

## Publishing as Standalone Executable

Create a self-contained executable for Windows:
```bash
dotnet publish -c Release -r win-x64 --self-contained
```

For Linux:
```bash
dotnet publish -c Release -r linux-x64 --self-contained
```

For macOS:
```bash
dotnet publish -c Release -r osx-x64 --self-contained
```

The executable will be in `bin/Release/net8.0/{runtime}/publish/`

## Command Reference

### Commands

- `get-stock` - Get current stock for US/EU/UK stores
- `get-stock-legacy` - Get current stock for Brazil/India/Japan/Taiwan/Singapore/Mexico/China stores
- `wait-stock` - Wait for products to be in stock (US/EU/UK stores)
- `wait-stock-legacy` - Wait for products to be in stock (legacy stores)

### Common Options

- `--store` - Store to check (required)
- `--collections` - Filter by specific collections (optional, multiple values allowed)
- `--product-names` - Product names to monitor (wait commands, multiple values allowed)
- `--product-skus` - Product SKUs to monitor (wait commands, multiple values allowed)
- `--seconds` - Check interval in seconds (wait commands, default: 60)
- `--no-website` - Don't open website when product is in stock (wait commands)
- `--no-sound` - Don't play sound notification (wait commands)

### Supported Stores

**Modern Stores (GraphQL API):**
- Europe
- USA
- UK

**Legacy Stores (Shopify API):**
- Brazil
- India
- Japan
- Taiwan
- Singapore
- Mexico
- China

## Project Structure

```
CSharp/
├── UnifiStockTracker.csproj    # Project file with dependencies
├── Program.cs                   # Main entry point with CLI
├── Models/
│   ├── UnifiProduct.cs         # Common product model
│   ├── GraphQLModels.cs        # GraphQL request/response models
│   └── LegacyModels.cs         # Shopify API models
├── Services/
│   ├── UnifiStockService.cs    # Modern GraphQL API service
│   ├── UnifiStockLegacyService.cs  # Legacy Shopify API service
│   └── StockWatcher.cs         # Stock monitoring service
└── Configuration/
    └── StoreConfiguration.cs   # Store URLs and collection mappings
```

## Features

- ✅ Support for both modern GraphQL and legacy Shopify APIs
- ✅ Monitor multiple products simultaneously
- ✅ Automatic website opening when stock is available
- ✅ Audio notifications (Windows beep)
- ✅ Configurable check intervals
- ✅ Cross-platform support (Windows, Linux, macOS)
- ✅ Type-safe C# implementation
- ✅ Modern async/await patterns

## Technical Details

### Dependencies

- **System.CommandLine** (v2.0.0-beta4) - Command-line parsing
- **System.Text.Json** (v8.0.0) - JSON serialization
- **Microsoft.Extensions.Http** (v8.0.0) - HTTP client factory

### API Communication

The application uses `HttpClient` to communicate with:
1. **GraphQL Endpoint** - POST requests with complex queries for modern stores
2. **REST Endpoints** - GET requests to Shopify product JSON for legacy stores

### Error Handling

- Validates store names before making API calls
- Handles HTTP errors gracefully
- Provides warnings for unknown products
- Continues monitoring even if individual requests fail

## Differences from PowerShell Version

1. **Type Safety**: Strongly typed models vs dynamic PowerShell objects
2. **Performance**: Compiled C# code vs interpreted PowerShell
3. **Dependencies**: NuGet packages vs PowerShell modules
4. **Cross-Platform**: Better Linux/macOS support without PowerShell-specific dependencies
5. **Async**: Native async/await throughout vs PowerShell's job-based async

## License

Same license as the original PowerShell version (MIT License)

## Credits

Original PowerShell version by Przemysław Kłys (EvotecIT)
C# port created as a rewrite exercise
