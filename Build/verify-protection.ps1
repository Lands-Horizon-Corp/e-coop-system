#!/usr/bin/env pwsh
<#
.SYNOPSIS
    Verifies string encryption and checks for exposed sensitive data

.DESCRIPTION
    Scans compiled binaries for plain text sensitive strings to verify
    encryption is working correctly.

.EXAMPLE
    .\verify-protection.ps1
    .\verify-protection.ps1 -Configuration Release -Platform win-x64
#>

param(
    [ValidateSet("Debug", "Release")]
    [string]$Configuration = "Release",
    
    [ValidateSet("win-x64", "linux-x64", "osx-x64", "osx-arm64")]
    [string]$Platform = "win-x64",
    
    [string[]]$SensitiveStrings = @(
        "railway.app",
        "secret.dat",
        "appstate.dat",
        "license/activate",
        "license/verify",
        "e-coop-server",
        "e-coop-client",
        "api.example.com",
        "app.example.com"
    )
)

$ErrorActionPreference = "Stop"

Write-Host "=========================================" -ForegroundColor Cyan
Write-Host " Protection Verification" -ForegroundColor Cyan
Write-Host "=========================================" -ForegroundColor Cyan
Write-Host ""

# Check if build exists
$publishPath = "bin\$Configuration\net9.0\$Platform\publish"
if (-not (Test-Path $publishPath)) {
    Write-Host "? Build not found at: $publishPath" -ForegroundColor Red
    Write-Host "Run this first:" -ForegroundColor Yellow
    Write-Host "  dotnet publish -c $Configuration -r $Platform" -ForegroundColor White
    exit 1
}

Write-Host "Scanning: $publishPath" -ForegroundColor White
Write-Host ""

# Find DLL and EXE files
$dllPath = Join-Path $publishPath "ECoopSystem.dll"
$exePath = Join-Path $publishPath "ECoopSystem.exe"

if (-not (Test-Path $dllPath)) {
    Write-Host "? ECoopSystem.dll not found" -ForegroundColor Red
    exit 1
}

$filesToCheck = @($dllPath)
if (Test-Path $exePath) {
    $filesToCheck += $exePath
}

Write-Host "Files to check:" -ForegroundColor Yellow
foreach ($file in $filesToCheck) {
    $size = (Get-Item $file).Length / 1MB
    Write-Host "  - $(Split-Path $file -Leaf) ($([math]::Round($size, 2)) MB)" -ForegroundColor White
}
Write-Host ""

# Check for sensitive strings
$foundIssues = @()
$protected = @()

Write-Host "Checking for exposed sensitive strings..." -ForegroundColor Yellow
Write-Host ""

foreach ($sensitiveString in $SensitiveStrings) {
    Write-Host "Checking for: '$sensitiveString'..." -NoNewline
    
    $found = $false
    foreach ($file in $filesToCheck) {
        $result = Select-String -Path $file -Pattern $sensitiveString -SimpleMatch -Quiet
        if ($result) {
            $found = $true
            break
        }
    }
    
    if ($found) {
        Write-Host " ? FOUND (Not Protected)" -ForegroundColor Red
        $foundIssues += $sensitiveString
    } else {
        Write-Host " ? Protected" -ForegroundColor Green
        $protected += $sensitiveString
    }
}

Write-Host ""
Write-Host "=========================================" -ForegroundColor Cyan
Write-Host " Verification Results" -ForegroundColor Cyan
Write-Host "=========================================" -ForegroundColor Cyan
Write-Host ""

Write-Host "Protected strings: $($protected.Count)" -ForegroundColor Green
Write-Host "Exposed strings:   $($foundIssues.Count)" -ForegroundColor $(if ($foundIssues.Count -eq 0) { "Green" } else { "Red" })
Write-Host ""

if ($foundIssues.Count -gt 0) {
    Write-Host "??  WARNING: Found exposed sensitive strings!" -ForegroundColor Red
    Write-Host ""
    Write-Host "Exposed strings:" -ForegroundColor Yellow
    foreach ($issue in $foundIssues) {
        Write-Host "  - $issue" -ForegroundColor Red
    }
    Write-Host ""
    Write-Host "Recommendation:" -ForegroundColor Yellow
    Write-Host "  1. Encrypt these strings using: .\Build\encrypt-string.ps1" -ForegroundColor White
    Write-Host "  2. Replace plain text in code with encrypted versions" -ForegroundColor White
    Write-Host "  3. Rebuild and run this verification again" -ForegroundColor White
    Write-Host ""
    exit 1
} else {
    Write-Host "? SUCCESS: No exposed sensitive strings detected!" -ForegroundColor Green
    Write-Host ""
    Write-Host "Your application is protected!" -ForegroundColor Green
    Write-Host ""
    Write-Host "Protection summary:" -ForegroundColor Cyan
    Write-Host "  ? String encryption: Active" -ForegroundColor Green
    Write-Host "  ? API endpoints: Protected" -ForegroundColor Green
    Write-Host "  ? File paths: Protected" -ForegroundColor Green
    
    # Check if obfuscated
    if (Test-Path ".\Confused\ECoopSystem.dll") {
        Write-Host "  ? Code obfuscation: Applied" -ForegroundColor Green
    } else {
        Write-Host "  ??  Code obfuscation: Not applied (optional)" -ForegroundColor Yellow
        Write-Host "      To enable: Install ConfuserEx and rebuild" -ForegroundColor DarkGray
    }
    
    Write-Host ""
}

# Additional checks
Write-Host "Additional security checks:" -ForegroundColor Cyan
Write-Host ""

# Check file size (obfuscation usually increases size)
$dllSize = (Get-Item $dllPath).Length / 1KB
Write-Host "  DLL size: $([math]::Round($dllSize, 2)) KB" -ForegroundColor White

# Check for debug symbols
$pdbPath = $dllPath -replace '\.dll$', '.pdb'
if (Test-Path $pdbPath) {
    Write-Host "  ??  Debug symbols found (remove for production)" -ForegroundColor Yellow
} else {
    Write-Host "  ? No debug symbols" -ForegroundColor Green
}

# Check for XML documentation
$xmlPath = $dllPath -replace '\.dll$', '.xml'
if (Test-Path $xmlPath) {
    Write-Host "  ??  XML documentation found (optional, can expose info)" -ForegroundColor Yellow
} else {
    Write-Host "  ? No XML documentation" -ForegroundColor Green
}

Write-Host ""
Write-Host "=========================================" -ForegroundColor Cyan
Write-Host "Verification complete!" -ForegroundColor Green
Write-Host "=========================================" -ForegroundColor Cyan
Write-Host ""
