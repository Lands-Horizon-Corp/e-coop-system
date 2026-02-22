#!/usr/bin/env pwsh
# Build and Create Installer for ECoopSystem
# This script builds the application with custom configuration and generates the Inno Setup installer

param(
    [string]$IFrameUrl = "https://e-coop-client-development.up.railway.app/",
    [string]$ApiUrl = "https://e-coop-server-development.up.railway.app/",
    [string]$Configuration = "Release",
    [string]$Version = "1.0.0",
    [switch]$SkipBuild = $false,
    [switch]$OpenOutput = $false
)

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "ECoopSystem - Build and Create Installer" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

# Configuration summary
Write-Host "Build Configuration:" -ForegroundColor Yellow
Write-Host "  IFrame URL:      $IFrameUrl" -ForegroundColor Gray
Write-Host "  API URL:         $ApiUrl" -ForegroundColor Gray
Write-Host "  Configuration:   $Configuration" -ForegroundColor Gray
Write-Host "  Version:         $Version" -ForegroundColor Gray
Write-Host "  Skip Build:      $SkipBuild" -ForegroundColor Gray
Write-Host ""

# Warning if using development URLs
if ($ApiUrl -like "*development*" -or $IFrameUrl -like "*development*") {
    Write-Host "WARNING: You are building an installer with DEVELOPMENT URLs!" -ForegroundColor Yellow
    Write-Host "The installed application will connect to development servers." -ForegroundColor Yellow
    Write-Host "For production builds, use:" -ForegroundColor Yellow
    Write-Host "  ./build-installer.ps1 -ApiUrl 'https://api.production.com' -IFrameUrl 'https://app.production.com'" -ForegroundColor Cyan
    Write-Host ""
}

# Check if Inno Setup is installed
$isccPath = Get-Command iscc -ErrorAction SilentlyContinue

if (-not $isccPath) {
    # Try default installation path
    $defaultPath = "C:\Program Files (x86)\Inno Setup 6\ISCC.exe"
    if (Test-Path $defaultPath) {
        $isccPath = $defaultPath
    } else {
        Write-Host "[ERROR] Inno Setup Compiler (iscc.exe) not found!" -ForegroundColor Red
        Write-Host ""
        Write-Host "Please install Inno Setup from: https://jrsoftware.org/isdl.php" -ForegroundColor Yellow
        Write-Host "After installation, add the Inno Setup installation directory to your PATH." -ForegroundColor Yellow
        Write-Host "Default location: C:\Program Files (x86)\Inno Setup 6" -ForegroundColor Yellow
        Write-Host ""
        exit 1
    }
} else {
    $isccPath = $isccPath.Source
}

Write-Host "Using Inno Setup Compiler: $isccPath" -ForegroundColor Green
Write-Host ""

if (-not $SkipBuild) {
    # Clean previous build
    Write-Host "[1/5] Cleaning previous build..." -ForegroundColor Cyan
    if (Test-Path "bin\$Configuration") {
        Remove-Item -Path "bin\$Configuration" -Recurse -Force
    }
    if (Test-Path "output\installer") {
        Remove-Item -Path "output\installer" -Recurse -Force
    }

    # Build using the existing build script
    Write-Host "[2/5] Building application with custom configuration..." -ForegroundColor Cyan
    Write-Host ""
    
    $buildArgs = @{
        IFrameUrl = $IFrameUrl
        ApiUrl = $ApiUrl
        Platform = "windows"
        Configuration = $Configuration
    }
    
    & .\build.ps1 @buildArgs
    
    if ($LASTEXITCODE -ne 0) {
        Write-Host "[ERROR] Build failed!" -ForegroundColor Red
        exit 1
    }
    
    Write-Host ""
    Write-Host "[3/5] Build completed successfully!" -ForegroundColor Green
    Write-Host "Output directory: bin\$Configuration\net9.0\win-x64\publish\" -ForegroundColor Gray
    Write-Host ""
} else {
    Write-Host "[SKIPPED] Build step skipped." -ForegroundColor Yellow
    Write-Host ""
}

# Update version in installer script if different
if ($Version -ne "1.0.0") {
    Write-Host "[4/5] Updating version in installer script..." -ForegroundColor Cyan
    $issContent = Get-Content "installer.iss" -Raw
    $issContent = $issContent -replace '#define MyAppVersion ".*"', "#define MyAppVersion `"$Version`""
    Set-Content "installer.iss" -Value $issContent
    Write-Host "Version updated to: $Version" -ForegroundColor Green
    Write-Host ""
} else {
    Write-Host "[4/5] Using default version from installer script." -ForegroundColor Cyan
    Write-Host ""
}

# Create installer
Write-Host "[5/5] Creating installer with Inno Setup..." -ForegroundColor Cyan
Write-Host ""

$issFile = Join-Path $PWD "installer.iss"

# Update the configuration path in the ISS file temporarily
$tempIssFile = Join-Path $PWD "installer.temp.iss"
$issContent = Get-Content $issFile -Raw
$issContent = $issContent -replace 'bin\\Release\\', "bin\$Configuration\"
Set-Content $tempIssFile -Value $issContent

try {
    & $isccPath $tempIssFile
    
    if ($LASTEXITCODE -ne 0) {
        Write-Host "[ERROR] Installer creation failed!" -ForegroundColor Red
        exit 1
    }
} finally {
    # Clean up temporary file
    if (Test-Path $tempIssFile) {
        Remove-Item $tempIssFile
    }
}

Write-Host ""
Write-Host "========================================" -ForegroundColor Green
Write-Host "SUCCESS!" -ForegroundColor Green
Write-Host "========================================" -ForegroundColor Green
Write-Host ""
Write-Host "Installer created successfully!" -ForegroundColor Green
Write-Host "Location: output\installer\" -ForegroundColor Gray
Write-Host ""

# List created installers
Get-ChildItem "output\installer\*.exe" | ForEach-Object {
    $size = [math]::Round($_.Length / 1MB, 2)
    Write-Host "  - $($_.Name) ($size MB)" -ForegroundColor Cyan
}

Write-Host ""

# Open output folder if requested
if ($OpenOutput) {
    Write-Host "Opening output folder..." -ForegroundColor Yellow
    explorer "output\installer"
}

Write-Host "Done!" -ForegroundColor Green
