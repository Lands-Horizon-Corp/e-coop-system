#!/usr/bin/env pwsh
param(
    [string]$IFrameUrl = "http://localhost:3000/",
    [string]$ApiUrl = "http://localhost:5000",
    [string]$AppName = "ECoopSystem",
    [string]$AppLogo = "Assets/Images/logo.png",
    
    [int]$ApiTimeout = 12,
    [int]$ApiMaxRetries = 3,
    [int]$ApiMaxResponseSizeBytes = 1048576,
    
    [bool]$WebViewAllowHttp = $false,
    [string[]]$WebViewTrustedDomains = @("localhost", "127.0.0.1", ""),
    
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

function Get-DotEnvValue {
    param(
        [string]$Key
    )

    if (-not (Test-Path ".env")) {
        return $null
    }

    $line = Get-Content ".env" | Where-Object { $_ -match "^$Key=" } | Select-Object -Last 1
    if (-not $line) {
        return $null
    }

    return ($line -replace "^$Key=", "").Trim().Trim('"')
}

if (-not $PSBoundParameters.ContainsKey("IFrameUrl")) {
    $dotEnvIFrame = Get-DotEnvValue -Key "IFRAME_URL"
    if (-not [string]::IsNullOrWhiteSpace($dotEnvIFrame)) { $IFrameUrl = $dotEnvIFrame }
}

if (-not $PSBoundParameters.ContainsKey("ApiUrl")) {
    $dotEnvApi = Get-DotEnvValue -Key "API_URL"
    if (-not [string]::IsNullOrWhiteSpace($dotEnvApi)) { $ApiUrl = $dotEnvApi }
}

if (-not $PSBoundParameters.ContainsKey("AppName")) {
    $dotEnvAppName = Get-DotEnvValue -Key "APP_NAME"
    if (-not [string]::IsNullOrWhiteSpace($dotEnvAppName)) { $AppName = $dotEnvAppName }
}

if (-not $PSBoundParameters.ContainsKey("AppLogo")) {
    $dotEnvAppLogo = Get-DotEnvValue -Key "APP_LOGO"
    if (-not [string]::IsNullOrWhiteSpace($dotEnvAppLogo)) { $AppLogo = $dotEnvAppLogo }
}

if (-not $PSBoundParameters.ContainsKey("WebViewTrustedDomains")) {
    $dotEnvDomains = Get-DotEnvValue -Key "WEBVIEW_TRUSTED_DOMAINS"
    if (-not [string]::IsNullOrWhiteSpace($dotEnvDomains)) {
        $WebViewTrustedDomains = $dotEnvDomains.Split(',') | ForEach-Object { $_.Trim() }
    }
}

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
$generatedContent = $templateContent

$domain1 = if ($WebViewTrustedDomains.Count -gt 0) { $WebViewTrustedDomains[0] } else { "" }
$domain2 = if ($WebViewTrustedDomains.Count -gt 1) { $WebViewTrustedDomains[1] } else { "" }
$domain3 = if ($WebViewTrustedDomains.Count -gt 2) { $WebViewTrustedDomains[2] } else { "" }

$generatedContent = $generatedContent.Replace('$(IFrameUrl)', $IFrameUrl)
$generatedContent = $generatedContent.Replace('$(ApiUrl)', $ApiUrl)
$generatedContent = $generatedContent.Replace('$(AppName)', $AppName)
$generatedContent = $generatedContent.Replace('$(AppLogo)', $AppLogo)
$generatedContent = $generatedContent.Replace('$(ApiTimeout)', $ApiTimeout.ToString())
$generatedContent = $generatedContent.Replace('$(ApiMaxRetries)', $ApiMaxRetries.ToString())
$generatedContent = $generatedContent.Replace('$(ApiMaxResponseSizeBytes)', $ApiMaxResponseSizeBytes.ToString())
$generatedContent = $generatedContent.Replace('$(WebViewTrustedDomain1)', $domain1)
$generatedContent = $generatedContent.Replace('$(WebViewTrustedDomain2)', $domain2)
$generatedContent = $generatedContent.Replace('$(WebViewTrustedDomain3)', $domain3)
$generatedContent = $generatedContent.Replace('$(WebViewAllowHttp)', $WebViewAllowHttp.ToString().ToLower())
$generatedContent = $generatedContent.Replace('$(SecurityGracePeriodDays)', $SecurityGracePeriodDays.ToString())
$generatedContent = $generatedContent.Replace('$(SecurityMaxActivationAttempts)', $SecurityMaxActivationAttempts.ToString())
$generatedContent = $generatedContent.Replace('$(SecurityLockoutMinutes)', $SecurityLockoutMinutes.ToString())
$generatedContent = $generatedContent.Replace('$(SecurityActivationLookbackMinutes)', $SecurityActivationLookbackMinutes.ToString())
$generatedContent = $generatedContent.Replace('$(SecurityBackgroundVerificationIntervalMinutes)', $SecurityBackgroundVerificationIntervalMinutes.ToString())

$generatedContent | Out-File -FilePath "Build/BuildConfiguration.cs" -Encoding UTF8 -NoNewline

Write-Host "BuildConfiguration.cs generated" -ForegroundColor Green
Write-Host ""

Write-Host "Building application..." -ForegroundColor Yellow

$buildArgs = @(
    "publish"
    "-c", $Configuration
    "-r", $runtimeId
    "-p:AppName=`"$AppName`""
)

if ($SelfContained) {
    $buildArgs += "--self-contained"
    # Note: PublishSingleFile is disabled because WebView/CEF requires external files
    # CEF (Chromium Embedded Framework) cannot run from a single extracted file
    # $buildArgs += "-p:PublishSingleFile=true"
    # $buildArgs += "-p:IncludeNativeLibrariesForSelfExtract=true"
}

Write-Host "Command: dotnet $($buildArgs -join ' ')" -ForegroundColor Gray
Write-Host ""

& dotnet @buildArgs

if ($LASTEXITCODE -eq 0) {
    Write-Host ""
    Write-Host "========================================" -ForegroundColor Green
    Write-Host "  Build Successful!" -ForegroundColor Green
    Write-Host "========================================" -ForegroundColor Green
    Write-Host ""
    Write-Host "Output: bin/$Configuration/net9.0/$runtimeId/publish/" -ForegroundColor Cyan
} else {
    Write-Host ""
    Write-Host "========================================" -ForegroundColor Red
    Write-Host "  Build Failed!" -ForegroundColor Red
    Write-Host "========================================" -ForegroundColor Red
    exit $LASTEXITCODE
}

