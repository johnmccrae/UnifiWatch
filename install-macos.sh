#!/bin/bash

# UnifiWatch macOS Installation Script
# Installs UnifiWatch as a launchd daemon
# Usage: ./install-macos.sh [--binary-path /path/to/binary]

set -e

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
NC='\033[0m' # No Color

# Configuration
BINARY_PATH="${1:-.}"
SERVICE_NAME="com.unifiwatch.service"
INSTALL_DIR="/usr/local/bin"
CONFIG_DIR="$HOME/.config/unifiwatch"
LOG_DIR="/var/log/unifiwatch"
PLIST_DIR="$HOME/Library/LaunchAgents"

echo "========================================"
echo "UnifiWatch macOS Installation"
echo "========================================"

# Validate binary exists
if [ ! -f "$BINARY_PATH/UnifiWatch" ]; then
    echo -e "${RED}✗ UnifiWatch binary not found at: $BINARY_PATH/UnifiWatch${NC}"
    exit 1
fi

echo -e "${GREEN}✓ Found UnifiWatch binary${NC}"

# Check for sudo (needed for system-wide installation)
read -p "Install as system service (requires sudo) or user service? (system/user) " -n 1 -r
echo
if [[ $REPLY =~ ^[Ss]$ ]]; then
    INSTALL_AS_SYSTEM=true
    PLIST_DIR="/Library/LaunchDaemons"
else
    INSTALL_AS_SYSTEM=false
fi

# Install binary
echo "Installing UnifiWatch binary to $INSTALL_DIR"
if [ "$INSTALL_AS_SYSTEM" = true ]; then
    sudo cp "$BINARY_PATH/UnifiWatch" "$INSTALL_DIR/"
    sudo chmod 755 "$INSTALL_DIR/UnifiWatch"
else
    cp "$BINARY_PATH/UnifiWatch" "$INSTALL_DIR/"
    chmod 755 "$INSTALL_DIR/UnifiWatch"
fi
echo -e "${GREEN}✓ Binary installed${NC}"

# Create directories
echo "Creating configuration directories"
mkdir -p "$CONFIG_DIR"
mkdir -p "$LOG_DIR"
chmod 700 "$CONFIG_DIR"
echo -e "${GREEN}✓ Directories created${NC}"

# Install launchd plist
echo "Installing launchd configuration"
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"

if [ ! -f "$SCRIPT_DIR/$SERVICE_NAME.plist" ]; then
    echo -e "${RED}✗ $SERVICE_NAME.plist not found in script directory${NC}"
    exit 1
fi

mkdir -p "$PLIST_DIR"

if [ "$INSTALL_AS_SYSTEM" = true ]; then
    sudo cp "$SCRIPT_DIR/$SERVICE_NAME.plist" "/Library/LaunchDaemons/"
    sudo plutil -replace ProgramArguments.0 -string "/usr/local/bin/UnifiWatch" "/Library/LaunchDaemons/$SERVICE_NAME.plist"
    sudo chmod 644 "/Library/LaunchDaemons/$SERVICE_NAME.plist"
    PLIST_PATH="/Library/LaunchDaemons/$SERVICE_NAME.plist"
else
    cp "$SCRIPT_DIR/$SERVICE_NAME.plist" "$PLIST_DIR/"
    plutil -replace ProgramArguments.0 -string "/usr/local/bin/UnifiWatch" "$PLIST_DIR/$SERVICE_NAME.plist"
    chmod 644 "$PLIST_DIR/$SERVICE_NAME.plist"
    PLIST_PATH="$PLIST_DIR/$SERVICE_NAME.plist"
fi

echo -e "${GREEN}✓ Service configuration installed${NC}"

# Load service
echo "Loading launchd service"
if [ "$INSTALL_AS_SYSTEM" = true ]; then
    sudo launchctl load -w "$PLIST_PATH"
    echo -e "${GREEN}✓ System service loaded${NC}"
else
    launchctl load -w "$PLIST_PATH"
    echo -e "${GREEN}✓ User service loaded${NC}"
fi

echo ""
echo "========================================"
echo "Installation Summary"
echo "========================================"
echo "Service Name:        $SERVICE_NAME"
echo "Binary Location:     $INSTALL_DIR/UnifiWatch"
echo "Config Directory:    $CONFIG_DIR"
echo "Log Directory:       $LOG_DIR"
echo "Plist Location:      $PLIST_PATH"
echo "Service Type:        $([ "$INSTALL_AS_SYSTEM" = true ] && echo "System" || echo "User")"
echo ""
echo "Next steps:"
echo "1. Configure the service:"
echo "   UnifiWatch configure"
echo "2. Test notifications:"
echo "   UnifiWatch test-notifications"
echo "3. Check service status:"
echo "   launchctl list | grep unifiwatch"
echo "4. View logs:"
echo "   tail -f $LOG_DIR/output.log"
echo ""
echo "To stop the service:"
echo "   launchctl unload -w \"$PLIST_PATH\""
echo ""
echo "To uninstall:"
echo "   rm \"$PLIST_PATH\""
echo "   rm $INSTALL_DIR/UnifiWatch"
echo "========================================"
