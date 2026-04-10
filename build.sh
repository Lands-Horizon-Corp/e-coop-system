#!/bin/bash
# ECoopSystem Build Script for Linux/macOS
# Usage: ./build.sh [iframe-url] [api-url] [platform] [configuration]

set -e  # Exit on error

get_env_value() {
    local key="$1"
    if [ ! -f ".env" ]; then
        return
    fi
    sed -n "s/^${key}=//p" .env | tail -n1
}

ENV_IFRAME_URL="$(get_env_value IFRAME_URL)"
ENV_API_URL="$(get_env_value API_URL)"
ENV_APP_NAME="$(get_env_value APP_NAME)"
ENV_APP_LOGO="$(get_env_value APP_LOGO)"
ENV_API_TIMEOUT="$(get_env_value API_TIMEOUT)"
ENV_API_MAX_RETRIES="$(get_env_value API_MAX_RETRIES)"
ENV_API_MAX_RESPONSE_SIZE="$(get_env_value API_MAX_RESPONSE_SIZE_BYTES)"
ENV_WEBVIEW_DOMAINS="$(get_env_value WEBVIEW_TRUSTED_DOMAINS)"
ENV_WEBVIEW_ALLOW_HTTP="$(get_env_value WEBVIEW_ALLOW_HTTP)"
ENV_SECURITY_GRACE_PERIOD="$(get_env_value SECURITY_GRACE_PERIOD_DAYS)"
ENV_SECURITY_MAX_ACTIVATION_ATTEMPTS="$(get_env_value SECURITY_MAX_ACTIVATION_ATTEMPTS)"
ENV_SECURITY_LOCKOUT_MINUTES="$(get_env_value SECURITY_LOCKOUT_MINUTES)"
ENV_SECURITY_ACTIVATION_LOOKBACK="$(get_env_value SECURITY_ACTIVATION_LOOKBACK_MINUTES)"
ENV_SECURITY_BG_VERIFICATION="$(get_env_value SECURITY_BACKGROUND_VERIFICATION_INTERVAL_MINUTES)"

IFRAME_URL="${1:-${ENV_IFRAME_URL:-http://localhost:3000/}}"
API_URL="${2:-${ENV_API_URL:-http://localhost:5000}}"
PLATFORM="${3:-linux}"
CONFIGURATION="${4:-Release}"

# API & Security Settings (defaults)
API_TIMEOUT="${ENV_API_TIMEOUT:-12}"
API_MAX_RETRIES="${ENV_API_MAX_RETRIES:-3}"
API_MAX_RESPONSE_SIZE="${ENV_API_MAX_RESPONSE_SIZE:-1048576}"

SECURITY_GRACE_PERIOD="${ENV_SECURITY_GRACE_PERIOD:-7}"
SECURITY_MAX_ACTIVATION_ATTEMPTS="${ENV_SECURITY_MAX_ACTIVATION_ATTEMPTS:-3}"
SECURITY_LOCKOUT_MINUTES="${ENV_SECURITY_LOCKOUT_MINUTES:-5}"
SECURITY_ACTIVATION_LOOKBACK="${ENV_SECURITY_ACTIVATION_LOOKBACK:-1}"
SECURITY_BG_VERIFICATION="${ENV_SECURITY_BG_VERIFICATION:-1}"

APP_NAME="${ENV_APP_NAME:-ECoopSystem}"
APP_LOGO="${ENV_APP_LOGO:-Assets/Images/logo.png}"

WEBVIEW_DOMAIN1="localhost"
WEBVIEW_DOMAIN2="127.0.0.1"
WEBVIEW_DOMAIN3=""
if [ -n "$ENV_WEBVIEW_DOMAINS" ]; then
    WEBVIEW_DOMAIN1="$(echo "$ENV_WEBVIEW_DOMAINS" | cut -d, -f1 | xargs)"
    WEBVIEW_DOMAIN2="$(echo "$ENV_WEBVIEW_DOMAINS" | cut -d, -f2 | xargs)"
    WEBVIEW_DOMAIN3="$(echo "$ENV_WEBVIEW_DOMAINS" | cut -d, -f3 | xargs)"
fi

# WebView policy (default)
WEBVIEW_ALLOW_HTTP="${ENV_WEBVIEW_ALLOW_HTTP:-false}"

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
    -e "s|\$(AppName)|$APP_NAME|g" \
    -e "s|\$(AppLogo)|$APP_LOGO|g" \
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

echo "Configuration generated"
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
    echo " Build Successful!"
    echo "========================================="
    echo "Output: $OUTPUT_DIR"
    
    if command -v du &> /dev/null; then
        SIZE=$(du -sh "$OUTPUT_DIR" | cut -f1)
        echo "Size: $SIZE"
    fi
else
    echo ""
    echo "========================================="
    echo " Build Failed!"
    echo "========================================="
    exit 1
fi
