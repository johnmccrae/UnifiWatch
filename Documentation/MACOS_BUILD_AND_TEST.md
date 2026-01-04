# Building and Testing on macOS

This guide walks you through building and testing UnifiStockTracker on a Mac (Intel or Apple Silicon).

## Prerequisites

- macOS 10.15 or later
- .NET 9.0 SDK or later
- Terminal access

## Step 1: Install .NET 9.0 SDK

### Option A: Download from Microsoft (Recommended)

1. Visit: https://dotnet.microsoft.com/download/dotnet/9.0
2. Select the appropriate version:
   - **Intel Mac**: Download "Installer" for `osx-x64`
   - **Apple Silicon (M1/M2/M3/M4)**: Download "Installer" for `osx-arm64`
3. Run the installer and follow the prompts
4. Restart Terminal if it was open

### Option B: Install via Homebrew

If you have Homebrew installed:

```bash
brew install dotnet
```

### Verify Installation

```bash
dotnet --version
```

You should see `9.0.x` or later.

## Step 2: Get the Source Code

### Option A: Clone from GitHub (if available)

```bash
git clone https://github.com/EvotecIT/UnifiStockTracker-CSharp.git
cd UnifiStockTracker-CSharp
```

### Option B: Copy from Windows Machine

- Use AirDrop, email, USB drive, or network share to transfer the `UnifiStockTracker-CSharp` folder
- Extract to your preferred location (e.g., `~/Projects/UnifiStockTracker-CSharp`)

## Step 3: Build the Application

Open Terminal and navigate to the project:

```bash
cd ~/path/to/UnifiStockTracker-CSharp
```

**For Apple Silicon (M1/M2/M3/M4):**

```bash
dotnet publish UnifiStockTracker.csproj -c Release -r osx-arm64 --self-contained -p:PublishSingleFile=true
```

**For Intel Mac:**

```bash
dotnet publish UnifiStockTracker.csproj -c Release -r osx-x64 --self-contained -p:PublishSingleFile=true
```

This will:
- Download dependencies
- Compile the application
- Create a standalone executable (no .NET runtime needed on other machines)
- Output the file to: `bin/Release/net9.0/osx-arm64/publish/UnifiStockTracker` or `bin/Release/net9.0/osx-x64/publish/UnifiStockTracker`

Build time: ~2-3 minutes on first build, ~10-20 seconds on subsequent builds.

## Step 4: Make the Executable

```bash
chmod +x bin/Release/net9.0/osx-arm64/publish/UnifiStockTracker
```

(Use `osx-x64` instead if you're on Intel Mac)

## Step 5: Test the Application

### Quick Test - Check Stock

```bash
./bin/Release/net9.0/osx-arm64/publish/UnifiStockTracker --stock --store USA --product-names "Dream Machine" | head -30
```

Expected output:
- `Getting Unifi products from USA store...`
- `Retrieved XXX products`
- Table of products with stock status

### Monitor Mode Test

To test the monitoring functionality:

```bash
./bin/Release/net9.0/osx-arm64/publish/UnifiStockTracker --wait --store USA --product-names "Dream Machine" --seconds 5
```

This will:
- Check for the product every 5 seconds
- Display the current stock status
- Press Ctrl+C to stop

### Test All Stores

**GraphQL API Stores (Modern):**

```bash
# USA
./bin/Release/net9.0/osx-arm64/publish/UnifiStockTracker --stock --store USA

# Europe
./bin/Release/net9.0/osx-arm64/publish/UnifiStockTracker --stock --store Europe

# UK
./bin/Release/net9.0/osx-arm64/publish/UnifiStockTracker --stock --store UK
```

**Shopify API Stores (Legacy):**

```bash
# Brazil
./bin/Release/net9.0/osx-arm64/publish/UnifiStockTracker --stock --legacy-api-store Brazil

# Japan
./bin/Release/net9.0/osx-arm64/publish/UnifiStockTracker --stock --legacy-api-store Japan
```

## Step 6: Test Notifications

When stock is found, macOS native notifications should appear in Notification Center.

To manually test notifications, create a file with the test code:

```bash
cat > /tmp/test_notification.sh << 'EOF'
osascript -e 'display notification "Dream Machine Pro is now in stock!" with title "UniFi Stock Alert" subtitle "Ubiquiti Stock Tracker"'
EOF

chmod +x /tmp/test_notification.sh
/tmp/test_notification.sh
```

You should see a notification in the top-right corner of your screen.

## Step 7: Run Unit Tests

To run the full test suite:

```bash
sudo dotnet test UnifiStockTracker-CSharp.sln
```

Expected output:
- `Test summary: total: 35, failed: 0, succeeded: 29, skipped: 6`
- All tests should pass

## Step 8: (Optional) Install Globally

To run the application from anywhere, copy it to a location in your PATH:

```bash
# Option 1: Copy to /usr/local/bin
sudo cp bin/Release/net9.0/osx-arm64/publish/UnifiStockTracker /usr/local/bin/

# Option 2: Create a symlink
sudo ln -s $(pwd)/bin/Release/net9.0/osx-arm64/publish/UnifiStockTracker /usr/local/bin/

# Now you can run it from anywhere:
UnifiStockTracker --stock --store USA
```

## Troubleshooting

### "command not found: dotnet"
- .NET is not installed or Terminal needs to be restarted
- Try: `brew install dotnet` or reinstall from Microsoft

### "xcrun: error: unable to find utility"
- Xcode Command Line Tools are missing
- Run: `xcode-select --install`

### "Permission denied"
- The executable isn't marked as executable
- Run: `chmod +x bin/Release/net9.0/osx-arm64/publish/UnifiStockTracker`

### "No notifications appearing"
- Check System Preferences > Notifications
- Ensure notifications are enabled for "Terminal" or your shell
- Notifications work with native mode (`--wait`) when stock is found

### Application hangs
- Press Ctrl+C to stop
- Check your internet connection
- Verify the store name is correct (case-sensitive)

## Common Commands

```bash
# Check stock in USA
./bin/Release/net9.0/osx-arm64/publish/UnifiStockTracker --stock --store USA

# Monitor for specific product
./bin/Release/net9.0/osx-arm64/publish/UnifiStockTracker --wait --store USA --product-names "Dream Machine"

# Monitor by SKU
./bin/Release/net9.0/osx-arm64/publish/UnifiStockTracker --wait --store USA --product-skus "UDM-Pro"

# Check with custom interval (check every 30 seconds)
./bin/Release/net9.0/osx-arm64/publish/UnifiStockTracker --wait --store USA --seconds 30

# Silent mode (no notifications, no browser)
./bin/Release/net9.0/osx-arm64/publish/UnifiStockTracker --wait --store USA --no-website --no-sound
```

## What to Verify

After building and testing, confirm:

- ✅ Build completes without errors
- ✅ `--stock` mode retrieves products from the store
- ✅ Product filtering works (`--product-names`, `--product-skus`)
- ✅ `--wait` mode monitors and counts down
- ✅ Native macOS notifications appear when monitored (if products were in stock)
- ✅ Ctrl+C stops the monitoring gracefully
- ✅ Unit tests pass (`sudo dotnet test UnifiStockTracker-CSharp.sln`)

## Performance Notes

- **First build**: 2-3 minutes (downloads dependencies)
- **Subsequent builds**: 10-20 seconds
- **Runtime**: ~100-200ms for a single stock check
- **Executable size**: ~50-60 MB (includes .NET runtime)
- **Memory usage**: ~40-50 MB when running

## Getting Help

If you encounter issues:

1. Check the [Troubleshooting](#troubleshooting) section above
2. Verify .NET 9.0 is installed: `dotnet --version`
3. Ensure you're on the correct Mac architecture (Intel vs Apple Silicon)
4. Run `sudo dotnet test UnifiStockTracker-CSharp.sln` to check if the test suite passes
5. Check internet connectivity when running API calls
