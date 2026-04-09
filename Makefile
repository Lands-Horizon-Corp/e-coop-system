# ECoopSystem Makefile
# Usage: make build IFRAME_URL=http://localhost:3000 API_URL=http://localhost:5000 PLATFORM=windows

BUILD_CONFIGURATION_FILE := Build/BuildConfiguration.cs

DEFAULT_IFRAME_URL := $(shell sed -n 's/.*GetEnvOrDefault("IFRAME_URL", "\([^"]*\)").*/\1/p' $(BUILD_CONFIGURATION_FILE) | head -n1)
DEFAULT_API_URL := $(shell sed -n 's/.*GetEnvOrDefault("API_URL", "\([^"]*\)").*/\1/p' $(BUILD_CONFIGURATION_FILE) | head -n1)
DEFAULT_APP_NAME := $(shell sed -n 's/.*public const string AppName = "\([^"]*\)";.*/\1/p' $(BUILD_CONFIGURATION_FILE) | head -n1)
DEFAULT_APP_LOGO := $(shell sed -n 's/.*public const string AppLogo = "\([^"]*\)";.*/\1/p' $(BUILD_CONFIGURATION_FILE) | head -n1)
DEFAULT_API_TIMEOUT := $(shell sed -n 's/.*public const int ApiTimeout = \([0-9][0-9]*\);.*/\1/p' $(BUILD_CONFIGURATION_FILE) | head -n1)
DEFAULT_API_MAX_RETRIES := $(shell sed -n 's/.*public const int ApiMaxRetries = \([0-9][0-9]*\);.*/\1/p' $(BUILD_CONFIGURATION_FILE) | head -n1)
DEFAULT_API_MAX_RESPONSE_SIZE := $(shell sed -n 's/.*public const int ApiMaxResponseSizeBytes = \([0-9][0-9]*\);.*/\1/p' $(BUILD_CONFIGURATION_FILE) | head -n1)
DEFAULT_SECURITY_GRACE_PERIOD := $(shell sed -n 's/.*public const int SecurityGracePeriodDays = \([0-9][0-9]*\);.*/\1/p' $(BUILD_CONFIGURATION_FILE) | head -n1)
DEFAULT_SECURITY_MAX_ACTIVATION_ATTEMPTS := $(shell sed -n 's/.*public const int SecurityMaxActivationAttempts = \([0-9][0-9]*\);.*/\1/p' $(BUILD_CONFIGURATION_FILE) | head -n1)
DEFAULT_SECURITY_LOCKOUT_MINUTES := $(shell sed -n 's/.*public const int SecurityLockoutMinutes = \([0-9][0-9]*\);.*/\1/p' $(BUILD_CONFIGURATION_FILE) | head -n1)
DEFAULT_SECURITY_ACTIVATION_LOOKBACK := $(shell sed -n 's/.*public const int SecurityActivationLookbackMinutes = \([0-9][0-9]*\);.*/\1/p' $(BUILD_CONFIGURATION_FILE) | head -n1)
DEFAULT_SECURITY_BG_VERIFICATION := $(shell sed -n 's/.*public const int SecurityBackgroundVerificationIntervalMinutes = \([0-9][0-9]*\);.*/\1/p' $(BUILD_CONFIGURATION_FILE) | head -n1)

# Default configuration
IFRAME_URL ?= $(if $(DEFAULT_IFRAME_URL),$(DEFAULT_IFRAME_URL),http://localhost:3000/)
API_URL ?= $(if $(DEFAULT_API_URL),$(DEFAULT_API_URL),http://localhost:5000)
APP_NAME ?= $(if $(DEFAULT_APP_NAME),$(DEFAULT_APP_NAME),ECoopSystem)
APP_LOGO ?= $(if $(DEFAULT_APP_LOGO),$(DEFAULT_APP_LOGO),Assets/Images/logo.png)
VERSION ?= 1.0.0
PLATFORM ?= all
CONFIG ?= Release

# API Settings (secure, compiled into binary)
API_TIMEOUT ?= $(if $(DEFAULT_API_TIMEOUT),$(DEFAULT_API_TIMEOUT),12)
API_MAX_RETRIES ?= $(if $(DEFAULT_API_MAX_RETRIES),$(DEFAULT_API_MAX_RETRIES),3)
API_MAX_RESPONSE_SIZE ?= $(if $(DEFAULT_API_MAX_RESPONSE_SIZE),$(DEFAULT_API_MAX_RESPONSE_SIZE),1048576)

# WebView Settings (secure, compiled into binary)
WEBVIEW_DOMAIN1 ?= e-coop-client-development.up.railway.app
WEBVIEW_DOMAIN2 ?= e-coop-server-development.up.railway.app
WEBVIEW_DOMAIN3 ?= railway.app
WEBVIEW_ALLOW_HTTP ?= false

# Security Settings (secure, compiled into binary)
SECURITY_GRACE_PERIOD ?= $(if $(DEFAULT_SECURITY_GRACE_PERIOD),$(DEFAULT_SECURITY_GRACE_PERIOD),7)
SECURITY_MAX_ACTIVATION_ATTEMPTS ?= $(if $(DEFAULT_SECURITY_MAX_ACTIVATION_ATTEMPTS),$(DEFAULT_SECURITY_MAX_ACTIVATION_ATTEMPTS),3)
SECURITY_LOCKOUT_MINUTES ?= $(if $(DEFAULT_SECURITY_LOCKOUT_MINUTES),$(DEFAULT_SECURITY_LOCKOUT_MINUTES),5)
SECURITY_ACTIVATION_LOOKBACK ?= $(if $(DEFAULT_SECURITY_ACTIVATION_LOOKBACK),$(DEFAULT_SECURITY_ACTIVATION_LOOKBACK),1)
SECURITY_BG_VERIFICATION ?= $(if $(DEFAULT_SECURITY_BG_VERIFICATION),$(DEFAULT_SECURITY_BG_VERIFICATION),1)

.PHONY: all build buildinstaller clean help generate-config prepare-output-dirs

# Default target
all: build

# Ensure output directory structure exists
prepare-output-dirs:
	@mkdir -p output/build/windows output/build/linux output/build/macos
	@mkdir -p output/installer/windows output/installer/linux output/installer/macos

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
build: generate-config prepare-output-dirs
	@echo "========================================="
	@echo " Building $(APP_NAME)"
	@echo "========================================="
	@echo "IFrame URL: $(IFRAME_URL)"
	@echo "API URL:    $(API_URL)"
	@echo "Platform:   $(PLATFORM)"
	@echo "Config:     $(CONFIG)"
	@echo ""
	@echo "Note: Single-file publish disabled due to WebView/CEF requirements"
	@set -e; \
	if [ "$(PLATFORM)" = "all" ]; then \
		platforms="windows linux macos"; \
	else \
		platforms="$(PLATFORM)"; \
	fi; \
	for platform in $$platforms; do \
		case "$$platform" in \
			windows) rid="win-x64" ;; \
			linux|linux-deb) rid="linux-x64" ;; \
			linux-arm) rid="linux-arm64" ;; \
			macos|mac-intel) rid="osx-x64" ;; \
			mac-arm) rid="osx-arm64" ;; \
			*) echo "Error: Unknown platform '$$platform'"; exit 1 ;; \
		esac; \
		echo ""; \
		echo "Publishing for $$platform ($$rid)..."; \
		dotnet publish -c $(CONFIG) -r $$rid --self-contained; \
		osFolder="$$platform"; \
		if [ "$$platform" = "linux-deb" ] || [ "$$platform" = "linux-arm" ]; then osFolder="linux"; fi; \
		if [ "$$platform" = "mac-intel" ] || [ "$$platform" = "mac-arm" ]; then osFolder="macos"; fi; \
		mkdir -p output/build/$$osFolder; \
		zipPath="output/build/$$osFolder/$(APP_NAME)-$$osFolder-$(CONFIG).zip"; \
		rm -f "$$zipPath"; \
		if command -v zip >/dev/null 2>&1; then \
			( cd "bin/$(CONFIG)/net9.0/$$rid/publish" && zip -r "$(CURDIR)/$$zipPath" . >/dev/null ); \
		elif command -v pwsh >/dev/null 2>&1; then \
			pwsh -NoProfile -Command "Compress-Archive -Path 'bin/$(CONFIG)/net9.0/$$rid/publish/*' -DestinationPath '$$zipPath' -Force" >/dev/null; \
		elif command -v powershell.exe >/dev/null 2>&1; then \
			powershell.exe -NoProfile -Command "Compress-Archive -Path 'bin/$(CONFIG)/net9.0/$$rid/publish/*' -DestinationPath '$$zipPath' -Force" >/dev/null; \
		else \
			echo "Error: zip command not found. Install zip or PowerShell (pwsh)."; \
			exit 1; \
		fi; \
		echo "Created $$zipPath"; \
	done
	@echo ""
	@echo "? Build completed"

# Build installer packages (Windows/Linux). macOS installer is not available yet.
buildinstaller: prepare-output-dirs
	@echo "========================================="
	@echo " Building Installers $(APP_NAME)"
	@echo "========================================="
	@echo "IFrame URL: $(IFRAME_URL)"
	@echo "API URL:    $(API_URL)"
	@echo "Platform:   $(PLATFORM)"
	@echo "Config:     $(CONFIG)"
	@echo "Version:    $(VERSION)"
	@set -e; \
	if [ "$(PLATFORM)" = "all" ]; then \
		platforms="windows linux macos"; \
	else \
		platforms="$(PLATFORM)"; \
	fi; \
	for platform in $$platforms; do \
		case "$$platform" in \
			windows) \
				echo ""; \
				echo "Creating Windows installer..."; \
				if [ -x "./build-windows-installer.sh" ]; then \
					./build-windows-installer.sh "$(VERSION)" "$(IFRAME_URL)" "$(API_URL)" "$(CONFIG)" false false; \
				elif command -v pwsh >/dev/null 2>&1; then \
					pwsh -NoProfile -ExecutionPolicy Bypass -File ./build-windows-installer.ps1 -Version "$(VERSION)" -IFrameUrl "$(IFRAME_URL)" -ApiUrl "$(API_URL)" -Configuration "$(CONFIG)"; \
				else \
					echo "Error: Windows installer script requires ./build-windows-installer.sh or pwsh."; \
					exit 1; \
				fi; \
				find output/installer -maxdepth 1 -type f -name '*.exe' -exec cp -f {} output/installer/windows/ \; \
				echo "Windows installers copied to output/installer/windows"; \
				;; \
			linux|linux-deb|linux-arm) \
				echo ""; \
				echo "Creating Linux installer..."; \
				./build-linux-installer.sh "$(VERSION)" "$(IFRAME_URL)" "$(API_URL)" "$(CONFIG)"; \
				find output/installer -maxdepth 1 -type f -name '*.deb' -exec cp -f {} output/installer/linux/ \; \
				echo "Linux installers copied to output/installer/linux"; \
				;; \
			macos|mac-intel|mac-arm) \
				echo ""; \
				echo "Skipping $$platform installer: macOS installer script is not available yet."; \
				;; \
			*) echo "Error: Unknown platform '$$platform'"; exit 1 ;; \
		esac; \
	done
	@echo ""
	@echo "? Installer build completed"

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
	@echo "  make build IFRAME_URL=http://localhost:3000 API_URL=http://localhost:5000 PLATFORM=windows"
	@echo ""
	@echo "Options:"
	@echo "  IFRAME_URL  - WebView URL (default: dev Railway URL)"
	@echo "  API_URL     - API Server URL (default: dev Railway URL)"
	@echo "  APP_NAME    - Application name (default: ECoopSystem)"
	@echo "  APP_LOGO    - Logo path (default: Assets/Images/logo.png)"
	@echo "  VERSION     - Installer/package version (default: 1.0.0)"
	@echo "  PLATFORM    - Target platform (all|windows|linux|macos|linux-deb|linux-arm|mac-intel|mac-arm)"
	@echo "  CONFIG      - Build configuration (Debug|Release, default: Release)"
	@echo ""
	@echo "Examples:"
	@echo "  make build"
	@echo "  make build IFRAME_URL=http://localhost:3000 API_URL=http://localhost:5000"
	@echo "  make build PLATFORM=windows IFRAME_URL=http://localhost:3000 API_URL=http://localhost:5000"
	@echo "  make buildinstaller"
	@echo "  make buildinstaller PLATFORM=linux VERSION=1.0.0"
	@echo "  make clean"
