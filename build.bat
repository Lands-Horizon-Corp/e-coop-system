@echo off
REM ECoopSystem Build Wrapper for Windows
REM Usage: build.bat [iframe-url] [api-url] [platform]

setlocal

set IFRAME_URL=%~1
set API_URL=%~2
set PLATFORM=%~3

if "%IFRAME_URL%"=="" set IFRAME_URL=https://e-coop-client-development.up.railway.app/
if "%API_URL%"=="" set API_URL=https://e-coop-server-development.up.railway.app/
if "%PLATFORM%"=="" set PLATFORM=windows

echo =========================================
echo   ECoopSystem Build Wrapper
echo =========================================
echo.
echo IFrame URL: %IFRAME_URL%
echo API URL:    %API_URL%
echo Platform:   %PLATFORM%
echo.

powershell -ExecutionPolicy Bypass -File build.ps1 -IFrameUrl "%IFRAME_URL%" -ApiUrl "%API_URL%" -Platform %PLATFORM%

if %ERRORLEVEL% NEQ 0 (
    echo.
    echo Build failed!
    pause
    exit /b %ERRORLEVEL%
)

echo.
echo Build successful!
pause
