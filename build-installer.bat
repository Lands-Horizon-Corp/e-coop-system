@echo off
REM Build and Create Installer for ECoopSystem
REM This script builds the application and generates the Inno Setup installer

echo ========================================
echo ECoopSystem - Build and Create Installer
echo ========================================
echo.

REM Check if Inno Setup is installed
where iscc >nul 2>nul
if %ERRORLEVEL% NEQ 0 (
    echo [ERROR] Inno Setup Compiler (iscc.exe) not found in PATH.
    echo.
    echo Please install Inno Setup from: https://jrsoftware.org/isdl.php
    echo After installation, add the Inno Setup installation directory to your PATH.
    echo Default location: C:\Program Files (x86)\Inno Setup 6
    echo.
    pause
    exit /b 1
)

echo [1/4] Cleaning previous build...
if exist "bin\Release" rmdir /s /q "bin\Release"
if exist "output\installer" rmdir /s /q "output\installer"

echo [2/4] Building application (Release - win-x64)...
echo.
dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=false -p:PublishTrimmed=false
if %ERRORLEVEL% NEQ 0 (
    echo [ERROR] Build failed!
    pause
    exit /b 1
)

echo.
echo [3/4] Build completed successfully!
echo Output directory: bin\Release\net9.0\win-x64\publish\
echo.

echo [4/4] Creating installer with Inno Setup...
echo.
iscc installer.iss
if %ERRORLEVEL% NEQ 0 (
    echo [ERROR] Installer creation failed!
    pause
    exit /b 1
)

echo.
echo ========================================
echo SUCCESS!
echo ========================================
echo.
echo Installer created successfully!
echo Location: output\installer\
echo.
dir /b output\installer\*.exe
echo.
pause
