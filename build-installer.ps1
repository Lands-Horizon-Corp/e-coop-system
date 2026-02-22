#!/usr/bin/env pwsh
# Build Windows installer using Inno Setup

param(
    [string]$IFrameUrl = "https://e-coop-client-development.up.railway.app/",
    [string]$ApiUrl = "https://e-coop-server-development.up.railway.app/",
    [string]$AppName = "ECoopSystem",
    [string]$Version = "1.0.0",
    [string]$ProjectFile = "ECoopSystem.csproj",
    [string]$InnoSetupPath = "C:\Program Files (x86)\Inno Setup 6\ISCC.exe"
)

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "  ECoopSystem Installer Builder" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

# Verify project file exists
if (-not (Test-Path $ProjectFile)) {
    Write-Host "Error: Project file not found: $ProjectFile" -ForegroundColor Red
    Write-Host "Available .csproj files:" -ForegroundColor Yellow
    Get-ChildItem -Filter "*.csproj" -Recurse -Depth 1 | ForEach-Object { Write-Host "  - $($_.FullName)" -ForegroundColor White }
    exit 1
}

Write-Host "Using project: $ProjectFile" -ForegroundColor White
Write-Host ""

# Step 1: Generate BuildConfiguration.cs
Write-Host "Step 1: Generating BuildConfiguration.cs..." -ForegroundColor Yellow

$templateContent = Get-Content "Build/BuildConfiguration.template.cs" -Raw
$generatedContent = $templateContent `
    -replace '\$\(IFrameUrl\)', $IFrameUrl `
    -replace '\$\(ApiUrl\)', $ApiUrl `
    -replace '\$\(AppName\)', $AppName `
    -replace '\$\(AppLogo\)', 'Assets/Images/logo.png'

$generatedContent | Out-File -FilePath "Build/BuildConfiguration.cs" -Encoding UTF8 -NoNewline

Write-Host "✓ BuildConfiguration.cs generated" -ForegroundColor Green
Write-Host ""

# Step 2: Build the application
Write-Host "Step 2: Building application..." -ForegroundColor Yellow
Write-Host "Project: $ProjectFile" -ForegroundColor White
Write-Host "Platform: win-x64" -ForegroundColor White
Write-Host "Configuration: Release" -ForegroundColor White
Write-Host ""

$buildArgs = @(
    "publish"
    $ProjectFile
    "-c", "Release"
    "-r", "win-x64"
    "--self-contained"
    "-p:PublishSingleFile=true"
    "-p:IncludeNativeLibrariesForSelfExtract=true"
    "-p:IFrameUrl=`"$IFrameUrl`""
    "-p:ApiUrl=`"$ApiUrl`""
    "-p:AppName=`"$AppName`""
)

Write-Host "Command: dotnet $($buildArgs -join ' ')" -ForegroundColor Gray
Write-Host ""

& dotnet @buildArgs

if ($LASTEXITCODE -ne 0) {
    Write-Host ""
    Write-Host "========================================" -ForegroundColor Red
    Write-Host "  Build Failed! ✗" -ForegroundColor Red
    Write-Host "========================================" -ForegroundColor Red
    exit 1
}

Write-Host ""
Write-Host "✓ Application built successfully" -ForegroundColor Green
Write-Host ""

# Step 3: Verify build output
Write-Host "Step 3: Verifying build output..." -ForegroundColor Yellow
$publishPath = "bin\Release\net9.0\win-x64\publish"
$exePath = "$publishPath\$AppName.exe"

if (-not (Test-Path $exePath)) {
    Write-Host "Error: Executable not found at: $exePath" -ForegroundColor Red
    Write-Host "Contents of publish directory:" -ForegroundColor Yellow
    if (Test-Path $publishPath) {
        Get-ChildItem $publishPath | ForEach-Object { Write-Host "  - $($_.Name)" -ForegroundColor White }
    } else {
        Write-Host "  Publish directory does not exist!" -ForegroundColor Red
    }
    exit 1
}

Write-Host "✓ Build output verified: $exePath" -ForegroundColor Green
Write-Host ""

# Step 4: Check Inno Setup
Write-Host "Step 4: Checking Inno Setup..." -ForegroundColor Yellow

if (-not (Test-Path $InnoSetupPath)) {
    Write-Host "Error: Inno Setup not found at: $InnoSetupPath" -ForegroundColor Red
    Write-Host "Searching for Inno Setup in common locations..." -ForegroundColor Yellow
    
    $altPaths = @(
        "C:\Program Files\Inno Setup 6\ISCC.exe",
        "$env:ProgramFiles\Inno Setup 6\ISCC.exe",
        "${env:ProgramFiles(x86)}\Inno Setup 6\ISCC.exe",
        "C:\Program Files (x86)\Inno Setup 5\ISCC.exe"
    )
    
    $found = $false
    foreach ($path in $altPaths) {
        if (Test-Path $path) {
            Write-Host "✓ Found Inno Setup at: $path" -ForegroundColor Green
            $InnoSetupPath = $path
            $found = $true
            break
        }
    }
    
    if (-not $found) {
        Write-Host ""
        Write-Host "Please install Inno Setup from: https://jrsoftware.org/isdl.php" -ForegroundColor Yellow
        exit 1
    }
} else {
    Write-Host "✓ Inno Setup found: $InnoSetupPath" -ForegroundColor Green
}

Write-Host ""

# Step 5: Create installer directory
Write-Host "Step 5: Preparing installer directory..." -ForegroundColor Yellow
$installerDir = "installer\output"
if (-not (Test-Path $installerDir)) {
    New-Item -ItemType Directory -Path $installerDir -Force | Out-Null
}

Write-Host "✓ Installer directory ready: $installerDir" -ForegroundColor Green
Write-Host ""

# Step 6: Verify installer.iss exists
Write-Host "Step 6: Verifying Inno Setup script..." -ForegroundColor Yellow
if (-not (Test-Path "installer.iss")) {
    Write-Host "Error: installer.iss not found in current directory" -ForegroundColor Red
    exit 1
}

Write-Host "✓ Inno Setup script found: installer.iss" -ForegroundColor Green
Write-Host ""

# Step 7: Build installer
Write-Host "Step 7: Building installer..." -ForegroundColor Yellow
Write-Host "Version: $Version" -ForegroundColor White
Write-Host ""

& $InnoSetupPath "installer.iss" /DMyAppVersion=$Version

if ($LASTEXITCODE -eq 0) {
    Write-Host ""
    Write-Host "========================================" -ForegroundColor Green
    Write-Host "  Installer Created Successfully! ✓" -ForegroundColor Green
    Write-Host "========================================" -ForegroundColor Green
    Write-Host ""
    
    $installerFileName = "${AppName}-Setup-v${Version}.exe"
    $installerPath = Join-Path $installerDir $installerFileName
    
    if (Test-Path $installerPath) {
        Write-Host "Installer Details:" -ForegroundColor Cyan
        Write-Host "  Location: $installerPath" -ForegroundColor White
        
        $installerFile = Get-Item $installerPath
        $sizeMB = [math]::Round($installerFile.Length / 1MB, 2)
        Write-Host "  Size: $sizeMB MB" -ForegroundColor White
        
        # Calculate SHA256 hash
        try {
            $hash = (Get-FileHash -Path $installerPath -Algorithm SHA256).Hash
            Write-Host "  SHA256: $hash" -ForegroundColor Gray
        } catch {
            # Ignore hash calculation errors
        }
        
        Write-Host ""
        Write-Host "Ready for distribution!" -ForegroundColor Green
    } else {
        Write-Host "Warning: Installer file not found at: $installerPath" -ForegroundColor Yellow
        Write-Host "Checking installer output directory..." -ForegroundColor Yellow
        if (Test-Path $installerDir) {
            Get-ChildItem $installerDir | ForEach-Object { Write-Host "  Found: $($_.Name)" -ForegroundColor White }
        }
    }
} else {
    Write-Host ""
    Write-Host "========================================" -ForegroundColor Red
    Write-Host "  Installer Build Failed! ✗" -ForegroundColor Red
    Write-Host "========================================" -ForegroundColor Red
    Write-Host "Exit code: $LASTEXITCODE" -ForegroundColor Red
    exit $LASTEXITCODE
}