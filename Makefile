# ECoopSystem Makefile
# Usage: make build IFRAME_URL=http://localhost:3000 API_URL=http://localhost:5000 PLATFORM=windows

ENV_FILE := .env

DEFAULT_IFRAME_URL := $(shell [ -f $(ENV_FILE) ] && sed -n 's/^IFRAME_URL=//p' $(ENV_FILE) | tail -n1)
DEFAULT_API_URL := $(shell [ -f $(ENV_FILE) ] && sed -n 's/^API_URL=//p' $(ENV_FILE) | tail -n1)
DEFAULT_APP_NAME := $(shell [ -f $(ENV_FILE) ] && sed -n 's/^APP_NAME=//p' $(ENV_FILE) | tail -n1)
DEFAULT_APP_LOGO := $(shell [ -f $(ENV_FILE) ] && sed -n 's/^APP_LOGO=//p' $(ENV_FILE) | tail -n1)
DEFAULT_API_TIMEOUT := $(shell [ -f $(ENV_FILE) ] && sed -n 's/^API_TIMEOUT=//p' $(ENV_FILE) | tail -n1)
DEFAULT_API_MAX_RETRIES := $(shell [ -f $(ENV_FILE) ] && sed -n 's/^API_MAX_RETRIES=//p' $(ENV_FILE) | tail -n1)
DEFAULT_API_MAX_RESPONSE_SIZE := $(shell [ -f $(ENV_FILE) ] && sed -n 's/^API_MAX_RESPONSE_SIZE_BYTES=//p' $(ENV_FILE) | tail -n1)
DEFAULT_SECURITY_GRACE_PERIOD := $(shell [ -f $(ENV_FILE) ] && sed -n 's/^SECURITY_GRACE_PERIOD_DAYS=//p' $(ENV_FILE) | tail -n1)
DEFAULT_SECURITY_MAX_ACTIVATION_ATTEMPTS := $(shell [ -f $(ENV_FILE) ] && sed -n 's/^SECURITY_MAX_ACTIVATION_ATTEMPTS=//p' $(ENV_FILE) | tail -n1)
DEFAULT_SECURITY_LOCKOUT_MINUTES := $(shell [ -f $(ENV_FILE) ] && sed -n 's/^SECURITY_LOCKOUT_MINUTES=//p' $(ENV_FILE) | tail -n1)
DEFAULT_SECURITY_ACTIVATION_LOOKBACK := $(shell [ -f $(ENV_FILE) ] && sed -n 's/^SECURITY_ACTIVATION_LOOKBACK_MINUTES=//p' $(ENV_FILE) | tail -n1)
DEFAULT_SECURITY_BG_VERIFICATION := $(shell [ -f $(ENV_FILE) ] && sed -n 's/^SECURITY_BACKGROUND_VERIFICATION_INTERVAL_MINUTES=//p' $(ENV_FILE) | tail -n1)
DEFAULT_WEBVIEW_DOMAINS := $(shell [ -f $(ENV_FILE) ] && sed -n 's/^WEBVIEW_TRUSTED_DOMAINS=//p' $(ENV_FILE) | tail -n1)
DEFAULT_WEBVIEW_ALLOW_HTTP := $(shell [ -f $(ENV_FILE) ] && sed -n 's/^WEBVIEW_ALLOW_HTTP=//p' $(ENV_FILE) | tail -n1)
DEFAULT_WEBVIEW_DOMAIN1 := $(shell printf "%s" "$(DEFAULT_WEBVIEW_DOMAINS)" | cut -d, -f1 | xargs)
DEFAULT_WEBVIEW_DOMAIN2 := $(shell printf "%s" "$(DEFAULT_WEBVIEW_DOMAINS)" | cut -d, -f2 | xargs)
DEFAULT_WEBVIEW_DOMAIN3 := $(shell printf "%s" "$(DEFAULT_WEBVIEW_DOMAINS)" | cut -d, -f3 | xargs)

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
WEBVIEW_DOMAIN1 ?= $(if $(DEFAULT_WEBVIEW_DOMAIN1),$(DEFAULT_WEBVIEW_DOMAIN1),localhost)
WEBVIEW_DOMAIN2 ?= $(if $(DEFAULT_WEBVIEW_DOMAIN2),$(DEFAULT_WEBVIEW_DOMAIN2),127.0.0.1)
WEBVIEW_DOMAIN3 ?= $(if $(DEFAULT_WEBVIEW_DOMAIN3),$(DEFAULT_WEBVIEW_DOMAIN3),)
WEBVIEW_ALLOW_HTTP ?= $(if $(DEFAULT_WEBVIEW_ALLOW_HTTP),$(DEFAULT_WEBVIEW_ALLOW_HTTP),false)

# Security Settings (secure, compiled into binary)
SECURITY_GRACE_PERIOD ?= $(if $(DEFAULT_SECURITY_GRACE_PERIOD),$(DEFAULT_SECURITY_GRACE_PERIOD),7)
SECURITY_MAX_ACTIVATION_ATTEMPTS ?= $(if $(DEFAULT_SECURITY_MAX_ACTIVATION_ATTEMPTS),$(DEFAULT_SECURITY_MAX_ACTIVATION_ATTEMPTS),3)
SECURITY_LOCKOUT_MINUTES ?= $(if $(DEFAULT_SECURITY_LOCKOUT_MINUTES),$(DEFAULT_SECURITY_LOCKOUT_MINUTES),5)
SECURITY_ACTIVATION_LOOKBACK ?= $(if $(DEFAULT_SECURITY_ACTIVATION_LOOKBACK),$(DEFAULT_SECURITY_ACTIVATION_LOOKBACK),1)
SECURITY_BG_VERIFICATION ?= $(if $(DEFAULT_SECURITY_BG_VERIFICATION),$(DEFAULT_SECURITY_BG_VERIFICATION),1)

.PHONY: all build buildinstaller clean help generate-config prepare-output-dirs ensure-script-permissions

# Default target
all: build

# Ensure output directory structure exists
prepare-output-dirs:
	@mkdir -p output/build/windows output/build/linux output/build/macos
	@mkdir -p output/installer/windows output/installer/linux output/installer/macos

# Ensure shell scripts are executable when present (useful after cloning from Windows)
ensure-script-permissions:
	@for script in build-linux-installer.sh build-windows-installer.sh build.sh; do \
		if [ -f "$$script" ]; then chmod +x "$$script" || true; fi; \
	done

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
	@echo "Configuration generated"

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
		zipPath="output/build/$$osFolder/$(APP_NAME)-$$osFolder-$(VERSION).zip"; \
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
	@echo "Build completed"

# Build installer packages (Windows/Linux). macOS installer is not available yet.
buildinstaller: prepare-output-dirs ensure-script-permissions
	@echo "========================================="
	@echo " Building Installers $(APP_NAME)"
	@echo "========================================="
	@echo "IFrame URL: $(IFRAME_URL)"
	@echo "API URL:    $(API_URL)"
	@echo "Platform:   $(PLATFORM)"
	@echo "Config:     $(CONFIG)"
	@echo "Version:    $(VERSION)"
	@set -e; \
	strictMode=0; \
	if [ "$(PLATFORM)" = "all" ]; then \
		platforms="windows linux macos"; \
	else \
		platforms="$(PLATFORM)"; \
		strictMode=1; \
	fi; \
	for platform in $$platforms; do \
		case "$$platform" in \
			windows) \
				echo ""; \
				echo "Creating Windows installer..."; \
				mkdir -p output/installer/windows output/installer/linux output/installer/macos; \
				if [ -f "./build-windows-installer.sh" ]; then \
					if bash ./build-windows-installer.sh "$(VERSION)" "$(IFRAME_URL)" "$(API_URL)" "$(CONFIG)" false false; then :; else \
						echo "Warning: Windows installer build failed on this environment."; \
						if [ $$strictMode -eq 1 ]; then exit 1; else continue; fi; \
					fi; \
				elif command -v pwsh >/dev/null 2>&1; then \
					if pwsh -NoProfile -ExecutionPolicy Bypass -File ./build-windows-installer.ps1 -Version "$(VERSION)" -IFrameUrl "$(IFRAME_URL)" -ApiUrl "$(API_URL)" -Configuration "$(CONFIG)"; then :; else \
						echo "Warning: Windows installer build failed on this environment."; \
						if [ $$strictMode -eq 1 ]; then exit 1; else continue; fi; \
					fi; \
				else \
					echo "Error: Windows installer script requires ./build-windows-installer.sh or pwsh."; \
					if [ $$strictMode -eq 1 ]; then exit 1; else continue; fi; \
				fi; \
				if find output/installer -maxdepth 1 -type f -name '*.exe' | grep -q .; then \
					srcExe=$$(find output/installer -maxdepth 1 -type f -name '*.exe' | head -n1); \
					ext=$${srcExe##*.}; \
					destExe="output/installer/windows/$(APP_NAME)-windows-$(VERSION).$$ext"; \
					mv -f "$$srcExe" "$$destExe"; \
					echo "Windows installer moved to $$destExe"; \
				else \
					echo "Warning: No Windows installer files were found in output/installer."; \
					if [ $$strictMode -eq 1 ]; then exit 1; fi; \
				fi; \
				;; \
			linux|linux-deb|linux-arm) \
				echo ""; \
				echo "Creating Linux installer..."; \
				mkdir -p output/installer/windows output/installer/linux output/installer/macos; \
				if [ -f "./build-linux-installer.sh" ]; then \
					if bash ./build-linux-installer.sh "$(VERSION)" "$(IFRAME_URL)" "$(API_URL)" "$(CONFIG)"; then :; else \
						echo "Warning: Linux installer build failed on this environment."; \
						if [ $$strictMode -eq 1 ]; then exit 1; else continue; fi; \
					fi; \
				else \
					echo "Warning: ./build-linux-installer.sh not found."; \
					if [ $$strictMode -eq 1 ]; then exit 1; else continue; fi; \
				fi; \
				if find output/installer -maxdepth 1 -type f -name '*.deb' | grep -q .; then \
					srcDeb=$$(find output/installer -maxdepth 1 -type f -name '*.deb' | head -n1); \
					ext=$${srcDeb##*.}; \
					destDeb="output/installer/linux/$(APP_NAME)-linux-$(VERSION).$$ext"; \
					mv -f "$$srcDeb" "$$destDeb"; \
					echo "Linux installer moved to $$destDeb"; \
				else \
					echo "Warning: No Linux installer files were found in output/installer."; \
					if [ $$strictMode -eq 1 ]; then exit 1; fi; \
				fi; \
				;; \
			macos|mac-intel|mac-arm) \
				echo ""; \
				echo "Skipping $$platform installer: macOS installer script is not available yet."; \
				;; \
			*) echo "Error: Unknown platform '$$platform'"; exit 1 ;; \
		esac; \
	done
	@echo ""
	@echo "Installer build completed"

# Clean build artifacts
clean:
	@echo "Cleaning build artifacts..."
	@rm -rf bin/ obj/ Build/BuildConfiguration.cs
	@echo "Clean completed"

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
	@echo "  VERSION     - Artifact version used in build zip and installer names (default: 1.0.0)"
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
