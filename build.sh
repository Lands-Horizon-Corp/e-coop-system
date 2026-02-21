#!/bin/bash
# ECoopSystem Build Script for Linux/macOS
# Usage: ./build.sh [iframe-url] [platform]

IFRAME_URL="${1:-https://e-coop-client-development.up.railway.app/}"
API_URL="${2:-https://e-coop-server-development.up.railway.app/}"
PLATFORM="${3:-linux}"

echo "========================================="
echo " ECoopSystem Build Script"
echo "========================================="
echo ""
echo "IFrame URL: $IFRAME_URL"
echo "API URL:    $API_URL"
echo "Platform:   $PLATFORM"
echo ""

# Determine runtime ID
case "$PLATFORM" in
    windows)
        RID="win-x64"
        ;;
    linux)
        RID="linux-x64"
        ;;
    linux-deb)
        RID="linux-x64"
        ;;
    linux-arm)
        RID="linux-arm64"
        ;;
    mac-intel)
        RID="osx-x64"
        ;;
    mac-arm)
        RID="osx-arm64"
        ;;
    *)
        echo "Error: Unknown platform '$PLATFORM'"
        echo "Supported platforms: windows, linux, linux-deb, linux-arm, mac-intel, mac-arm"
        exit 1
        ;;
esac

echo "Generating BuildConfiguration.cs..."
sed -e "s|\$(IFrameUrl)|$IFRAME_URL|g" \
    -e "s|\$(ApiUrl)|$API_URL|g" \
    -e "s|\$(AppName)|ECoopSystem|g" \
    -e "s|\$(AppLogo)|Assets/Images/logo.png|g" \
    Build/BuildConfiguration.template.cs > Build/BuildConfiguration.cs

echo "? Configuration generated"
echo ""

echo "Building for $RID..."
dotnet publish -c Release -r "$RID" \
    --self-contained \
    -p:PublishSingleFile=true \
    -p:IncludeNativeLibrariesForSelfExtract=true \
    -p:IFrameUrl="$IFRAME_URL" \
    -p:ApiUrl="$API_URL"

if [ $? -eq 0 ]; then
    echo ""
    echo "========================================="
    echo " Build Successful! ?"
    echo "========================================="
    echo ""
    echo "Output: bin/Release/net9.0/$RID/publish/"
else
    echo ""
    echo "========================================="
    echo " Build Failed! ?"
    echo "========================================="
    exit 1
fi
