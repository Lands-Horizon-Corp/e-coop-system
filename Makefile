# ECoopSystem Makefile
# Usage: make build IFRAME_URL=https://yoursite.com PLATFORM=windows

# Default configuration
IFRAME_URL ?= https://e-coop-client-development.up.railway.app/
API_URL ?= https://e-coop-server-development.up.railway.app/
APP_NAME ?= ECoopSystem
APP_LOGO ?= Assets/Images/logo.png
PLATFORM ?= windows
CONFIG ?= Release

# API Settings (secure, compiled into binary)
API_TIMEOUT ?= 12
API_MAX_RETRIES ?= 3
API_MAX_RESPONSE_SIZE ?= 1048576

# WebView Settings (secure, compiled into binary)
WEBVIEW_DOMAIN1 ?= dev-client.example.com
WEBVIEW_DOMAIN2 ?= app.example.com
WEBVIEW_DOMAIN3 ?= api.example.com
WEBVIEW_ALLOW_HTTP ?= false

# Security Settings (secure, compiled into binary)
SECURITY_GRACE_PERIOD ?= 7
SECURITY_MAX_ACTIVATION_ATTEMPTS ?= 3
SECURITY_LOCKOUT_MINUTES ?= 5
SECURITY_ACTIVATION_LOOKBACK ?= 1
SECURITY_BG_VERIFICATION ?= 1

# Runtime identifiers
ifeq ($(PLATFORM),windows)
    RID = win-x64
else ifeq ($(PLATFORM),linux)
    RID = linux-x64
else ifeq ($(PLATFORM),linux-deb)
    RID = linux-x64
else ifeq ($(PLATFORM),linux-arm)
    RID = linux-arm64
else ifeq ($(PLATFORM),mac-intel)
    RID = osx-x64
else ifeq ($(PLATFORM),mac-arm)
    RID = osx-arm64
else
    $(error Unknown platform: $(PLATFORM))
endif

.PHONY: all build clean help generate-config

# Default target
all: build

# Generate BuildConfiguration.cs from template
generate-config:
	@echo "Generating BuildConfiguration.cs..."
	@sed -e 's|\$$(IFrameUrl)|$(IFRAME_URL)|g' \
	     -e 's|\$$(ApiUrl)|$(API_URL)|g' \
	     -e 's|\$$(AppName)|$(APP_NAME)|g' \
	     -e 's|\$$(AppLogo)|$(APP_LOGO)|g' \
	     -e 's|\$$(ApiTimeout)|$(API_TIMEOUT)|g' \
	     -e 's|\$$(ApiMaxRetries)|$(API_MAX_RETRIES)|g' \
	     -e 's|\$$(ApiMaxResponseSizeBytes)|$(API_MAX_RESPONSE_SIZE)|g' \
	     -e 's|\$$(WebViewTrustedDomain1)|$(WEBVIEW_DOMAIN1)|g' \
	     -e 's|\$$(WebViewTrustedDomain2)|$(WEBVIEW_DOMAIN2)|g' \
	     -e 's|\$$(WebViewTrustedDomain3)|$(WEBVIEW_DOMAIN3)|g' \
	     -e 's|\$$(WebViewAllowHttp)|$(WEBVIEW_ALLOW_HTTP)|g' \
	     -e 's|\$$(SecurityGracePeriodDays)|$(SECURITY_GRACE_PERIOD)|g' \
	     -e 's|\$$(SecurityMaxActivationAttempts)|$(SECURITY_MAX_ACTIVATION_ATTEMPTS)|g' \
	     -e 's|\$$(SecurityLockoutMinutes)|$(SECURITY_LOCKOUT_MINUTES)|g' \
	     -e 's|\$$(SecurityActivationLookbackMinutes)|$(SECURITY_ACTIVATION_LOOKBACK)|g' \
	     -e 's|\$$(SecurityBackgroundVerificationIntervalMinutes)|$(SECURITY_BG_VERIFICATION)|g' \
	     Build/BuildConfiguration.template.cs > Build/BuildConfiguration.cs
	@echo "? Configuration generated"

# Build the application
build: generate-config
	@echo "========================================="
	@echo " Building $(APP_NAME)"
	@echo "========================================="
	@echo "IFrame URL: $(IFRAME_URL)"
	@echo "API URL:    $(API_URL)"
	@echo "Platform:   $(PLATFORM) ($(RID))"
	@echo "Config:     $(CONFIG)"
	@echo ""
	@dotnet publish -c $(CONFIG) -r $(RID) \
		--self-contained \
		-p:PublishSingleFile=true \
		-p:IncludeNativeLibrariesForSelfExtract=true
	@echo ""
	@echo "? Build completed: bin/$(CONFIG)/net9.0/$(RID)/publish/"

# Clean build artifacts
clean:
	@echo "Cleaning build artifacts..."
	@rm -rf bin/ obj/ Build/BuildConfiguration.cs
	@echo "? Clean completed"

# Show help
help:
	@echo "ECoopSystem Build System"
	@echo ""
	@echo "Usage:"
	@echo "  make build IFRAME_URL=https://yoursite.com PLATFORM=windows"
	@echo ""
	@echo "Options:"
	@echo "  IFRAME_URL  - WebView URL (default: dev Railway URL)"
	@echo "  API_URL     - API Server URL (default: dev Railway URL)"
	@echo "  APP_NAME    - Application name (default: ECoopSystem)"
	@echo "  APP_LOGO    - Logo path (default: Assets/Images/logo.png)"
	@echo "  PLATFORM    - Target platform (windows|linux|linux-deb|linux-arm|mac-intel|mac-arm)"
	@echo "  CONFIG      - Build configuration (Debug|Release, default: Release)"
	@echo ""
	@echo "Examples:"
	@echo "  make build IFRAME_URL=https://ecoopsuite.com PLATFORM=windows"
	@echo "  make build IFRAME_URL=https://ecoopsuite.com PLATFORM=linux"
	@echo "  make build IFRAME_URL=https://ecoopsuite.com PLATFORM=mac-arm"
	@echo "  make clean"
