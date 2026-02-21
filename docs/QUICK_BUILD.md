# Quick Build Reference

## Windows

### PowerShell
```powershell
./build.ps1 -IFrameUrl "https://example.com" -Platform windows
```

### Command Prompt
```cmd
build.bat https://example.com windows
```

## Linux/macOS

### Using Make
```bash
make build IFRAME_URL=https://example.com PLATFORM=linux
```

### Using Shell Script
```bash
chmod +x build.sh
./build.sh https://example.com https://api.example.com linux
```

## All Platforms

```bash
# Windows
./build.ps1 -IFrameUrl "https://example.com" -Platform windows

# Linux
./build.ps1 -IFrameUrl "https://example.com" -Platform linux

# macOS ARM (M1/M2)
./build.ps1 -IFrameUrl "https://example.com" -Platform mac-arm

# macOS Intel
./build.ps1 -IFrameUrl "https://example.com" -Platform mac-intel
```

## Common Use Cases

### Development Build (uses dev URLs)
```bash
dotnet build
```

### Production Build
```bash
./build.ps1 -IFrameUrl "https://app.production.com" -ApiUrl "https://api.production.com" -Platform windows
```

### Staging Build
```bash
./build.ps1 -IFrameUrl "https://app.staging.com" -ApiUrl "https://api.staging.com" -Platform linux
```

### Custom Branded Build
```bash
./build.ps1 `
    -IFrameUrl "https://custom.client.com" `
    -ApiUrl "https://custom.api.com" `
    -AppName "Custom Coop" `
    -Platform windows
```
