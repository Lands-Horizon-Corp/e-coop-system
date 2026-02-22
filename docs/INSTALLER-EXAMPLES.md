# Installer Configuration Examples

This file contains example commands for building installers for different environments.

## Environment Configurations

### Development Environment
```powershell
.\build-installer.ps1 `
    -IFrameUrl "https://e-coop-client-development.up.railway.app/" `
    -ApiUrl "https://e-coop-server-development.up.railway.app/" `
    -Configuration Release `
    -Version "1.0.0-dev" `
    -OpenOutput
```

### Staging Environment
```powershell
.\build-installer.ps1 `
    -IFrameUrl "https://e-coop-client-staging.up.railway.app/" `
    -ApiUrl "https://e-coop-server-staging.up.railway.app/" `
    -Configuration Release `
    -Version "1.0.0-staging"
```

### Production Environment
```powershell
.\build-installer.ps1 `
    -IFrameUrl "https://app.ecoopsystem.com/" `
    -ApiUrl "https://api.ecoopsystem.com/" `
    -Configuration Release `
    -Version "1.0.0"
```

### Testing Environment (Local)
```powershell
.\build-installer.ps1 `
    -IFrameUrl "http://localhost:3000/" `
    -ApiUrl "http://localhost:5000/" `
    -Configuration Debug `
    -Version "1.0.0-test"
```

## Batch Commands for CI/CD

### GitHub Actions Build
```yaml
- name: Build Windows Installer
  run: |
    .\build-installer.ps1 `
      -IFrameUrl "${{ secrets.IFRAME_URL }}" `
      -ApiUrl "${{ secrets.API_URL }}" `
      -Version "${{ github.ref_name }}" `
      -Configuration Release
```

### Build Multiple Versions
```powershell
# Build all environment installers
$environments = @(
    @{ Name = "dev"; IFrame = "https://dev.app.com"; Api = "https://dev.api.com" },
    @{ Name = "staging"; IFrame = "https://staging.app.com"; Api = "https://staging.api.com" },
    @{ Name = "production"; IFrame = "https://app.com"; Api = "https://api.com" }
)

foreach ($env in $environments) {
    Write-Host "Building $($env.Name) installer..." -ForegroundColor Cyan
    .\build-installer.ps1 `
        -IFrameUrl $env.IFrame `
        -ApiUrl $env.Api `
        -Version "1.0.0-$($env.Name)" `
        -Configuration Release
}
```

## Version Numbering Schemes

### Semantic Versioning
```powershell
# Major.Minor.Patch
-Version "1.0.0"      # Stable release
-Version "1.0.1"      # Patch release
-Version "1.1.0"      # Minor release
-Version "2.0.0"      # Major release
```

### Pre-release Versions
```powershell
-Version "1.0.0-alpha"    # Alpha version
-Version "1.0.0-beta"     # Beta version
-Version "1.0.0-rc1"      # Release candidate 1
-Version "1.0.0-dev"      # Development build
```

### Build Numbers
```powershell
# Include build date or commit hash
$version = "1.0.0-$(Get-Date -Format 'yyyyMMdd')"
-Version $version

# With Git commit hash
$commit = git rev-parse --short HEAD
-Version "1.0.0-$commit"
```

## Custom Inno Setup Parameters

### Modify installer.iss for Custom Branding

```pascal
; Change application name
#define MyAppName "YourAppName"

; Change publisher
#define MyAppPublisher "Your Company Name"

; Change URLs
#define MyAppURL "https://yourcompany.com"

; Change installation directory
DefaultDirName={autopf}\YourAppName

; Change GUID (generate new one)
AppId={{YOUR-NEW-GUID-HERE}}
```

### Add Custom Icons
```pascal
[Icons]
; Add more shortcuts
Name: "{autoprograms}\{#MyAppName}\Documentation"; Filename: "{app}\README.md"
Name: "{autoprograms}\{#MyAppName}\Uninstall"; Filename: "{uninstallexe}"
```

### Include Additional Files
```pascal
[Files]
; Include documentation folder
Source: "docs\*"; DestDir: "{app}\docs"; Flags: ignoreversion recursesubdirs

; Include sample configurations
Source: "samples\*"; DestDir: "{app}\samples"; Flags: ignoreversion
```

## Advanced Build Scripts

### Build with Logging
```powershell
$logFile = "build-installer-$(Get-Date -Format 'yyyyMMdd-HHmmss').log"

.\build-installer.ps1 `
    -IFrameUrl "https://app.com" `
    -ApiUrl "https://api.com" `
    -Version "1.0.0" `
    *>&1 | Tee-Object -FilePath $logFile
```

### Build with Error Handling
```powershell
try {
    .\build-installer.ps1 `
        -IFrameUrl "https://app.com" `
        -ApiUrl "https://api.com" `
        -Version "1.0.0"
    
    Write-Host "? Installer created successfully!" -ForegroundColor Green
} catch {
    Write-Host "? Build failed: $_" -ForegroundColor Red
    exit 1
}
```

### Automated Testing After Build
```powershell
# Build installer
.\build-installer.ps1 -Version "1.0.0"

# Verify installer exists
$installerPath = "output\installer\ECoopSystem-Setup-1.0.0-win-x64.exe"
if (Test-Path $installerPath) {
    # Get file size
    $size = (Get-Item $installerPath).Length / 1MB
    Write-Host "Installer size: $([math]::Round($size, 2)) MB"
    
    # Generate checksum
    $hash = Get-FileHash $installerPath -Algorithm SHA256
    Write-Host "SHA256: $($hash.Hash)"
    
    # Save checksum to file
    $hash.Hash | Out-File "output\installer\checksum.txt"
} else {
    Write-Host "? Installer not found!" -ForegroundColor Red
    exit 1
}
```

## Distribution Checklist

Before distributing the installer:

1. ? Test installation on clean Windows 10/11 machine
2. ? Verify application launches correctly
3. ? Test uninstallation
4. ? Generate and verify checksums
5. ? Sign installer with code signing certificate (if available)
6. ? Test upgrade from previous version
7. ? Document release notes
8. ? Update version numbers in documentation

## Code Signing (Production)

```powershell
# Sign the installer after creation
$certPath = "path\to\certificate.pfx"
$certPassword = "your-password"
$installerPath = "output\installer\ECoopSystem-Setup-1.0.0-win-x64.exe"

signtool sign /f $certPath /p $certPassword /t http://timestamp.digicert.com $installerPath
```

## Continuous Deployment

### Automatic Release on Tag
```powershell
# In your CI/CD pipeline
if ($env:GITHUB_REF -match "refs/tags/v(.*)") {
    $version = $matches[1]
    
    .\build-installer.ps1 `
        -IFrameUrl $env:PRODUCTION_IFRAME_URL `
        -ApiUrl $env:PRODUCTION_API_URL `
        -Version $version `
        -Configuration Release
    
    # Upload to GitHub Releases
    gh release upload "v$version" "output\installer\*.exe"
}
```

## Notes

- Always use HTTPS URLs in production builds
- Keep sensitive configuration out of version control
- Test installers in isolated environments
- Maintain separate builds for different environments
- Document version changes in release notes
- Keep backup of previous installer versions

## Support

For help with installer configuration:
- [Full Documentation](INSTALLER.md)
- [Build System Guide](BUILD.md)
- [GitHub Issues](https://github.com/Lands-Horizon-Corp/e-coop-system/issues)
