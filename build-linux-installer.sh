#!/bin/bash

# Build Ubuntu .deb installer for ECoopSystem

set -e

# Configuration
VERSION="${1:-1.0.0}"
IFRAME_URL="${2:-http://localhost:3000/}"
API_URL="${3:-http://localhost:5000}"
CONFIGURATION="${4:-Release}"
APP_NAME="ECoopSystem"
PACKAGE_NAME="ecoopsystem"
OUTPUT_DIR="./output/installer"
BUILD_DIR="./build-deb"
WORK_DIR="${BUILD_DIR}"
PKGROOT="${WORK_DIR}/pkg"

# On WSL mounted Windows drives (/mnt/*), dpkg-deb may fail due to 777 perms.
# Build package structure in Linux filesystem and only write final .deb to project output.
if [[ "$(pwd)" == /mnt/* ]]; then
    WORK_DIR="/tmp/${APP_NAME}-deb-build"
    PKGROOT="${WORK_DIR}/pkg"
fi

# Colors
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m'

# Functions
print_header() {
    echo -e "${BLUE}======================================== ${NC}"
    echo -e "${BLUE}$1${NC}"
    echo -e "${BLUE}======================================== ${NC}"
    echo ""
}

print_success() {
    echo -e "${GREEN}[OK]${NC} $1"
}

print_error() {
    echo -e "${RED}[ERROR]${NC} $1"
    exit 1
}

print_info() {
    echo -e "${YELLOW}[INFO]${NC} $1"
}

# Main script
print_header "ECoopSystem - Ubuntu DEB Builder"

echo -e "${YELLOW}Build Configuration:${NC}"
echo "  IFrame URL:      $IFRAME_URL"
echo "  API URL:         $API_URL"
echo "  Configuration:   $CONFIGURATION"
echo "  Version:         $VERSION"
echo ""

if [ -f ".env" ]; then
    cp ".env" ".env.build.bak"
fi
{
    echo "IFRAME_URL=$IFRAME_URL"
    echo "API_URL=$API_URL"
} > ".env"

# Check for required tools
print_info "Checking for required tools..."

DOTNET_CMD=""
if command -v dotnet &> /dev/null; then
    DOTNET_CMD="dotnet"
elif [ -x "$HOME/.dotnet/dotnet" ]; then
    DOTNET_CMD="$HOME/.dotnet/dotnet"
fi

if [ -z "$DOTNET_CMD" ]; then
    print_error ".NET SDK not found. Install dotnet-sdk (9/10) or use dotnet-install script."
fi
print_success ".NET SDK found: $DOTNET_CMD"

if ! command -v dpkg-deb &> /dev/null; then
    print_error "dpkg-deb not found. Install with: sudo apt install dpkg-dev"
fi
print_success "dpkg-deb found"

# Clean previous build
print_info "Cleaning previous builds..."
rm -rf "${WORK_DIR}"
mkdir -p "${OUTPUT_DIR}"
print_success "Clean complete"

# Build the application
print_info "Building ECoopSystem for Linux (net9.0, linux-x64)..."
"$DOTNET_CMD" publish -c "$CONFIGURATION" -r linux-x64 --self-contained true -p:PublishTrimmed=false -p:IncludeNativeLibrariesForSelfExtract=true
print_success "Build complete"

# Create Debian package structure
print_info "Creating Debian package structure..."
mkdir -p "${PKGROOT}/DEBIAN"
mkdir -p "${PKGROOT}/opt/${APP_NAME}"
mkdir -p "${PKGROOT}/usr/bin"
mkdir -p "${PKGROOT}/usr/share/applications"
mkdir -p "${PKGROOT}/usr/share/pixmaps"
chmod 0755 "${PKGROOT}/DEBIAN"
print_success "Package structure created"

# Copy app files
print_info "Copying application files..."
cp -r "bin/${CONFIGURATION}/net9.0/linux-x64/publish/"* "${PKGROOT}/opt/${APP_NAME}/"
print_success "Application files copied"

# Launcher
print_info "Creating launcher..."
cat > "${PKGROOT}/usr/bin/ecoopsystem" << EOF
#!/bin/bash
exec /opt/${APP_NAME}/${APP_NAME} "\$@"
EOF
chmod +x "${PKGROOT}/usr/bin/ecoopsystem"
print_success "Launcher created"

# Desktop entry
print_info "Creating desktop entry..."
cat > "${PKGROOT}/usr/share/applications/ecoopsystem.desktop" << EOF
[Desktop Entry]
Type=Application
Name=ECoopSystem
Comment=E-Cooperative Management System
Exec=ecoopsystem
Icon=ecoopsystem
Categories=Business;Office;Finance;
Terminal=false
StartupNotify=true
EOF
print_success "Desktop entry created"

# Icon
if [ -f "Assets/Icons/ecoopsuite.png" ]; then
    cp "Assets/Icons/ecoopsuite.png" "${PKGROOT}/usr/share/pixmaps/ecoopsystem.png"
elif [ -f "Assets/Icons/ecoopsuite.ico" ]; then
    cp "Assets/Icons/ecoopsuite.ico" "${PKGROOT}/usr/share/pixmaps/ecoopsystem.ico" 2>/dev/null || true
fi

# Control file
print_info "Creating control file..."
cat > "${PKGROOT}/DEBIAN/control" << EOF
Package: ${PACKAGE_NAME}
Version: ${VERSION}
Section: utils
Priority: optional
Architecture: amd64
Maintainer: Lands Horizon <support@landshorizon.com>
Depends: libgtk-3-0, libnss3, libasound2, libxss1
Description: ECoopSystem desktop application
EOF
chmod 0644 "${PKGROOT}/DEBIAN/control"
print_success "Control file created"

# Build .deb
print_info "Building .deb package..."
DEB_PATH="${OUTPUT_DIR}/${PACKAGE_NAME}_${VERSION}_amd64.deb"
dpkg-deb --build "${PKGROOT}" "${DEB_PATH}" >/dev/null
print_success ".deb created: ${DEB_PATH}"

if [ -f ".env.build.bak" ]; then
    mv -f ".env.build.bak" ".env"
else
    rm -f ".env"
fi

# Summary
print_header "Build Complete"
print_success "ECoopSystem ${VERSION} Ubuntu .deb build complete!"
echo ""
echo "Output files:"
ls -lh "${OUTPUT_DIR}"/*.deb
echo ""
echo "Installation instructions (Ubuntu):"
echo ""
echo "  sudo apt install ./${PACKAGE_NAME}_${VERSION}_amd64.deb"
echo "  ecoopsystem"
