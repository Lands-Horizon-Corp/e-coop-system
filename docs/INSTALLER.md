# Creating Windows Installer with Inno Setup

This guide explains how to create a Windows installer for ECoopSystem using Inno Setup.

## Prerequisites

### 1. Install Inno Setup

Download and install Inno Setup from: https://jrsoftware.org/isdl.php

**Recommended Version:** Inno Setup 6.3.0 or later

**Installation Path:** 
- Default: `C:\Program Files (x86)\Inno Setup 6`
- Make sure to add the installation directory to your system PATH

### 2. Verify Installation

Open a command prompt or PowerShell and run:
```bash
iscc /?
```

If the command is not found, add Inno Setup to your PATH:
1. Open System Environment Variables
2. Add `C:\Program Files (x86)\Inno Setup 6` to the PATH variable
3. Restart your terminal

## Quick Start

### Option 1: Simple Build (Batch Script)

For a quick installer build with default settings:

```batch
build-installer.bat
```

This will:
1. Clean previous builds
2. Build the application in Release mode for win-x64
3. Create the installer using Inno Setup
4. Output the installer to `output\installer\`

### Option 2: Custom Build (PowerShell)

For advanced builds with custom configuration:

```powershell
# Basic build
.\build-installer.ps1

# Production build with custom URLs
.\build-installer.ps1 `
    -IFrameUrl "https://your-production-app.com" `
    -ApiUrl "https://your-production-api.com" `
    -Configuration Release `
    -Version "1.0.0"

# Skip rebuild and just create installer
.\build-installer.ps1 -SkipBuild

# Build and open output folder
.\build-installer.ps1 -OpenOutput
```

## PowerShell Script Parameters

| Parameter | Description | Default |
|-----------|-------------|---------|
| `-IFrameUrl` | Client application URL | Development URL |
| `-ApiUrl` | API server URL | Development URL |
| `-Configuration` | Build configuration (Release/Debug) | `Release` |
| `-Version` | Application version number | `1.0.0` |
| `-SkipBuild` | Skip build step, only create installer | `false` |
| `-OpenOutput` | Open output folder after completion | `false` |

## Installer Configuration

The installer script (`installer.iss`) includes the following features:

### Features
- ? **64-bit Windows Support** - Targets Windows 10/11 x64
- ? **Modern UI** - Uses Inno Setup's modern wizard style
- ? **Compression** - LZMA2 ultra compression for smaller file size
- ? **Desktop & Start Menu Shortcuts** - Optional during installation
- ? **Uninstaller** - Clean uninstallation with optional data removal
- ? **License Agreement** - Shows LICENSE.md during installation
- ? **README** - Includes README.md in installation
- ? **.NET Check** - Optional runtime detection (commented out by default)

### Installation Locations

| Item | Location |
|------|----------|
| **Application Files** | `C:\Program Files\ECoopSystem\` |
| **User Data** | `%APPDATA%\ECoopSystem\` |
| **Start Menu** | `%ProgramData%\Microsoft\Windows\Start Menu\Programs\` |
| **Desktop Icon** | `%USERPROFILE%\Desktop\` (optional) |

### Customization Options

Edit `installer.iss` to customize:

1. **Application Information**
```pascal
#define MyAppName "ECoopSystem"
#define MyAppVersion "1.0.0"
#define MyAppPublisher "Lands Horizon Corporation"
```

2. **Installation Directory**
```pascal
DefaultDirName={autopf}\{#MyAppName}
```

3. **Required Windows Version**
```pascal
MinVersion=10.0.17763  ; Windows 10 October 2018 Update
```

4. **Compression Settings**
```pascal
Compression=lzma2/ultra64
SolidCompression=yes
```

5. **Icons and Branding**
```pascal
SetupIconFile=Assets\Images\logo.png
WizardImageFile=compiler:WizModernImage-IS.bmp
```

## Build Output

After successful build, you'll find:

```
output/
??? installer/
    ??? ECoopSystem-Setup-1.0.0-win-x64.exe
```

**Installer Naming Convention:**
- Format: `ECoopSystem-Setup-{Version}-{Platform}.exe`
- Example: `ECoopSystem-Setup-1.0.0-win-x64.exe`

## Advanced Configuration

### Enable .NET Runtime Check

To enforce .NET 9 runtime check before installation, edit `installer.iss` and uncomment the runtime check in the `InitializeSetup()` function:

```pascal
function InitializeSetup(): Boolean;
var
  ErrorCode: Integer;
begin
  Result := True;
  
  // Uncomment these lines to enable .NET check
  if not IsDotNetInstalled() then
  begin
    if MsgBox('.NET 9 Runtime does not appear to be installed...', 
              mbConfirmation, MB_YESNO) = IDYES then
    begin
      ShellExec('open',
        'https://dotnet.microsoft.com/download/dotnet/9.0',
        '', '', SW_SHOW, ewNoWait, ErrorCode);
    end;
  end;
end;
```

### Custom Uninstall Behavior

The installer prompts users to remove application data during uninstallation. This includes:
- Configuration files
- Data protection keys
- Cached data
- Application state

Users can choose to keep or remove this data.

### Including Additional Files

To include additional files in the installer, add them to the `[Files]` section:

```pascal
[Files]
; Your additional files
Source: "docs\*"; DestDir: "{app}\docs"; Flags: ignoreversion recursesubdirs
Source: "config\*.json"; DestDir: "{app}\config"; Flags: ignoreversion
```

## Building for Different Environments

### Development Build
```powershell
.\build-installer.ps1 `
    -Configuration Release `
    -Version "1.0.0-dev"
```

### Staging Build
```powershell
.\build-installer.ps1 `
    -IFrameUrl "https://staging-client.com" `
    -ApiUrl "https://staging-api.com" `
    -Configuration Release `
    -Version "1.0.0-staging"
```

### Production Build
```powershell
.\build-installer.ps1 `
    -IFrameUrl "https://production-client.com" `
    -ApiUrl "https://production-api.com" `
    -Configuration Release `
    -Version "1.0.0"
```

## Testing the Installer

### Before Distribution

1. **Test Installation**
   - Run the installer on a clean Windows 10/11 machine
   - Verify all files are installed correctly
   - Check shortcuts work properly

2. **Test Application**
   - Launch the application from Start Menu
   - Verify configuration is correct
   - Test all major features

3. **Test Uninstallation**
   - Uninstall the application
   - Verify all files are removed
   - Check if user data cleanup works

4. **Test Upgrade**
   - Install version 1.0.0
   - Install version 1.0.1 over it
   - Verify settings are preserved

### Common Issues

| Issue | Solution |
|-------|----------|
| **iscc.exe not found** | Add Inno Setup to PATH |
| **Build files not found** | Run build before creating installer |
| **Missing dependencies** | Ensure all files are in publish directory |
| **Icon not found** | Verify `Assets\Images\logo.png` exists |
| **Large installer size** | Enable compression in installer.iss |

## Continuous Integration

### GitHub Actions Example

```yaml
name: Build Installer

on:
  push:
    tags:
      - 'v*'

jobs:
  build-installer:
    runs-on: windows-latest
    
    steps:
    - uses: actions/checkout@v3
    
    - name: Setup .NET
      uses: actions/setup-dotnet@v3
      with:
        dotnet-version: '9.0.x'
    
    - name: Install Inno Setup
      run: choco install innosetup -y
    
    - name: Build Installer
      run: .\build-installer.ps1 -Version ${{ github.ref_name }}
    
    - name: Upload Installer
      uses: actions/upload-artifact@v3
      with:
        name: installer
        path: output/installer/*.exe
```

## Distribution

### Code Signing (Recommended)

For production releases, sign your installer with a code signing certificate:

1. Obtain a code signing certificate
2. Add to `installer.iss`:

```pascal
[Setup]
SignTool=signtool
SignedUninstaller=yes
```

3. Configure signtool in Inno Setup Compiler settings

### Checksum Verification

Generate checksums for verification:

```powershell
# SHA256 checksum
Get-FileHash "output\installer\*.exe" -Algorithm SHA256 | Format-List

# MD5 checksum (optional)
Get-FileHash "output\installer\*.exe" -Algorithm MD5 | Format-List
```

Include checksums in your release notes for users to verify downloads.

## Additional Resources

- **Inno Setup Documentation**: https://jrsoftware.org/ishelp/
- **Inno Setup Examples**: https://jrsoftware.org/isinfo.php
- **Code Signing Guide**: https://learn.microsoft.com/windows/win32/seccrypto/cryptography-tools
- **Windows Installer Best Practices**: https://learn.microsoft.com/windows/win32/msi/installation-best-practices

## Support

For issues or questions:
- Check the [main documentation](../README.md)
- Review [build documentation](BUILD.md)
- Open an issue on [GitHub](https://github.com/Lands-Horizon-Corp/e-coop-system/issues)

---

**Note**: Always test installers thoroughly before distributing to end users.
