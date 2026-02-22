#!/bin/bash
# ECoopSystem Build Script for Linux/macOS
# Usage: ./build.sh [iframe-url] [api-url] [platform] [configuration]

set -e  # Exit on error

IFRAME_URL="${1:-https://e-coop-client-development.up.railway.app/}"
API_URL="${2:-https://e-coop-server-development.up.railway.app/}"
PLATFORM="${3:-linux}"
CONFIGURATION="${4:-Release}"

# API & Security Settings (defaults)
API_TIMEOUT=12
API_MAX_RETRIES=3
API_MAX_RESPONSE_SIZE=1048576

SECURITY_GRACE_PERIOD=7
SECURITY_MAX_ACTIVATION_ATTEMPTS=3
SECURITY_LOCKOUT_MINUTES=5
SECURITY_ACTIVATION_LOOKBACK=1
SECURITY_BG_VERIFICATION=1

# WebView Trusted Domains
WEBVIEW_DOMAIN1="dev-client.example.com"
WEBVIEW_DOMAIN2="app.example.com"
WEBVIEW_DOMAIN3="api.example.com"
WEBVIEW_ALLOW_HTTP="false"

echo "========================================="
echo " ECoopSystem Build Script"
echo "========================================="
echo "IFrame URL: $IFRAME_URL"
echo "API URL:    $API_URL"
echo "Platform:   $PLATFORM"
echo "Config:     $CONFIGURATION"
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
    -e "s|\$(ApiTimeout)|$API_TIMEOUT|g" \
    -e "s|\$(ApiMaxRetries)|$API_MAX_RETRIES|g" \
    -e "s|\$(ApiMaxResponseSizeBytes)|$API_MAX_RESPONSE_SIZE|g" \
    -e "s|\$(WebViewTrustedDomain1)|$WEBVIEW_DOMAIN1|g" \
    -e "s|\$(WebViewTrustedDomain2)|$WEBVIEW_DOMAIN2|g" \
    -e "s|\$(WebViewTrustedDomain3)|$WEBVIEW_DOMAIN3|g" \
    -e "s|\$(WebViewAllowHttp)|$WEBVIEW_ALLOW_HTTP|g" \
    -e "s|\$(SecurityGracePeriodDays)|$SECURITY_GRACE_PERIOD|g" \
    -e "s|\$(SecurityMaxActivationAttempts)|$SECURITY_MAX_ACTIVATION_ATTEMPTS|g" \
    -e "s|\$(SecurityLockoutMinutes)|$SECURITY_LOCKOUT_MINUTES|g" \
    -e "s|\$(SecurityActivationLookbackMinutes)|$SECURITY_ACTIVATION_LOOKBACK|g" \
    -e "s|\$(SecurityBackgroundVerificationIntervalMinutes)|$SECURITY_BG_VERIFICATION|g" \
    Build/BuildConfiguration.template.cs > Build/BuildConfiguration.cs

echo "? Configuration generated"
echo ""

echo "Building for $RID..."
dotnet publish -c $CONFIGURATION -r "$RID" \
    --self-contained \
    -p:PublishSingleFile=true \
    -p:IncludeNativeLibrariesForSelfExtract=true

if [ $? -eq 0 ]; then
    OUTPUT_DIR="bin/$CONFIGURATION/net9.0/$RID/publish"
    
    # Set executable permission on Linux/macOS
    if [ "$PLATFORM" != "windows" ]; then
        chmod +x "$OUTPUT_DIR/ECoopSystem"
    fi
    
    echo ""
    echo "========================================="
    echo " Build Successful! ?"
    echo "========================================="
    echo "Output: $OUTPUT_DIR"
    
    if command -v du &> /dev/null; then
        SIZE=$(du -sh "$OUTPUT_DIR" | cut -f1)
        echo "Size: $SIZE"
    fi
else
    echo ""
    echo "========================================="
    echo " Build Failed! ?"
    echo "========================================="
    exit 1
fi
