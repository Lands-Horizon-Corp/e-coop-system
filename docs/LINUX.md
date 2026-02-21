# Linux Deployment Guide for ECoopSystem

## System Requirements

### Minimum Requirements
- Linux Kernel 4.15+ (for modern .NET features)
- x86_64 architecture
- 2GB RAM minimum
- 500MB disk space
- X11 or Wayland display server

### Recommended Desktop Environments
- Ubuntu 22.04 LTS or later
- Fedora 38+
- Arch Linux (latest)
- Debian 11+
- Linux Mint 21+

## Dependencies Installation

### Ubuntu/Debian
```bash
# Install .NET 9 SDK
wget https://packages.microsoft.com/config/ubuntu/$(lsb_release -rs)/packages-microsoft-prod.deb -O packages-microsoft-prod.deb
sudo dpkg -i packages-microsoft-prod.deb
rm packages-microsoft-prod.deb

sudo apt update
sudo apt install -y dotnet-sdk-9.0

# Install runtime dependencies
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

### Fedora/RHEL/CentOS
```bash
# Install .NET 9 SDK
sudo dnf install -y dotnet-sdk-9.0

# Install runtime dependencies
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
    mesa-libgbm \
    at-spi2-atk
```

### Arch Linux
```bash
# Install .NET 9 SDK
sudo pacman -S dotnet-sdk

# Install runtime dependencies
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
    mesa \
    at-spi2-atk
```

## Building for Linux

### Option 1: Self-Contained (Recommended)
Includes .NET runtime with the app:
```bash
dotnet publish -c Release -r linux-x64 --self-contained -p:PublishSingleFile=true
```

### Option 2: Framework-Dependent
Requires .NET runtime installed on target system:
```bash
dotnet publish -c Release -r linux-x64 --no-self-contained -p:PublishSingleFile=true
```

### Using the Build Script
```bash
chmod +x build-linux.sh
./build-linux.sh
```

## Installation

### Manual Installation
```bash
# Create application directory
sudo mkdir -p /opt/ecoopsystem
sudo cp -r ./publish/linux-x64/* /opt/ecoopsystem/

# Set permissions
sudo chmod +x /opt/ecoopsystem/ECoopSystem

# Create desktop entry (optional)
cat > ~/.local/share/applications/ecoopsystem.desktop << EOF
[Desktop Entry]
Type=Application
Name=ECoopSystem
Exec=/opt/ecoopsystem/ECoopSystem
Icon=/opt/ecoopsystem/Assets/icon.png
Terminal=false
Categories=Utility;Office;
EOF
```

### User-Local Installation
```bash
# Install to user directory
mkdir -p ~/.local/bin/ecoopsystem
cp -r ./publish/linux-x64/* ~/.local/bin/ecoopsystem/
chmod +x ~/.local/bin/ecoopsystem/ECoopSystem

# Add to PATH (add to ~/.bashrc or ~/.zshrc)
export PATH="$HOME/.local/bin/ecoopsystem:$PATH"
```

## Configuration

### Application Data Location
```bash
# Configuration and data
~/.config/ECoopSystem/

# Data protection keys
~/.config/ECoopSystem/dp-keys/

# Application state
~/.config/ECoopSystem/appstate.dat

# Secret key (encrypted)
~/.config/ECoopSystem/secret.dat
```

### Permissions
The application needs:
- Read/Write access to `~/.config/ECoopSystem/`
- Read access to `/etc/machine-id` (for hardware fingerprinting)
- Network access for license validation

## Troubleshooting

### Application Won't Start

#### Check Dependencies
```bash
cd ./publish/linux-x64/
ldd ECoopSystem
```
Look for any "not found" entries.

#### Check Permissions
```bash
ls -la ECoopSystem
# Should show: -rwxr-xr-x (executable)

chmod +x ECoopSystem
```

#### Run with Debug Logging
```bash
DOTNET_LOGGING_LEVEL=Debug ./ECoopSystem
```

### WebView Issues

#### Missing CEF Libraries
```bash
# Check CEF dependencies
ldd libcef.so  # If it exists in your publish directory
```

#### Graphics Acceleration Issues
```bash
# Disable hardware acceleration if needed
export CEFSHARP_DISABLE_GPU=1
./ECoopSystem
```

### Display Server Issues

#### Wayland Compatibility
If running on Wayland:
```bash
# Force X11 mode
export GDK_BACKEND=x11
./ECoopSystem
```

#### Missing Display
```bash
export DISPLAY=:0
./ECoopSystem
```

### SSL/TLS Certificate Issues

#### Trust System Certificates
```bash
# Ubuntu/Debian
sudo update-ca-certificates

# Fedora/RHEL
sudo update-ca-trust
```

### Machine ID Not Found

The application tries to read machine ID from:
1. `/etc/machine-id` (primary)
2. `/var/lib/dbus/machine-id` (fallback)
3. `Environment.MachineName` (last resort)

Check if these files exist:
```bash
cat /etc/machine-id
cat /var/lib/dbus/machine-id
```

### Performance Issues

#### Enable Hardware Acceleration
```bash
# Check graphics driver
lspci | grep -i vga
glxinfo | grep "direct rendering"
```

#### Increase Memory Limit
```bash
# Set environment variable
export DOTNET_GCHeapCount=2
export DOTNET_GCHeapAffinitizeMask=0x3
./ECoopSystem
```

## Firewall Configuration

If using firewall, allow outbound HTTPS connections:
```bash
# UFW (Ubuntu)
sudo ufw allow out 443/tcp

# firewalld (Fedora/RHEL)
sudo firewall-cmd --permanent --add-port=443/tcp
sudo firewall-cmd --reload
```

## Development on Linux

### IDE Options
- **Visual Studio Code** with C# Dev Kit
- **JetBrains Rider**
- **MonoDevelop** (legacy)

### Running in Development
```bash
dotnet run --project ECoopSystem.csproj
```

### Hot Reload
```bash
dotnet watch --project ECoopSystem.csproj
```

## Common Error Messages

### "cannot open shared object file"
**Solution**: Install missing library dependency (see Dependencies Installation above)

### "Permission denied"
**Solution**: `chmod +x ECoopSystem`

### "Display is not set"
**Solution**: `export DISPLAY=:0`

### "Assembly not found"
**Solution**: Use `--self-contained` when publishing

### "Machine ID could not be determined"
**Solution**: Check `/etc/machine-id` exists and is readable

## Security Considerations

### File Permissions
```bash
# Ensure config directory has correct permissions
chmod 700 ~/.config/ECoopSystem/
chmod 600 ~/.config/ECoopSystem/*.dat
```

### SELinux (Fedora/RHEL)
If SELinux is enabled:
```bash
# Check for denials
sudo ausearch -m avc -ts recent

# Allow if needed (replace with actual policy)
sudo semanage fcontext -a -t bin_t "/opt/ecoopsystem/ECoopSystem"
sudo restorecon -v /opt/ecoopsystem/ECoopSystem
```

### AppArmor (Ubuntu)
If AppArmor is enabled and blocking:
```bash
# Check for denials
sudo dmesg | grep -i apparmor

# Create profile if needed
sudo aa-genprof /opt/ecoopsystem/ECoopSystem
```

## Packaging for Distribution

### Creating a .deb Package (Debian/Ubuntu)
```bash
# Structure
mkdir -p ecoopsystem_1.0.0/DEBIAN
mkdir -p ecoopsystem_1.0.0/opt/ecoopsystem
mkdir -p ecoopsystem_1.0.0/usr/share/applications

# Copy files
cp -r ./publish/linux-x64/* ecoopsystem_1.0.0/opt/ecoopsystem/

# Create control file
cat > ecoopsystem_1.0.0/DEBIAN/control << EOF
Package: ecoopsystem
Version: 1.0.0
Architecture: amd64
Maintainer: Your Name <your@email.com>
Description: ECoopSystem Desktop Application
Depends: libgtk-3-0, libnss3, libasound2
EOF

# Build package
dpkg-deb --build ecoopsystem_1.0.0
```

### Creating an RPM Package (Fedora/RHEL)
```bash
# Install rpm-build
sudo dnf install rpm-build

# Create RPM structure
mkdir -p ~/rpmbuild/{BUILD,RPMS,SOURCES,SPECS,SRPMS}

# Create spec file and build
rpmbuild -ba ecoopsystem.spec
```

### Creating an AppImage
```bash
# Download appimagetool
wget https://github.com/AppImage/AppImageKit/releases/download/continuous/appimagetool-x86_64.AppImage
chmod +x appimagetool-x86_64.AppImage

# Create AppDir structure
mkdir -p ECoopSystem.AppDir/usr/bin
cp -r ./publish/linux-x64/* ECoopSystem.AppDir/usr/bin/

# Create AppRun script
cat > ECoopSystem.AppDir/AppRun << 'EOF'
#!/bin/bash
SELF=$(readlink -f "$0")
HERE=${SELF%/*}
export PATH="${HERE}/usr/bin:${PATH}"
exec "${HERE}/usr/bin/ECoopSystem" "$@"
EOF
chmod +x ECoopSystem.AppDir/AppRun

# Build AppImage
./appimagetool-x86_64.AppImage ECoopSystem.AppDir
```

## Support and Resources

- Documentation: [Project README](README.md)
- Issues: https://github.com/BlirrHub/ECoopSystem/issues
- .NET on Linux: https://learn.microsoft.com/en-us/dotnet/core/install/linux
- Avalonia on Linux: https://docs.avaloniaui.net/docs/getting-started/linux
