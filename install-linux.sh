#!/bin/bash

# UnifiWatch Linux Installation Script
# Installs UnifiWatch as a systemd service
# Usage: sudo ./install-linux.sh [--binary-path /path/to/binary] [--user unifiwatch]

set -e

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
NC='\033[0m' # No Color

# Configuration
BINARY_PATH="${1:-.}"
SERVICE_USER="${2:-unifiwatch}"
SERVICE_NAME="unifiwatch"
INSTALL_DIR="/opt/unifiwatch"
CONFIG_DIR="/etc/unifiwatch"
STATE_DIR="/var/lib/unifiwatch"

# Check if running as root
if [[ $EUID -ne 0 ]]; then
   echo -e "${RED}This script must be run as root (use sudo)${NC}"
   exit 1
fi

echo "========================================"
echo "UnifiWatch Linux Installation"
echo "========================================"

# Validate binary exists
if [ ! -f "$BINARY_PATH/UnifiWatch" ]; then
    echo -e "${RED}✗ UnifiWatch binary not found at: $BINARY_PATH/UnifiWatch${NC}"
    exit 1
fi

echo -e "${GREEN}✓ Found UnifiWatch binary${NC}"

# Create service user
if ! id "$SERVICE_USER" &>/dev/null; then
    echo "Creating system user: $SERVICE_USER"
    useradd --system --home /var/lib/unifiwatch --shell /usr/sbin/nologin $SERVICE_USER
    echo -e "${GREEN}✓ User created${NC}"
else
    echo -e "${GREEN}✓ User $SERVICE_USER already exists${NC}"
fi

# Create installation directory
echo "Creating installation directory: $INSTALL_DIR"
mkdir -p "$INSTALL_DIR"
cp "$BINARY_PATH/UnifiWatch" "$INSTALL_DIR/"
chmod 755 "$INSTALL_DIR/UnifiWatch"
echo -e "${GREEN}✓ Binary installed${NC}"

# Create config directory
echo "Creating configuration directory: $CONFIG_DIR"
mkdir -p "$CONFIG_DIR"
chmod 750 "$CONFIG_DIR"
chown "$SERVICE_USER:$SERVICE_USER" "$CONFIG_DIR"
echo -e "${GREEN}✓ Config directory created${NC}"

# Create state directory
echo "Creating state directory: $STATE_DIR"
mkdir -p "$STATE_DIR"
chmod 750 "$STATE_DIR"
chown "$SERVICE_USER:$SERVICE_USER" "$STATE_DIR"
echo -e "${GREEN}✓ State directory created${NC}"

# Install systemd service
echo "Installing systemd service"
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
if [ -f "$SCRIPT_DIR/unifiwatch.service" ]; then
    cp "$SCRIPT_DIR/unifiwatch.service" "/etc/systemd/system/$SERVICE_NAME.service"
    sed -i "s|/opt/unifiwatch|$INSTALL_DIR|g" "/etc/systemd/system/$SERVICE_NAME.service"
    sed -i "s|^User=unifiwatch|User=$SERVICE_USER|g" "/etc/systemd/system/$SERVICE_NAME.service"
    chmod 644 "/etc/systemd/system/$SERVICE_NAME.service"
    systemctl daemon-reload
    echo -e "${GREEN}✓ Service file installed${NC}"
else
    echo -e "${YELLOW}⚠ unifiwatch.service not found in script directory${NC}"
    echo "Please ensure unifiwatch.service is in the same directory as this script"
fi

# Enable and start service
echo "Enabling service..."
systemctl enable "$SERVICE_NAME.service"
echo -e "${GREEN}✓ Service enabled${NC}"

# Optionally start service
read -p "Start UnifiWatch service now? (y/n) " -n 1 -r
echo
if [[ $REPLY =~ ^[Yy]$ ]]; then
    systemctl start "$SERVICE_NAME.service"
    sleep 2
    if systemctl is-active --quiet "$SERVICE_NAME.service"; then
        echo -e "${GREEN}✓ Service started successfully${NC}"
    else
        echo -e "${YELLOW}⚠ Service created but failed to start${NC}"
        echo "Check logs: journalctl -u $SERVICE_NAME -n 20"
    fi
fi

echo ""
echo "========================================"
echo "Installation Summary"
echo "========================================"
echo "Service Name:        $SERVICE_NAME"
echo "Binary Location:     $INSTALL_DIR/UnifiWatch"
echo "Config Directory:    $CONFIG_DIR"
echo "State Directory:     $STATE_DIR"
echo "Service User:        $SERVICE_USER"
echo ""
echo "Next steps:"
echo "1. Configure the service:"
echo "   sudo -u $SERVICE_USER UnifiWatch configure --config-dir $CONFIG_DIR"
echo "2. Test notifications:"
echo "   sudo -u $SERVICE_USER UnifiWatch test-notifications --config-dir $CONFIG_DIR"
echo "3. Check service status:"
echo "   systemctl status $SERVICE_NAME"
echo "4. View logs:"
echo "   journalctl -u $SERVICE_NAME -f"
echo "========================================"
