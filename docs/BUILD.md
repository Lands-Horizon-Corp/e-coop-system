# ECoopSystem Build System

This document explains how to build ECoopSystem with custom configurations for different environments and platforms.

## Quick Reference

| Platform | Command |
|----------|---------|
| **Windows** | `./build.ps1 -IFrameUrl "https://example.com" -ApiUrl "https://api.example.com" -Platform windows` |
| **Linux** | `make build IFRAME_URL=https://example.com API_URL=https://api.example.com PLATFORM=linux` |
| **Linux (alt)** | `./build.sh https://example.com https://api.example.com linux` |
| **macOS ARM** | `./build.ps1 -IFrameUrl "https://example.com" -ApiUrl "https://api.example.com" -Platform mac-arm` |
| **macOS Intel** | `./build.ps1 -IFrameUrl "https://example.com" -ApiUrl "https://api.example.com" -Platform mac-intel` |

**Development (uses default dev URLs):**
```bash
dotnet run
```

**Clean build artifacts:**
```bash
make clean  # or: Remove-Item -Recurse bin,obj,Build/BuildConfiguration.cs
```

---

## Quick Start

### Windows (PowerShell)
```powershell
# Build for Windows with custom URL
./build.ps1 -IFrameUrl "https://example.com" -Platform windows

# Build for Linux
./build.ps1 -IFrameUrl "https://example.com" -Platform linux

# Build for macOS ARM (M1/M2)
./build.ps1 -IFrameUrl "https://example.com" -Platform mac-arm
```

### Linux/macOS (Makefile)
```bash
# Build for Windows
make build IFRAME_URL=https://example.com PLATFORM=windows

# Build for Linux
make build IFRAME_URL=https://example.com PLATFORM=linux

# Build for macOS ARM
make build IFRAME_URL=https://example.com PLATFORM=mac-arm
```

---

## Build Options

### PowerShell (`build.ps1`)

```powershell
./build.ps1 `
    -IFrameUrl "https://your-client-url.com" `
    -ApiUrl "https://your-api-url.com" `
    -AppName "YourAppName" `
    -AppLogo "path/to/logo.png" `
    -Platform windows `
    -Configuration Release `
    -SelfContained
```

**Parameters:**

| Parameter       | Description                          | Default                                    |
|-----------------|--------------------------------------|--------------------------------------------|
| `-IFrameUrl`    | WebView/IFrame URL                   | `https://dev-client.example.com/` |
| `-ApiUrl`       | API Server URL                       | `https://dev-api.example.com/` |
| `-AppName`      | Application display name             | `ECoopSystem`                              |
| `-AppLogo`      | Path to application logo             | `Assets/Images/logo.png`                   |
| `-Platform`     | Target platform (see below)          | `windows`                                  |
| `-Configuration`| Build configuration                  | `Release`                                  |
| `-SelfContained`| Create self-contained executable     | `$true`                                    |

---

### Makefile (Linux/macOS)

```bash
make build \
    IFRAME_URL=https://your-client-url.com \
    API_URL=https://your-api-url.com \
    APP_NAME=YourAppName \
    PLATFORM=linux \
    CONFIG=Release
```

**Variables:**

| Variable    | Description              | Default                                    |
|-------------|--------------------------|---------------------------------------------|
| `IFRAME_URL`| WebView/IFrame URL       | `https://dev-client.example.com/` |
| `API_URL`   | API Server URL           | `https://dev-api.example.com/` |
| `APP_NAME`  | Application name         | `ECoopSystem`                              |
| `APP_LOGO`  | Logo file path           | `Assets/Images/logo.png`                   |
| `PLATFORM`  | Target platform          | `windows`                                  |
| `CONFIG`    | Build configuration      | `Release`                                  |

---

## Supported Platforms

| Platform      | Runtime ID    | Description                    |
|---------------|---------------|--------------------------------|
| `windows`     | `win-x64`     | Windows 10/11 (64-bit)         |
| `linux`       | `linux-x64`   | Linux (64-bit)                 |
| `linux-deb`   | `linux-x64`   | Debian/Ubuntu Linux            |
| `linux-arm`   | `linux-arm64` | Linux ARM (Raspberry Pi, etc.) |
| `mac-intel`   | `osx-x64`     | macOS Intel (x86_64)           |
| `mac-arm`     | `osx-arm64`   | macOS Apple Silicon (M1/M2)    |

---

## Build Examples

### Example 1: Production Build for Windows
```powershell
./build.ps1 `
    -IFrameUrl "https://app.example.com" `
    -ApiUrl "https://api.example.com" `
    -Platform windows
```

### Example 2: Staging Build for Linux
```bash
make build \
    IFRAME_URL=https://staging.example.com \
    API_URL=https://api-staging.example.com \
    PLATFORM=linux
```

### Example 3: Custom Branded Build
```powershell
./build.ps1 `
    -IFrameUrl "https://customclient.com" `
    -ApiUrl "https://customapi.com" `
    -AppName "Custom Coop System" `
    -AppLogo "branding/custom-logo.png" `
    -Platform windows
```

### Example 4: Multi-Platform Release
```bash
# Windows
./build.ps1 -IFrameUrl "https://example.com" -Platform windows

# Linux
./build.ps1 -IFrameUrl "https://example.com" -Platform linux

# macOS Intel
./build.ps1 -IFrameUrl "https://example.com" -Platform mac-intel

# macOS ARM
./build.ps1 -IFrameUrl "https://example.com" -Platform mac-arm
```

---

## Output Location

Built executables are placed in:
```
bin/Release/net9.0/<runtime-id>/publish/
```

For example:
- Windows: `bin/Release/net9.0/win-x64/publish/ECoopSystem.exe`
- Linux: `bin/Release/net9.0/linux-x64/publish/ECoopSystem`
- macOS: `bin/Release/net9.0/osx-arm64/publish/ECoopSystem`

**Important:** The build creates a **directory** with multiple files, not a single executable. This is required because:
- WebView/CEF (Chromium Embedded Framework) requires external runtime files
- CEF binaries (libcef.dll/libcef.so), locales, resources must be accessible
- Single-file publish is **not supported** with WebView controls

**Do not delete or move files** from the publish directory - distribute the entire folder.

---

## Clean Build Artifacts

### PowerShell
```powershell
Remove-Item -Recurse -Force bin, obj, Build/BuildConfiguration.cs
```

### Makefile
```bash
make clean
```

---

## How It Works

1. **Template File**: `Build/BuildConfiguration.template.cs` contains placeholders
2. **Build Script**: Replaces placeholders with actual values
3. **Generated File**: `Build/BuildConfiguration.cs` is created with final values
4. **Compilation**: Application uses `BuildConfiguration` class for URLs
5. **Debug Mode**: Uses hardcoded development URLs
6. **Release Mode**: Uses build-time configured URLs

---

## CI/CD Integration

### GitHub Actions Example

```yaml
name: Build Release

on:
  push:
    tags:
      - 'v*'

jobs:
  build-windows:
    runs-on: windows-latest
    steps:
      - uses: actions/checkout@v3
      - name: Setup .NET
        uses: actions/setup-dotnet@v3
        with:
          dotnet-version: '9.0.x'
      - name: Build
        run: |
          ./build.ps1 `
            -IFrameUrl "${{ secrets.PROD_IFRAME_URL }}" `
            -ApiUrl "${{ secrets.PROD_API_URL }}" `
            -Platform windows
      - name: Upload Artifact
        uses: actions/upload-artifact@v3
        with:
          name: ECoopSystem-Windows
          path: bin/Release/net9.0/win-x64/publish/

  build-linux:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v3
      - name: Setup .NET
        uses: actions/setup-dotnet@v3
        with:
          dotnet-version: '9.0.x'
      - name: Build
        run: |
          make build \
            IFRAME_URL=${{ secrets.PROD_IFRAME_URL }} \
            API_URL=${{ secrets.PROD_API_URL }} \
            PLATFORM=linux
      - name: Upload Artifact
        uses: actions/upload-artifact@v3
        with:
          name: ECoopSystem-Linux
          path: bin/Release/net9.0/linux-x64/publish/
```

---

## Troubleshooting

### "BuildConfiguration not found"
- Ensure `Build/BuildConfiguration.cs` exists
- Run `make generate-config` or `./build.ps1` to generate it

### "Permission denied" on Linux/macOS
```bash
chmod +x build.ps1
```

### PowerShell execution policy error
```powershell
Set-ExecutionPolicy -ExecutionPolicy RemoteSigned -Scope CurrentUser
```

---

## Security Note

?? **Never commit production URLs to source control!**

- Development URLs are safe to commit
- Production URLs should be passed via:
  - Environment variables in CI/CD
  - Command-line arguments during build
  - Secure configuration management

---

For more information, see the main [README.md](../README.md)
