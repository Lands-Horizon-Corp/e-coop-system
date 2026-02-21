# ECoopSystem

A cross-platform desktop application built with Avalonia UI and .NET 9, supporting Windows and Linux.

## Supported Platforms

- ? **Windows** (x64)
- ? **Linux** (x64)
- ?? **macOS** (planned for future release)

## Prerequisites

### Windows
- .NET 9 SDK or Runtime
- Windows 10/11 (x64)

### Linux
- .NET 9 SDK or Runtime
- X11 or Wayland display server
- GTK3 (usually pre-installed)
- libX11, libXext (for Avalonia)
- CEF/Chromium dependencies for WebView (if using WebViewControl)

#### Ubuntu/Debian Dependencies
```bash
sudo apt update
sudo apt install -y dotnet-sdk-9.0 libx11-6 libxext6 libgtk-3-0
```

#### Fedora/RHEL Dependencies
```bash
sudo dnf install -y dotnet-sdk-9.0 libX11 libXext gtk3
```

#### Arch Linux Dependencies
```bash
sudo pacman -S dotnet-sdk libx11 libxext gtk3
```

## Building from Source

### Development Build
```bash
# Clone the repository
git clone https://github.com/BlirrHub/ECoopSystem.git
cd ECoopSystem

# Restore dependencies
dotnet restore

# Build the project
dotnet build
```

### Run in Development Mode
```bash
dotnet run
```

## Publishing for Distribution

### Windows (x64)
```bash
dotnet publish -c Release -r win-x64 --self-contained -p:PublishSingleFile=true
```
Output: `bin/Release/net9.0/win-x64/publish/`

### Linux (x64)
```bash
dotnet publish -c Release -r linux-x64 --self-contained -p:PublishSingleFile=true
```
Output: `bin/Release/net9.0/linux-x64/publish/`

### Platform-Specific Notes

#### Linux Execution
After publishing, you may need to make the binary executable:
```bash
chmod +x ./bin/Release/net9.0/linux-x64/publish/ECoopSystem
./bin/Release/net9.0/linux-x64/publish/ECoopSystem
```

## Configuration

The application uses configuration files:
- `appsettings.json` - Base configuration
- `appsettings.Development.json` - Development environment settings
- `appsettings.Production.json` - Production environment settings

### Data Storage Locations

#### Windows
- Application Data: `%APPDATA%\ECoopSystem\`
- Data Protection Keys: `%APPDATA%\ECoopSystem\dp-keys\`

#### Linux
- Application Data: `~/.config/ECoopSystem/`
- Data Protection Keys: `~/.config/ECoopSystem/dp-keys/`

## Architecture

- **Framework**: .NET 9
- **UI**: Avalonia UI 11.3.11
- **MVVM**: CommunityToolkit.Mvvm
- **Data Protection**: ASP.NET Core Data Protection
- **WebView**: WebViewControl-Avalonia (CEF-based)

## Cross-Platform Compatibility

The application uses platform-specific code where necessary:

- **Machine ID Detection**:
  - Windows: Uses Registry (`HKLM\SOFTWARE\Microsoft\Cryptography\MachineGuid`)
  - Linux: Uses `/etc/machine-id` with fallback to `/var/lib/dbus/machine-id`

- **File Paths**: Cross-platform paths using `Path.Combine()` and `Environment.SpecialFolder`

- **URL Opening**:
  - Windows: Uses `Process.Start()` with `UseShellExecute = true`
  - Linux: Uses `xdg-open` command

## Known Limitations

### Linux-Specific
1. **WebView Performance**: CEF initialization may be slower on first run
2. **System Tray**: May have limited support depending on desktop environment
3. **Hardware Acceleration**: Requires proper graphics drivers for optimal performance

## Troubleshooting

### Linux: Application won't start
```bash
# Check if all dependencies are installed
ldd ./ECoopSystem

# Run with verbose logging
DOTNET_LOGGING_LEVEL=Debug ./ECoopSystem
```

### Linux: WebView not loading
Ensure CEF runtime libraries are present in the application directory or system paths.

### Linux: Permission denied
```bash
chmod +x ./ECoopSystem
```

## Development

### Debug Mode
The application includes debug logging in development builds:
- SSL certificate warnings
- WebView navigation validation
- Machine ID detection fallbacks

### Building for Multiple Platforms
You can build for all supported platforms:
```bash
# Windows
dotnet publish -c Release -r win-x64 --self-contained

# Linux
dotnet publish -c Release -r linux-x64 --self-contained
```

## License Activation

The application requires license activation on first run. The license is validated against:
- Machine ID (hardware fingerprint)
- Installation ID (unique per installation)
- Timestamp (installation time)

## Security Features

- Data Protection API for sensitive data storage
- SSL/TLS certificate validation
- Trusted domain validation for WebView navigation
- Encrypted secret key storage

## Contributing

Contributions are welcome! Please ensure your code:
- Builds on both Windows and Linux
- Follows existing code style
- Includes appropriate error handling
- Works in both Debug and Release configurations

## Support

For issues and feature requests, please use the GitHub issue tracker:
https://github.com/BlirrHub/ECoopSystem/issues