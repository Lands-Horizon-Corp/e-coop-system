#!/usr/bin/env pwsh
# Build Ubuntu .deb installer for ECoopSystem
# PowerShell wrapper that invokes the Linux bash script

param(
    [string]$Version = "1.0.0",
    [string]$IFrameUrl = "https://e-coop-client-development.up.railway.app/",
    [string]$ApiUrl = "https://e-coop-server-development.up.railway.app/",
    [string]$Configuration = "Release",
    [switch]$SkipBuild = $false,
    [switch]$OpenOutput = $false
)

function Write-Header {
    param([string]$Message)
    Write-Host ""
    Write-Host "========================================" -ForegroundColor Cyan
    Write-Host $Message -ForegroundColor Cyan
    Write-Host "========================================" -ForegroundColor Cyan
    Write-Host ""
}

function Write-Info {
    param([string]$Message)
    Write-Host "- $Message" -ForegroundColor Yellow
}

$isWindows = $PSVersionTable.Platform -eq "Win32NT" -or $PSVersionTable.OS -like "*Windows*"
$isLinux = $PSVersionTable.Platform -eq "Linux"

Write-Header "ECoopSystem - Ubuntu DEB Builder"

if ($isWindows) {
    Write-Host "Run this in WSL Ubuntu:" -ForegroundColor Yellow
    Write-Host "  bash build-linux-installer.sh $Version '$IFrameUrl' '$ApiUrl' $Configuration" -ForegroundColor Cyan
    exit 1
}

if (-not $isLinux) {
    Write-Host "Unsupported platform for Linux .deb build." -ForegroundColor Red
    exit 1
}

Write-Info "Delegating to build-linux-installer.sh"

$bashArgs = @("build-linux-installer.sh", $Version, $IFrameUrl, $ApiUrl, $Configuration)

if ($SkipBuild) {
    Write-Info "SkipBuild is not used by the bash script and will be ignored."
}

bash @bashArgs

if ($LASTEXITCODE -ne 0) {
    Write-Host "Linux .deb build failed." -ForegroundColor Red
    exit $LASTEXITCODE
}

if ($OpenOutput) {
    Write-Info "Output directory: ./output/installer"
}
