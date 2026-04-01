# Linux Installer Guide for ECoopSystem

This guide explains how to create and distribute Linux installers for ECoopSystem using AppImage and other formats.

## Overview

ECoopSystem supports multiple Linux distribution formats:

| Format | Best For | Complexity | File Size |
|--------|----------|-----------|-----------|
| **AppImage** | Single file distribution (recommended) | Low | ~200-300 MB |
| **tar.gz** | Manual installation, portable | Very Low | ~180-250 MB |
| **.deb** | Ubuntu/Debian systems | Medium | ~150-200 MB |
| **.rpm** | Fedora/RHEL systems | Medium | ~150-200 MB |

**For sending a single installer to a Linux user, use AppImage.**

---

## Quick Start

### Building AppImage (Recommended)

#### On Linux
```bash
chmod +x build-linux-appimage.sh
./build-linux-appimage.sh 1.0.0
```

#### On Windows (with WSL)
```powershell
.\build-linux-appimage.ps1 -Version 1.0.0
```

#### Output
```
output/installer/
ECoopSystem-1.0.0-linux-x64.tar.gz    (Universal portable)
ECoopSystem-1.0.0-x86_64.AppImage     (Single-file executable)
```

---

## Installation Methods for Users

### Option 1: AppImage (Easiest)

User receives: `ECoopSystem-1.0.0-x86_64.AppImage`

**Installation:**
```bash
# Make executable
chmod +x ECoopSystem-1.0.0-x86_64.AppImage

# Run
./ECoopSystem-1.0.0-x86_64.AppImage
```

**Features:**
- Works on any Linux distribution
- No installation required
- Self-contained with all dependencies
- Can be placed anywhere
- Integrates with desktop (optional)

### Option 2: Portable tar.gz

User receives: `ECoopSystem-1.0.0-linux-x64.tar.gz`

**Installation:**
```bash
# Extract
tar -xzf ECoopSystem-1.0.0-linux-x64.tar.gz

# Run
./AppDir/AppRun
```

---

## System Requirements

### Minimum
- Linux Kernel 4.15+ (for modern .NET features)
- x86_64 architecture
- 2GB RAM
- 500MB disk space
- X11 or Wayland display server

### Recommended Distributions
- Ubuntu 22.04 LTS or later
- Ubuntu 24.04 LTS
- Debian 11 or later
- Fedora 38+
- Linux Mint 21+
- Arch Linux (latest)

### Dependencies

AppImage and tar.gz are self-contained and include the .NET 9 runtime. However, some system libraries are required:

**Ubuntu/Debian:**
```bash
sudo apt update
sudo apt install -y \
    libx11-6 \
    libxext6 \
    libxrender1 \
    libxrandr2 \
    libxi6 \
    libxcursor1 \
    libxdamage1 \
    libxfixes3 \
    libxcomposite1 \
    libgtk-3-0 \
    libnss3 \
    libnspr4 \
    libasound2 \
    libatk1.0-0 \
    libcups2 \
    libdrm2 \
    libgbm1 \
    libatspi2.0-0
```

**Fedora/RHEL:**
```bash
sudo dnf install -y \
    libX11 \
    libXext \
    libXrender \
    libXrandr \
    libXi \
    libXcursor \
    libXdamage \
    libXfixes \
    libXcomposite \
    gtk3 \
    nss \
    nspr \
    alsa-lib \
    atk \
    cups-libs \
    libdrm \
    libgbm \
    at-spi2-atk
```

**Arch Linux:**
```bash
sudo pacman -S \
    libx11 \
    libxext \
    libxrender \
    libxrandr \
    libxi \
    libxcursor \
    libxdamage \
    libxfixes \
    libxcomposite \
    gtk3 \
    nss \
    nspr \
    alsa-lib \
    atk \
    libcups \
    libdrm \
    libgbm \
    at-spi2-core
```

---

## Build Configuration

### Build Scripts

#### Bash Script (Linux/macOS)
```bash
./build-linux-appimage.sh [VERSION]

# Examples:
./build-linux-appimage.sh              # Default: 1.0.0
./build-linux-appimage.sh 1.0.1
./build-linux-appimage.sh 2.0.0-beta
```

#### PowerShell Script (Windows with WSL/Linux)
```powershell
.\build-linux-appimage.ps1 -Version 1.0.0
.\build-linux-appimage.ps1 -Version 1.0.0 -SkipBuild
.\build-linux-appimage.ps1 -Version 1.0.0 -OpenOutput
```

### Build Process

The build script performs these steps:

1. **Verify Prerequisites**
   - Check for .NET 9 SDK
   - Check for tar command

2. **Clean Previous Build**
   - Remove old build artifacts
   - Create output directory

3. **Build Application**
   - `dotnet publish -c Release -r linux-x64`
   - Self-contained deployment with all dependencies

4. **Create AppImage Structure**
   - `/usr/lib/ECoopSystem` - Application files
   - `/usr/bin/ECoopSystem` - Launcher script
   - `/usr/share/applications/` - Desktop entry
   - `/usr/share/icons/` - Application icon
   - Desktop metadata and documentation

5. **Create AppRun Script**
   - Sets up environment variables
   - Handles library paths
   - Executes main application

6. **Create Desktop Entry**
   - Integrates with desktop environments
   - Shows in application menus
   - Enables desktop shortcuts

7. **Package Files**
   - Create tar.gz archive (universal)
   - Create AppImage (if appimagetool available)

---

## Creating Additional Formats

### Creating .deb Package

If you need Ubuntu/Debian-specific installers, use `fpm`:

```bash
# Install fpm
sudo apt-get install ruby ruby-dev build-essential
sudo gem install fpm

# Build
dotnet publish -c Release -r linux-x64 --self-contained true

# Create .deb
fpm -s dir \
    -t deb \
    -n ecoopsystem \
    -v 1.0.0 \
    -C bin/Release/net9.0/linux-x64/publish \
    --prefix /opt/ecoopsystem \
    -p output/installer/ecoopsystem_VERSION_amd64.deb \
    .

# Install
sudo dpkg -i output/installer/ecoopsystem_1.0.0_amd64.deb
```

### Creating .rpm Package

For Fedora/RHEL systems:

```bash
# Create .rpm
fpm -s dir \
    -t rpm \
    -n ecoopsystem \
    -v 1.0.0 \
    -C bin/Release/net9.0/linux-x64/publish \
    --prefix /opt/ecoopsystem \
    -p output/installer/ecoopsystem-VERSION.x86_64.rpm \
    .

# Install
sudo rpm -i output/installer/ecoopsystem-1.0.0.x86_64.rpm
```

---

## Customization

### Changing Application Icon

Place a PNG icon at `Assets/Icons/ecoopsuite.png`:
```bash
# Build script will automatically use it
./build-linux-appimage.sh 1.0.0
```

### Modifying Desktop Entry

Edit the desktop entry creation section in the build script:

```bash
# In build-linux-appimage.sh
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
```

### Setting Environment Variables

Modify the `AppRun` script section:

```bash
# In build-linux-appimage.sh
cat > "${APPDIR}/AppRun" << 'EOF'
#!/bin/bash
SELF=$(readlink -f "$0")
HERE="${SELF%/*}"
EXEC="${HERE}/usr/lib/ECoopSystem/ECoopSystem"

# Custom environment variables
export DOTNET_ENVIRONMENT=Production
export LOG_LEVEL=Information
export LD_LIBRARY_PATH="${HERE}/usr/lib:${HERE}/usr/lib/x86_64-linux-gnu:$LD_LIBRARY_PATH"
export PATH="${HERE}/usr/bin:$PATH"

exec "$EXEC" "$@"
EOF
```

---

## Troubleshooting

### AppImage Won't Run

**Permission Error:**
```bash
chmod +x ECoopSystem-1.0.0-x86_64.AppImage
./ECoopSystem-1.0.0-x86_64.AppImage
```

**Missing Dependencies:**
```bash
# Install required libraries (see System Requirements section)
sudo apt install libgtk-3-0 libnss3 libxss1 libasound2
```

**FUSE Error:**
```bash
# If FUSE is not available, extract and run directly
./ECoopSystem-1.0.0-x86_64.AppImage --appimage-extract
./squashfs-root/AppRun
```

### Build Fails

**dotnet not found:**
```bash
# Install .NET 9 SDK
wget https://dot.net/v1/dotnet-install.sh -O dotnet-install.sh
chmod +x ./dotnet-install.sh
./dotnet-install.sh --channel 9.0
export PATH="$HOME/.dotnet:$PATH"
```

**Permission denied:**
```bash
# Make build scripts executable
chmod +x build-linux-appimage.sh
chmod +x build-linux-appimage.ps1
```

---

## Distribution

### Checksums

Generate checksums for verification:

```bash
# SHA256
sha256sum output/installer/ECoopSystem-*.AppImage

# MD5 (optional)
md5sum output/installer/ECoopSystem-*.AppImage
```

### File Transfer

**Using a file hosting service:**
- Upload AppImage to GitHub Releases
- Use cloud storage (Dropbox, Google Drive, etc.)
- Host on your website

**Include with downloads:**
```
Release v1.0.0
??? ECoopSystem-1.0.0-win-x64.exe           (Windows)
??? ECoopSystem-1.0.0-x86_64.AppImage       (Linux)
??? SHA256SUMS                               (Checksums)
??? README.md                                (Instructions)
```

---

## Desktop Integration

### Permanent Installation (System-wide)

Users can install system-wide:

```bash
# Extract AppImage
./ECoopSystem-1.0.0-x86_64.AppImage --appimage-extract

# Install
sudo mkdir -p /opt/ecoopsystem
sudo cp -r squashfs-root/* /opt/ecoopsystem/

# Create symlink
sudo ln -s /opt/ecoopsystem/usr/bin/ECoopSystem /usr/local/bin/ecoopsystem

# Update desktop database
sudo update-desktop-database /usr/share/applications

# Run from anywhere
ecoopsystem
```

### Uninstallation

```bash
# Remove from PATH
sudo rm /usr/local/bin/ecoopsystem

# Remove installation
sudo rm -rf /opt/ecoopsystem

# Clean cache
rm -rf ~/.cache/ecoopsystem
```

---

## CI/CD Integration

### GitHub Actions Workflow

```yaml
name: Build Linux Installers

on:
  push:
    tags:
      - 'v*'

jobs:
  build-linux:
    runs-on: ubuntu-latest
    
    steps:
      - uses: actions/checkout@v3
      
      - name: Setup .NET
        uses: actions/setup-dotnet@v3
        with:
          dotnet-version: '9.0.x'
      
      - name: Install AppImage Tools
        run: |
          wget https://github.com/AppImage/AppImageKit/releases/download/continuous/appimagetool-x86_64.AppImage
          chmod +x appimagetool-x86_64.AppImage
          sudo mv appimagetool-x86_64.AppImage /usr/local/bin/appimagetool
      
      - name: Build Installers
        run: bash build-linux-appimage.sh ${{ github.ref_name }}
      
      - name: Generate Checksums
        run: |
          cd output/installer
          sha256sum * > SHA256SUMS
          cat SHA256SUMS
      
      - name: Upload Release Assets
        uses: softprops/action-gh-release@v1
        with:
          files: |
            output/installer/ECoopSystem-*
            output/installer/SHA256SUMS
        env:
          GITHUB_TOKEN: ${{ secrets.GITHUB_TOKEN }}
```

---

## Additional Resources

- **Avalonia on Linux**: https://docs.avaloniaui.net/docs/getting-started/test-drive/linux
- **.NET on Linux**: https://learn.microsoft.com/en-us/dotnet/core/install/linux
- **AppImage Documentation**: https://docs.appimage.org/
- **Desktop Entry Specification**: https://specifications.freedesktop.org/desktop-entry-spec/

---

## Support

For issues or questions:
- Check the [main README](../README.md)
- Review [build documentation](BUILD.md)
- Open an issue on [GitHub](https://github.com/Lands-Horizon-Corp/e-coop-system/issues)

---

**Note**: Always test installers on target systems before distributing to end users.
