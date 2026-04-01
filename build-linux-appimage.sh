#!/bin/bash

# Build and Create AppImage for ECoopSystem
# This script builds the application for Linux and packages it as an AppImage

set -e

# Configuration
VERSION="${1:-1.0.0}"
IFRAME_URL="${2:-https://e-coop-client-development.up.railway.app/}"
API_URL="${3:-https://e-coop-server-development.up.railway.app/}"
CONFIGURATION="${4:-Release}"
APP_NAME="ECoopSystem"
OUTPUT_DIR="./output/installer"
BUILD_DIR="./build-appimage"
APPDIR="${BUILD_DIR}/AppDir"

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
    echo -e "${GREEN}?${NC} $1"
}

print_error() {
    echo -e "${RED}?${NC} $1"
    exit 1
}

print_info() {
    echo -e "${YELLOW}?${NC} $1"
}

# Main script
print_header "ECoopSystem - AppImage Builder"

echo -e "${YELLOW}Build Configuration:${NC}"
echo "  IFrame URL:      $IFRAME_URL"
echo "  API URL:         $API_URL"
echo "  Configuration:   $CONFIGURATION"
echo "  Version:         $VERSION"
echo ""

if [[ "$API_URL" == *"development"* ]] || [[ "$IFRAME_URL" == *"development"* ]]; then
    echo -e "${YELLOW}WARNING: You are building an installer with DEVELOPMENT URLs!${NC}"
    echo "The installed application will connect to development servers."
    echo ""
fi

# Check for required tools
print_info "Checking for required tools..."

if ! command -v dotnet &> /dev/null; then
    print_error ".NET SDK not found. Please install .NET 9 SDK."
fi
print_success ".NET SDK found"

if ! command -v tar &> /dev/null; then
    print_error "tar command not found"
fi
print_success "tar found"

# Clean previous build
print_info "Cleaning previous builds..."
rm -rf "${BUILD_DIR}"
rm -rf "bin/${CONFIGURATION}/net9.0/linux-x64"
mkdir -p "${OUTPUT_DIR}"
print_success "Clean complete"

# Build the application
print_info "Building ECoopSystem for Linux (net9.0, linux-x64)..."
dotnet publish -c "$CONFIGURATION" -r linux-x64 --self-contained true -p:PublishTrimmed=false -p:IncludeNativeLibrariesForSelfExtract=true -p:IFrameUrl="$IFRAME_URL" -p:ApiUrl="$API_URL"
print_success "Build complete"

# Create AppDir structure
print_info "Creating AppImage directory structure..."
mkdir -p "${APPDIR}/usr/bin"
mkdir -p "${APPDIR}/usr/lib/${APP_NAME}"
mkdir -p "${APPDIR}/usr/share/applications"
mkdir -p "${APPDIR}/usr/share/icons/hicolor/256x256/apps"
mkdir -p "${APPDIR}/usr/share/pixmaps"
mkdir -p "${APPDIR}/usr/share/doc/${APP_NAME}"
print_success "Directory structure created"

# Copy application files
print_info "Copying application files..."
cp -r "bin/${CONFIGURATION}/net9.0/linux-x64/publish/"* "${APPDIR}/usr/lib/${APP_NAME}/"
print_success "Application files copied"

# Create launcher script
print_info "Creating launcher script..."
cat > "${APPDIR}/usr/bin/${APP_NAME}" << 'EOF'
#!/bin/bash
exec "/usr/lib/ECoopSystem/ECoopSystem" "$@"
EOF
chmod +x "${APPDIR}/usr/bin/${APP_NAME}"
print_success "Launcher script created"

# Create AppRun script
print_info "Creating AppRun script..."
cat > "${APPDIR}/AppRun" << 'EOF'
#!/bin/bash
SELF=$(readlink -f "$0")
HERE="${SELF%/*}"
EXEC="${HERE}/usr/lib/ECoopSystem/ECoopSystem"
export LD_LIBRARY_PATH="${HERE}/usr/lib:${HERE}/usr/lib/x86_64-linux-gnu:$LD_LIBRARY_PATH"
export PATH="${HERE}/usr/bin:$PATH"
exec "$EXEC" "$@"
EOF
chmod +x "${APPDIR}/AppRun"
print_success "AppRun script created"

# Create desktop entry
print_info "Creating desktop entry..."
cat > "${APPDIR}/usr/share/applications/${APP_NAME}.desktop" << 'EOF'
[Desktop Entry]
Type=Application
Name=ECoopSystem
Comment=E-Cooperative Management System
Exec=ECoopSystem
Icon=ecoopsystem
Categories=Business;Office;Finance;
Terminal=false
StartupNotify=true
EOF
print_success "Desktop entry created"

# Try to copy icon if it exists
if [ -f "Assets/Icons/ecoopsuite.ico" ]; then
    print_info "Copying icon..."
    cp "Assets/Icons/ecoopsuite.ico" "${APPDIR}/usr/share/pixmaps/ecoopsystem.ico" 2>/dev/null || true
    print_success "Icon copied"
elif [ -f "Assets/Icons/ecoopsuite.png" ]; then
    print_info "Copying icon..."
    cp "Assets/Icons/ecoopsuite.png" "${APPDIR}/usr/share/icons/hicolor/256x256/apps/ecoopsystem.png"
    cp "Assets/Icons/ecoopsuite.png" "${APPDIR}/usr/share/pixmaps/ecoopsystem.png"
    print_success "Icon copied"
else
    print_info "No icon found - skipping"
fi

# Create changelog
print_info "Creating changelog..."
cat > "${APPDIR}/usr/share/doc/${APP_NAME}/changelog" << EOF
# ECoopSystem v${VERSION}

## Release Notes
- Release date: $(date +%Y-%m-%d)
- Built for Linux x86_64
- .NET 9 Self-Contained Deployment

## Configuration
- IFrame URL: ${IFRAME_URL}
- API URL: ${API_URL}
- Configuration: ${CONFIGURATION}

## System Requirements
- Linux Kernel 4.15+
- x86_64 architecture
- 2GB RAM minimum
- 500MB disk space
- X11 or Wayland display server

For more information, visit: https://github.com/Lands-Horizon-Corp/e-coop-system
EOF
print_success "Changelog created"

# Package as tar.gz
print_info "Creating portable tar.gz archive..."
cd "${BUILD_DIR}"
tar -czf "../${OUTPUT_DIR}/ECoopSystem-${VERSION}-linux-x64.tar.gz" AppDir/
cd - > /dev/null
print_success "Tar.gz created: ${OUTPUT_DIR}/ECoopSystem-${VERSION}-linux-x64.tar.gz"

# Attempt to create AppImage if appimagetool is available
if command -v appimagetool &> /dev/null; then
    print_info "Creating AppImage..."
    APPIMAGE_PATH="${OUTPUT_DIR}/ECoopSystem-${VERSION}-x86_64.AppImage"
    
    appimagetool -n "${APPDIR}" "${APPIMAGE_PATH}" 2>/dev/null || {
        print_error "Failed to create AppImage with appimagetool"
    }
    
    chmod +x "${APPIMAGE_PATH}"
    print_success "AppImage created: ${APPIMAGE_PATH}"
else
    print_info "appimagetool not found - AppImage creation skipped"
    print_info "To create AppImage, install appimagetool:"
    print_info "  wget https://github.com/AppImage/AppImageKit/releases/download/continuous/appimagetool-x86_64.AppImage"
    print_info "  chmod +x appimagetool-x86_64.AppImage"
    print_info "  sudo mv appimagetool-x86_64.AppImage /usr/local/bin/appimagetool"
fi

# Summary
print_header "Build Complete"
print_success "ECoopSystem ${VERSION} Linux build complete!"
echo ""
echo "Output files:"
ls -lh "${OUTPUT_DIR}"/ECoopSystem-*
echo ""
echo "Installation instructions:"
echo ""
echo "Option 1: Using tar.gz (Universal)"
echo "  tar -xzf ECoopSystem-${VERSION}-linux-x64.tar.gz"
echo "  chmod +x AppDir/AppRun"
echo "  ./AppDir/AppRun"
echo ""
if [ -f "${OUTPUT_DIR}/ECoopSystem-${VERSION}-x86_64.AppImage" ]; then
    echo "Option 2: Using AppImage (Recommended)"
    echo "  chmod +x ECoopSystem-${VERSION}-x86_64.AppImage"
    echo "  ./ECoopSystem-${VERSION}-x86_64.AppImage"
    echo ""
fi
