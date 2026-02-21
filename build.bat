@echo off
REM ECoopSystem Build Script for Windows
REM Usage: build.bat [iframe-url] [platform]

setlocal

set IFRAME_URL=%~1
set PLATFORM=%~2

if "%IFRAME_URL%"=="" set IFRAME_URL=https://e-coop-client-development.up.railway.app/
if "%PLATFORM%"=="" set PLATFORM=windows

echo =========================================
echo   ECoopSystem Build Script
echo =========================================
echo.
echo IFrame URL: %IFRAME_URL%
echo Platform:   %PLATFORM%
echo.

powershell -ExecutionPolicy Bypass -File build.ps1 -IFrameUrl "%IFRAME_URL%" -Platform %PLATFORM%

if %ERRORLEVEL% NEQ 0 (
    echo Build failed!
    exit /b %ERRORLEVEL%
)

echo.
echo Build successful!
pause
