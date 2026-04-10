#!/bin/bash

# Build Windows installer from Bash (Git Bash / WSL)
# Wrapper for build-windows-installer.ps1
#
# Usage:
#   ./build-windows-installer.sh
#   ./build-windows-installer.sh 1.0.0
#   ./build-windows-installer.sh 1.0.0 "https://app.example.com" "https://api.example.com" Release
#   ./build-windows-installer.sh 1.0.0 "https://app.example.com" "https://api.example.com" Release true true

set -e

VERSION="${1:-1.0.0}"
IFRAME_URL="${2:-https://e-coop-client-development.up.railway.app/}"
API_URL="${3:-https://e-coop-server-development.up.railway.app/}"
CONFIGURATION="${4:-Release}"
SKIP_BUILD="${5:-false}"
OPEN_OUTPUT="${6:-false}"

SCRIPT_PATH="./build-windows-installer.ps1"

if [ ! -f "$SCRIPT_PATH" ]; then
  echo "Error: $SCRIPT_PATH not found"
  exit 1
fi

PS_CMD=""
if command -v pwsh >/dev/null 2>&1; then
  PS_CMD="pwsh"
elif command -v powershell.exe >/dev/null 2>&1; then
  PS_CMD="powershell.exe"
elif command -v powershell >/dev/null 2>&1; then
  PS_CMD="powershell"
else
  echo "Error: PowerShell not found (pwsh/powershell.exe)."
  exit 1
fi

echo "========================================"
echo "ECoopSystem - Windows Installer (Bash)"
echo "========================================"
echo ""
echo "Build Configuration:"
echo "  IFrame URL:      $IFRAME_URL"
echo "  API URL:         $API_URL"
echo "  Configuration:   $CONFIGURATION"
echo "  Version:         $VERSION"
echo "  Skip Build:      $SKIP_BUILD"
echo "  Open Output:     $OPEN_OUTPUT"
echo ""

ARGS=(
  "-NoProfile"
  "-ExecutionPolicy" "Bypass"
  "-File" "$SCRIPT_PATH"
  "-IFrameUrl" "$IFRAME_URL"
  "-ApiUrl" "$API_URL"
  "-Configuration" "$CONFIGURATION"
  "-Version" "$VERSION"
)

if [ "$SKIP_BUILD" = "true" ]; then
  ARGS+=("-SkipBuild")
fi

if [ "$OPEN_OUTPUT" = "true" ]; then
  ARGS+=("-OpenOutput")
fi

"$PS_CMD" "${ARGS[@]}"
