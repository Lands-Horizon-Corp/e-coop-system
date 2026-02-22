#!/usr/bin/env pwsh
param(
    [string]$IFrameUrl = "https://e-coop-client-development.up.railway.app/",
    [string]$ApiUrl = "https://e-coop-server-development.up.railway.app/",
    [string]$AppName = "ECoopSystem",
    [string]$AppLogo = "Assets/Images/logo.png",
    
    [int]$ApiTimeout = 12,
    [int]$ApiMaxRetries = 3,
    [int]$ApiMaxResponseSizeBytes = 1048576,
    
    [string[]]$WebViewTrustedDomains = @("dev-client.example.com", "app.example.com", "api.example.com"),
    [bool]$WebViewAllowHttp = $false,
    
    [int]$SecurityGracePeriodDays = 7,
    [int]$SecurityMaxActivationAttempts = 3,
    [int]$SecurityLockoutMinutes = 5,
    [int]$SecurityActivationLookbackMinutes = 1,
    [int]$SecurityBackgroundVerificationIntervalMinutes = 1,
    
    [ValidateSet("windows", "linux", "linux-deb", "linux-arm", "mac-intel", "mac-arm")]
    [string]$Platform = "windows",
    [ValidateSet("Debug", "Release")]
    [string]$Configuration = "Release",
    [switch]$SelfContained = $true
)

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "  ECoopSystem Build Script" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

Write-Host "Build Configuration:" -ForegroundColor Yellow
Write-Host "  IFrame URL:    $IFrameUrl" -ForegroundColor White
Write-Host "  API URL:       $ApiUrl" -ForegroundColor White
Write-Host "  App Name:      $AppName" -ForegroundColor White
Write-Host "  Platform:      $Platform" -ForegroundColor White
Write-Host "  Configuration: $Configuration" -ForegroundColor White
Write-Host ""

$runtimeId = switch ($Platform) {
    "windows"       { "win-x64" }
    "linux"         { "linux-x64" }
    "linux-deb"     { "linux-x64" }
    "linux-arm"     { "linux-arm64" }
    "mac-intel"     { "osx-x64" }
    "mac-arm"       { "osx-arm64" }
}

Write-Host "Target Runtime: $runtimeId" -ForegroundColor Green
Write-Host ""

Write-Host "Generating BuildConfiguration.cs..." -ForegroundColor Yellow

$templateContent = Get-Content "Build/BuildConfiguration.template.cs" -Raw
$generatedContent = $templateContent `
    -replace '\$\(IFrameUrl\)', $IFrameUrl `
    -replace '\$\(ApiUrl\)', $ApiUrl `
    -replace '\$\(AppName\)', $AppName `
    -replace '\$\(AppLogo\)', $AppLogo `
    -replace '\$\(ApiTimeout\)', $ApiTimeout `
    -replace '\$\(ApiMaxRetries\)', $ApiMaxRetries `
    -replace '\$\(ApiMaxResponseSizeBytes\)', $ApiMaxResponseSizeBytes `
    -replace '\$\(WebViewTrustedDomain1\)', $WebViewTrustedDomains[0] `
    -replace '\$\(WebViewTrustedDomain2\)', $WebViewTrustedDomains[1] `
    -replace '\$\(WebViewTrustedDomain3\)', $WebViewTrustedDomains[2] `
    -replace '\$\(WebViewAllowHttp\)', $WebViewAllowHttp.ToString().ToLower() `
    -replace '\$\(SecurityGracePeriodDays\)', $SecurityGracePeriodDays `
    -replace '\$\(SecurityMaxActivationAttempts\)', $SecurityMaxActivationAttempts `
    -replace '\$\(SecurityLockoutMinutes\)', $SecurityLockoutMinutes `
    -replace '\$\(SecurityActivationLookbackMinutes\)', $SecurityActivationLookbackMinutes `
    -replace '\$\(SecurityBackgroundVerificationIntervalMinutes\)', $SecurityBackgroundVerificationIntervalMinutes

$generatedContent | Out-File -FilePath "Build/BuildConfiguration.cs" -Encoding UTF8 -NoNewline

Write-Host "? BuildConfiguration.cs generated" -ForegroundColor Green
Write-Host ""

Write-Host "Building application..." -ForegroundColor Yellow

$buildArgs = @(
    "publish"
    "-c", $Configuration
    "-r", $runtimeId
    "-p:IFrameUrl=`"$IFrameUrl`""
    "-p:ApiUrl=`"$ApiUrl`""
    "-p:AppName=`"$AppName`""
)

if ($SelfContained) {
    $buildArgs += "--self-contained"
    $buildArgs += "-p:PublishSingleFile=true"
    $buildArgs += "-p:IncludeNativeLibrariesForSelfExtract=true"
}

Write-Host "Command: dotnet $($buildArgs -join ' ')" -ForegroundColor Gray
Write-Host ""

& dotnet @buildArgs

if ($LASTEXITCODE -eq 0) {
    Write-Host ""
    Write-Host "========================================" -ForegroundColor Green
    Write-Host "  Build Successful! ?" -ForegroundColor Green
    Write-Host "========================================" -ForegroundColor Green
    Write-Host ""
    Write-Host "Output: bin/$Configuration/net9.0/$runtimeId/publish/" -ForegroundColor Cyan
} else {
    Write-Host ""
    Write-Host "========================================" -ForegroundColor Red
    Write-Host "  Build Failed! ?" -ForegroundColor Red
    Write-Host "========================================" -ForegroundColor Red
    exit $LASTEXITCODE
}

