# ECoopSystem

A secure, cross-platform desktop application built with Avalonia UI and .NET 9, supporting Windows and Linux.

## ?? Quick Start

### Prerequisites
- .NET 9 SDK or Runtime
- Platform-specific dependencies:
  - **Windows**: Windows 10/11 (x64)
  - **Linux**: See [docs/LINUX.md](docs/LINUX.md) for distribution-specific requirements

### Development Build
```bash
# Clone and build
git clone https://github.com/Lands-Horizon-Corp/e-coop-system.git
cd ECoopSystem
dotnet restore
dotnet run
```

### Production Build
```powershell
# Windows
./build.ps1 -IFrameUrl "https://your-client.com" -ApiUrl "https://your-api.com" -Platform windows

# Linux
make build IFRAME_URL=https://your-client.com API_URL=https://your-api.com PLATFORM=linux
```

## ?? Documentation

| Document | Description |
|----------|-------------|
| **[Build System](docs/BUILD.md)** | Complete build instructions and options |
| **[Configuration](docs/CONFIGURATION.md)** | Settings, configuration system, and security model |
| **[Windows Installer](docs/INSTALLER.md)** | Creating Windows installers with Inno Setup |
| **[Linux Deployment](docs/LINUX.md)** | Linux-specific installation and troubleshooting |
| **[Cross-Platform Dev](docs/QUICK-REFERENCE.md)** | Platform detection and development tips |

## ?? Security Architecture

ECoopSystem uses a **hybrid configuration model** for maximum security:

- ? **Hardcoded Sensitive Settings**: API URLs, security parameters, and trusted domains are **compiled into the binary** at build time
- ? **User Settings Only**: `appsettings.json` contains only non-sensitive UI preferences that users can safely modify
- ? **Encrypted Storage**: License keys and secrets use ASP.NET Data Protection with file system persistence
- ? **SSL/TLS Validation**: Strict certificate validation in production, configurable for development
- ? **Domain Whitelisting**: WebView navigation restricted to build-time defined trusted domains

**Users cannot tamper with security-critical configuration** - it's baked into the executable.

## ??? Architecture

| Component | Technology |
|-----------|------------|
| **Framework** | .NET 9 |
| **UI Framework** | Avalonia UI 11.3.11 |
| **MVVM** | CommunityToolkit.Mvvm 8.4.0 |
| **WebView** | WebViewControl-Avalonia 3.120.11 (CEF) |
| **Configuration** | Microsoft.Extensions.Configuration 10.0.3 |
| **Data Protection** | ASP.NET Core Data Protection 10.0.2 |
| **HTTP Client** | Microsoft.Extensions.Http 10.0.3 |
| **Logging** | Microsoft.Extensions.Logging 10.0.3 |

## ?? Supported Platforms

| Platform | Status | Runtime ID | Notes |
|----------|--------|------------|-------|
| **Windows 10/11 (x64)** | ? Fully Supported | `win-x64` | Primary target |
| **Linux x64** | ? Fully Supported | `linux-x64` | Ubuntu, Debian, Fedora, Arch |
| **Linux ARM64** | ? Supported | `linux-arm64` | Raspberry Pi, ARM servers |
| **macOS Intel** | ?? Planned | `osx-x64` | Future release |
| **macOS ARM (M1/M2)** | ?? Planned | `osx-arm64` | Future release |

## ??? Development

### Building from Source

```bash
# Restore dependencies
dotnet restore

# Build
dotnet build

# Run with hot reload
dotnet watch

# Run tests (if available)
dotnet test
```

### Publishing for Distribution

```bash
# Windows (x64) - Self-contained
dotnet publish -c Release -r win-x64 --self-contained -p:PublishSingleFile=true

# Linux (x64) - Self-contained
dotnet publish -c Release -r linux-x64 --self-contained -p:PublishSingleFile=true

# Framework-dependent (requires .NET runtime)
dotnet publish -c Release -r linux-x64 --no-self-contained
```

**Output location:** `bin/Release/net9.0/<runtime-id>/publish/`

### Custom Builds

Use the build scripts for production deployments with custom configurations:

```powershell
# Production build with all options
./build.ps1 `
    -IFrameUrl "https://app.production.com" `
    -ApiUrl "https://api.production.com" `
    -ApiTimeout 30 `
    -SecurityGracePeriodDays 14 `
    -WebViewTrustedDomains @("production.com", "api.production.com", "cdn.production.com") `
    -Platform windows `
    -Configuration Release
```

See [docs/BUILD.md](docs/BUILD.md) for all available build parameters.

## ?? Configuration

### User-Modifiable Settings (appsettings.json)

Users can customize:
- Application name and version display
- Window dimensions
- Logging preferences

**File Locations:**
- Windows: `%APPDATA%\ECoopSystem\appsettings.json`
- Linux: `~/.config/ECoopSystem/appsettings.json`
- Or: Same directory as executable

### Build-Time Settings (Not User-Modifiable)

The following are **compiled into the binary** and cannot be changed after deployment:
- API server URLs
- WebView client URLs
- Trusted domain whitelist
- Security parameters (grace periods, lockout durations, retry limits)
- HTTP timeouts and size limits

To change these, rebuild with new parameters. See [docs/CONFIGURATION.md](docs/CONFIGURATION.md).

## ??? Data Storage Locations

| Data Type | Windows | Linux |
|-----------|---------|-------|
| **Application Data** | `%APPDATA%\ECoopSystem\` | `~/.config/ECoopSystem/` |
| **Data Protection Keys** | `%APPDATA%\ECoopSystem\dp-keys\` | `~/.config/ECoopSystem/dp-keys/` |
| **Application State** | `%APPDATA%\ECoopSystem\appstate.dat` | `~/.config/ECoopSystem/appstate.dat` |
| **Secret Key (Encrypted)** | `%APPDATA%\ECoopSystem\secret.dat` | `~/.config/ECoopSystem/secret.dat` |
| **Configuration** | Same as executable or AppData | Same as executable or ~/.config |

## ?? Known Limitations

### Linux-Specific
1. **WebView Performance**: CEF initialization may be slower on first run
2. **System Tray**: May have limited support depending on desktop environment  
3. **Hardware Acceleration**: Requires proper graphics drivers for optimal performance
4. **Dependencies**: Requires GTK3, X11/Wayland, and various system libraries

See [docs/LINUX.md](docs/LINUX.md) for detailed Linux setup and troubleshooting.

## ?? Troubleshooting

### Windows
- Ensure .NET 9 runtime is installed
- Check Windows Defender hasn't quarantined the executable
- Run as administrator if file permission issues occur

### Linux
```bash
# Check dependencies
ldd ./ECoopSystem

# Run with debug logging
DOTNET_LOGGING_LEVEL=Debug ./ECoopSystem

# Set executable permission
chmod +x ./ECoopSystem
```

For comprehensive troubleshooting, see:
- [Linux Guide](docs/LINUX.md)
- [Configuration Guide](docs/CONFIGURATION.md)

## Contributing

Contributions are welcome! Please:
1. Fork the repository
2. Create a feature branch
3. Commit your changes
4. Push to the branch
5. Open a Pull Request

## ?? License

This project is licensed under the MIT License - see the [LICENSE.md](LICENSE.md) file for details.

## ?? Organization

Developed and maintained by **Lands Horizon Corporation**

- **Repository**: https://github.com/Lands-Horizon-Corp/e-coop-system
- **Copyright**: ©2026 Lands Horizon

## ?? Support

For issues, questions, or support:
- Open an issue on [GitHub Issues](https://github.com/Lands-Horizon-Corp/e-coop-system/issues)
- Check the [documentation](docs/)
- Review [Linux troubleshooting guide](docs/LINUX.md)

---

**Built with using Avalonia UI and .NET 9**
