# Windows Deployment Guide for ECoopSystem
# Windows Deployment Guide for ECoopSystem

## System Requirements

### Minimum Requirements
- Windows 10 (64-bit) or later
- x64 architecture
- 2 GB RAM minimum
- 500 MB disk space
- Internet access for license validation

### Recommended
- Windows 11 (64-bit)
- 4 GB RAM or higher
- SSD storage

## Dependencies

### .NET
If you are using a framework-dependent publish, install:
- .NET 9 Desktop Runtime (x64)

If you are using self-contained publish, runtime installation is not required.

### WebView
ECoopSystem uses embedded web content. Install:
- Microsoft Edge WebView2 Runtime (Evergreen)

Check installation:
```powershell
Get-ItemProperty "HKLM:\SOFTWARE\Microsoft\EdgeUpdate\Clients\*" |
  Where-Object { $_.name -like "*WebView*" } |
  Select-Object name, pv
```

## Building for Windows

### Option 1: Self-Contained (Recommended)
Includes the .NET runtime in the publish output.
```powershell
dotnet publish -c Release -r win-x64 --self-contained -p:PublishSingleFile=true
```

### Option 2: Framework-Dependent
Requires .NET runtime on the target system.
```powershell
dotnet publish -c Release -r win-x64 --no-self-contained -p:PublishSingleFile=true
```

### Build Windows Installer (Inno Setup)
```powershell
.\build-windows-installer.ps1 -Version "1.0.0"
```

Installer output:
- `output\installer\ECoopSystem-Setup-1.0.0-win-x64.exe`

## Installation

### Manual Installation
```powershell
# Create installation directory
New-Item -ItemType Directory -Force "C:\Program Files\ECoopSystem" | Out-Null

# Copy published files
Copy-Item ".\publish\win-x64\*" "C:\Program Files\ECoopSystem\" -Recurse -Force
```

### Installer-Based Installation
1. Run `ECoopSystem-Setup-<version>-win-x64.exe`
2. Follow installer steps
3. Launch from Start Menu or desktop shortcut

## Configuration

### Application Data Location
```text
%APPDATA%\ECoopSystem\
```

Common files:
- `%APPDATA%\ECoopSystem\appsettings.json`
- `%APPDATA%\ECoopSystem\appstate.dat`
- `%APPDATA%\ECoopSystem\secret.dat`
- `%APPDATA%\ECoopSystem\dp-keys\`

### Required Permissions
- Read/Write to `%APPDATA%\ECoopSystem\`
- Outbound HTTPS access for license validation

## Troubleshooting

### Application Does Not Start
```powershell
# From publish folder
.\ECoopSystem.exe
```

If blocked by SmartScreen:
1. Click **More info**
2. Click **Run anyway** for trusted internal builds

### Missing Runtime Error
If you see a .NET runtime error:
- Rebuild as self-contained, or
- Install .NET 9 Desktop Runtime (x64)

### WebView2 Runtime Missing
Install WebView2 Runtime:
- https://developer.microsoft.com/microsoft-edge/webview2/

### Permission Denied or Access Errors
Run terminal as Administrator for installation into `C:\Program Files`.

## Firewall and Network

Allow outbound HTTPS (TCP 443) for API and license endpoints.

PowerShell example:
```powershell
New-NetFirewallRule `
  -DisplayName "ECoopSystem Outbound HTTPS" `
  -Direction Outbound `
  -Action Allow `
  -Protocol TCP `
  -RemotePort 443
```

## Development on Windows

### Run in Development
```powershell
dotnet run --project .\ECoopSystem.csproj
```

### Hot Reload
```powershell
dotnet watch --project .\ECoopSystem.csproj
```

### Diagnostics
```powershell
dotnet --info
dotnet --list-sdks
dotnet --list-runtimes
```

## Support and Resources

- Documentation: [Project README](README.md)
- Installer Guide: [INSTALLER.md](INSTALLER.md)
- Issues: https://github.com/Lands-Horizon-Corp/e-coop-system/issues
- .NET on Windows: https://learn.microsoft.com/dotnet/core/install/windows
- WebView2 Runtime: https://developer.microsoft.com/microsoft-edge/webview2/
