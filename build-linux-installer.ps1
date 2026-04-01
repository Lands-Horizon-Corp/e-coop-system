#!/usr/bin/env pwsh
# Build and Create AppImage for ECoopSystem
# This script builds the application for Linux and packages it as an AppImage
# Can be run from Windows with WSL or from Linux directly

param(
    [string]$Version = "1.0.0",
    [string]$IFrameUrl = "https://e-coop-client-development.up.railway.app/",
    [string]$ApiUrl = "https://e-coop-server-development.up.railway.app/",
    [string]$Configuration = "Release",
    [switch]$SkipBuild = $false,
    [switch]$OpenOutput = $false
)

# Colors
$Error.Clear()

function Write-Header {
    param([string]$Message)
    Write-Host ""
    Write-Host "========================================" -ForegroundColor Cyan
    Write-Host $Message -ForegroundColor Cyan
    Write-Host "========================================" -ForegroundColor Cyan
    Write-Host ""
}

function Write-Success {
    param([string]$Message)
    Write-Host "? $Message" -ForegroundColor Green
}

function Write-Error-Custom {
    param([string]$Message)
    Write-Host "? $Message" -ForegroundColor Red
    exit 1
}

function Write-Info {
    param([string]$Message)
    Write-Host "? $Message" -ForegroundColor Yellow
}

# Check if running on Windows or Linux
$isWindows = $PSVersionTable.Platform -eq "Win32NT" -or $PSVersionTable.OS -like "*Windows*"
$isLinux = $PSVersionTable.Platform -eq "Linux"

Write-Header "ECoopSystem - AppImage Builder"

Write-Info "Detected OS: $(if ($isWindows) { "Windows" } elseif ($isLinux) { "Linux" } else { "Unknown" })"

if ($isWindows) {
    Write-Header "Windows Detected"
    Write-Host "This script builds Linux installers and only works on Linux systems." -ForegroundColor Red
    Write-Host ""
    Write-Host "To build Linux installers from Windows, use:" -ForegroundColor Yellow
    Write-Host ""
    Write-Host "Option 1: Git Bash (Recommended)" -ForegroundColor Cyan
    Write-Host "  1. Right-click in project folder ? Git Bash Here"
    Write-Host "  2. Run: bash build-linux-installer.sh 1.0.0"
    Write-Host ""
    Write-Host "Option 2: WSL (Windows Subsystem for Linux)" -ForegroundColor Cyan
    Write-Host "  1. Open WSL terminal in project directory"
    Write-Host "  2. Run: bash build-linux-installer.sh 1.0.0"
    Write-Host ""
    exit 1
}

# For Linux builds
if ($isLinux) {
    Write-Info "Running on Linux - executing build..."
    
    # Check for dotnet
    if (-not (Get-Command dotnet -ErrorAction SilentlyContinue)) {
        Write-Error-Custom ".NET SDK not found. Please install .NET 9 SDK."
    }
    Write-Success ".NET SDK found"
    
    # Check for tar
    if (-not (Get-Command tar -ErrorAction SilentlyContinue)) {
        Write-Error-Custom "tar command not found"
    }
    Write-Success "tar found"
    
    $outputDir = "./output/installer"
    $buildDir = "./build-appimage"
    $appDir = "$buildDir/AppDir"

    Write-Host ""
    Write-Host "Build Configuration:" -ForegroundColor Yellow
    Write-Host "  IFrame URL:      $IFrameUrl" -ForegroundColor Gray
    Write-Host "  API URL:         $ApiUrl" -ForegroundColor Gray
    Write-Host "  Configuration:   $Configuration" -ForegroundColor Gray
    Write-Host "  Version:         $Version" -ForegroundColor Gray
    Write-Host ""

    if ($ApiUrl -like "*development*" -or $IFrameUrl -like "*development*") {
        Write-Host "WARNING: You are building an installer with DEVELOPMENT URLs!" -ForegroundColor Yellow
        Write-Host "The installed application will connect to development servers." -ForegroundColor Yellow
        Write-Host ""
    }
    
    # Clean previous build
    Write-Info "Cleaning previous builds..."
    Remove-Item -Path $buildDir -Recurse -Force -ErrorAction SilentlyContinue
    Remove-Item -Path "bin/$Configuration/net9.0/linux-x64" -Recurse -Force -ErrorAction SilentlyContinue
    New-Item -ItemType Directory -Path $outputDir -Force | Out-Null
    Write-Success "Clean complete"
    
    if (-not $SkipBuild) {
        # Build the application
        Write-Info "Building ECoopSystem for Linux (net9.0, linux-x64)..."
        & dotnet publish -c $Configuration -r linux-x64 --self-contained true -p:PublishTrimmed=false -p:IncludeNativeLibrariesForSelfExtract=true -p:IFrameUrl=$IFrameUrl -p:ApiUrl=$ApiUrl
        Write-Success "Build complete"
    }
    
    # Create AppDir structure
    Write-Info "Creating AppImage directory structure..."
    @(
        "$appDir/usr/bin",
        "$appDir/usr/lib/ECoopSystem",
        "$appDir/usr/share/applications",
        "$appDir/usr/share/icons/hicolor/256x256/apps",
        "$appDir/usr/share/pixmaps",
        "$appDir/usr/share/doc/ECoopSystem"
    ) | ForEach-Object { New-Item -ItemType Directory -Path $_ -Force | Out-Null }
    Write-Success "Directory structure created"
    
    # Copy application files
    Write-Info "Copying application files..."
    Copy-Item -Path "bin/$Configuration/net9.0/linux-x64/publish/*" -Destination "$appDir/usr/lib/ECoopSystem/" -Recurse -Force
    Write-Success "Application files copied"
    
    # Create launcher script
    Write-Info "Creating launcher script..."
    $launcherScript = @"
#!/bin/bash
exec "/usr/lib/ECoopSystem/ECoopSystem" "`$@"
"@
    Set-Content -Path "$appDir/usr/bin/ECoopSystem" -Value $launcherScript -Encoding Ascii
    & chmod +x "$appDir/usr/bin/ECoopSystem"
    Write-Success "Launcher script created"
    
    # Create AppRun script
    Write-Info "Creating AppRun script..."
    $appRunScript = @"
#!/bin/bash
SELF=`$(readlink -f "`$0")
HERE="`${SELF%/*}"
EXEC="`${HERE}/usr/lib/ECoopSystem/ECoopSystem"
export LD_LIBRARY_PATH="`${HERE}/usr/lib:`${HERE}/usr/lib/x86_64-linux-gnu:`$LD_LIBRARY_PATH"
export PATH="`${HERE}/usr/bin:`$PATH"
exec "`$EXEC" "`$@"
"@
    Set-Content -Path "$appDir/AppRun" -Value $appRunScript -Encoding Ascii
    & chmod +x "$appDir/AppRun"
    Write-Success "AppRun script created"
    
    # Create desktop entry
    Write-Info "Creating desktop entry..."
    $desktopEntry = @"
[Desktop Entry]
Type=Application
Name=ECoopSystem
Comment=E-Cooperative Management System
Exec=ECoopSystem
Icon=ecoopsystem
Categories=Business;Office;Finance;
Terminal=false
StartupNotify=true
"@
    Set-Content -Path "$appDir/usr/share/applications/ECoopSystem.desktop" -Value $desktopEntry -Encoding Ascii
    Write-Success "Desktop entry created"
    
    # Try to copy icon if it exists
    if (Test-Path "Assets/Icons/ecoopsuite.ico") {
        Write-Info "Copying icon..."
        Copy-Item -Path "Assets/Icons/ecoopsuite.ico" -Destination "$appDir/usr/share/pixmaps/ecoopsystem.ico" -Force -ErrorAction SilentlyContinue
        Write-Success "Icon copied"
    }
    elseif (Test-Path "Assets/Icons/ecoopsuite.png") {
        Write-Info "Copying icon..."
        Copy-Item -Path "Assets/Icons/ecoopsuite.png" -Destination "$appDir/usr/share/icons/hicolor/256x256/apps/ecoopsystem.png" -Force
        Copy-Item -Path "Assets/Icons/ecoopsuite.png" -Destination "$appDir/usr/share/pixmaps/ecoopsystem.png" -Force
        Write-Success "Icon copied"
    }
    else {
        Write-Info "No icon found - skipping"
    }
    
    # Create changelog
    Write-Info "Creating changelog..."
    $changelog = @"
# ECoopSystem v$Version

## Release Notes
- Release date: $(Get-Date -Format 'yyyy-MM-dd')
- Built for Linux x86_64
- .NET 9 Self-Contained Deployment

## System Requirements
- Linux Kernel 4.15+
- x86_64 architecture
- 2GB RAM minimum
- 500MB disk space
- X11 or Wayland display server

For more information, visit: https://github.com/Lands-Horizon-Corp/e-coop-system
"@
    Set-Content -Path "$appDir/usr/share/doc/ECoopSystem/changelog" -Value $changelog -Encoding Ascii
    Write-Success "Changelog created"
    
    # Package as tar.gz
    Write-Info "Creating portable tar.gz archive..."
    Push-Location $buildDir
    tar -czf "../$outputDir/ECoopSystem-$Version-linux-x64.tar.gz" "AppDir/"
    Pop-Location
    Write-Success "Tar.gz created: $outputDir/ECoopSystem-$Version-linux-x64.tar.gz"
    
    # Check for appimagetool
    if (Get-Command appimagetool -ErrorAction SilentlyContinue) {
        Write-Info "Creating AppImage..."
        $appImagePath = "$outputDir/ECoopSystem-$Version-x86_64.AppImage"
        & appimagetool -n $appDir $appImagePath 2>$null
        & chmod +x $appImagePath
        Write-Success "AppImage created: $appImagePath"
    }
    else {
        Write-Info "appimagetool not found - AppImage creation skipped"
        Write-Info "To create AppImage, install appimagetool:"
        Write-Info "  wget https://github.com/AppImage/AppImageKit/releases/download/continuous/appimagetool-x86_64.AppImage"
        Write-Info "  chmod +x appimagetool-x86_64.AppImage"
        Write-Info "  sudo mv appimagetool-x86_64.AppImage /usr/local/bin/appimagetool"
    }
    
    # Summary
    Write-Header "Build Complete"
    Write-Success "ECoopSystem $Version Linux build complete!"
    
    Get-ChildItem "$outputDir/ECoopSystem-*" -ErrorAction SilentlyContinue | ForEach-Object {
        Write-Host "  $($_.Name)`t$('{0:N2} MB' -f (($_.Length) / 1MB))"
    }
    
    if ($OpenOutput) {
        Write-Info "Opening output directory..."
        $xdgOpen = Get-Command xdg-open -ErrorAction SilentlyContinue
        if ($xdgOpen) {
            & xdg-open $outputDir 2>$null
        }
        else {
            $nautilus = Get-Command nautilus -ErrorAction SilentlyContinue
            if ($nautilus) {
                & nautilus $outputDir 2>$null
            }
        }
    }
}
