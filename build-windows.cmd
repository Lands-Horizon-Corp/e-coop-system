@echo off
REM Build script for Windows deployment

setlocal enabledelayedexpansion

echo ======================================
echo ECoopSystem Windows Build Script
echo ======================================
echo.

REM Configuration
set PROJECT_NAME=ECoopSystem
set RUNTIME=win-x64
set CONFIGURATION=Release
set OUTPUT_DIR=.\publish\win-x64

REM Clean previous builds
echo Cleaning previous builds...
if exist "%OUTPUT_DIR%" (
    rmdir /s /q "%OUTPUT_DIR%"
)

REM Restore dependencies
echo Restoring dependencies...
dotnet restore
if errorlevel 1 goto :error

REM Build project
echo Building project...
dotnet build -c %CONFIGURATION%
if errorlevel 1 goto :error

REM Run tests if they exist
if exist "Tests" (
    echo Running tests...
    dotnet test -c %CONFIGURATION% --no-build
    if errorlevel 1 goto :error
)
if exist "tests" (
    echo Running tests...
    dotnet test -c %CONFIGURATION% --no-build
    if errorlevel 1 goto :error
)

REM Publish application
echo Publishing application for %RUNTIME%...
dotnet publish -c %CONFIGURATION% -r %RUNTIME% ^
    --self-contained ^
    -p:PublishSingleFile=true ^
    -p:PublishTrimmed=false ^
    -p:IncludeNativeLibrariesForSelfExtract=true ^
    -o "%OUTPUT_DIR%"
if errorlevel 1 goto :error

REM Copy configuration files
echo Copying configuration files...
copy /y appsettings*.json "%OUTPUT_DIR%\" >nul 2>&1

REM Display build info
echo.
echo ======================================
echo Build completed successfully!
echo ======================================
echo Output directory: %OUTPUT_DIR%
echo Executable: %OUTPUT_DIR%\%PROJECT_NAME%.exe
echo.
echo To run the application:
echo   cd %OUTPUT_DIR%
echo   %PROJECT_NAME%.exe
echo.

REM Display size
for /f "tokens=3" %%a in ('dir /s /-c "%OUTPUT_DIR%" ^| findstr /i "bytes"') do set SIZE=%%a
echo Total size: !SIZE! bytes
echo.
echo ======================================

goto :end

:error
echo.
echo ======================================
echo Build FAILED!
echo ======================================
exit /b 1

:end
endlocal
