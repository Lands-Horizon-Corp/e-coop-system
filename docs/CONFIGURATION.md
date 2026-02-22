# Configuration System Documentation

## Quick Reference

### User-Configurable Settings (appsettings.json)

The following settings can be modified by users without rebuilding:

```json
{
  "Application": {
    "Name": "ECoopSystem",
    "Version": "1.0.0",
    "MinimumLoadingTimeSeconds": 5,
    "WindowWidth": 1280,
    "WindowHeight": 720
  },
  "Logging": {
    "EnableDebugLogging": false,
    "LogLevel": "Warning"
  }
}
```

**File Locations:**
- Application directory (next to executable)
- Windows: `%APPDATA%\ECoopSystem\appsettings.json`
- Linux: `~/.config/ECoopSystem/appsettings.json`

### Build-Time Settings (Not User-Modifiable)

**API URLs, security settings, and trusted domains are compiled into the application binary** and cannot be modified after deployment. These settings are secure and protected from tampering.

To change these settings, rebuild the application:

```powershell
# Windows
./build.ps1 -ApiUrl "https://new-api.com" -IFrameUrl "https://new-client.com" -Platform windows

# Linux
make build API_URL=https://new-api.com IFRAME_URL=https://new-client.com PLATFORM=linux
```

**Build-time settings include:**
- API Base URL
- API Timeout, Retries, Response Size Limits
- WebView Base URL
- WebView Trusted Domains
- WebView HTTP/HTTPS Policy
- Security: Grace Period, Max Activation Attempts, Lockout Duration
- Security: Activation Lookback Period, Background Verification Interval

---

## Overview

ECoopSystem uses a **hybrid configuration system**:
- **User-configurable settings** in `appsettings.json` for UI preferences
- **Build-time compiled settings** in `BuildConfiguration` for security-critical values

This approach ensures sensitive settings cannot be tampered with by end users.

---

## Configuration Files

### `appsettings.json` (User-Configurable)
Contains only non-sensitive settings that users can safely modify:
- Application name and version
- Window dimensions and UI preferences  
- Logging levels and debug settings

### `BuildConfiguration.cs` (Compiled, Read-Only)
Generated at build time from `BuildConfiguration.template.cs`. Contains security-critical settings:
- API server URLs
- WebView trusted domains
- Security parameters (grace periods, lockout durations)
- HTTP timeout and retry policies

**Users cannot modify these settings** - they are compiled into the binary.

### `appsettings.Development.json` (Optional, Development Only)
Development overrides for `appsettings.json`. Applied automatically in Debug builds.

---

## User-Configurable Settings Structure

Only these sections appear in the user-accessible `appsettings.json`:


```json
{
  "Application": {
    "Name": "ECoopSystem",
    "Version": "1.0.0",
    "MinimumLoadingTimeSeconds": 5,
    "WindowWidth": 1280,
    "WindowHeight": 720
  },
  "Logging": {
    "EnableDebugLogging": false,
    "LogLevel": "Warning"
  }
}
```

**Note:** Sensitive settings (API URLs, security parameters) are **not** in this file. They are compiled into the application at build time.

---

## Build-Time Configuration Structure

These settings are defined in `Build/BuildConfiguration.template.cs` and generated during build:

```csharp
// API Settings (compiled into binary)
public const string ApiUrl = "https://api.yourserver.com/";
public const int ApiTimeout = 12;
public const int ApiMaxRetries = 3;
public const int ApiMaxResponseSizeBytes = 1048576;

// WebView Settings (compiled into binary)
public const string IFrameUrl = "https://app.yourserver.com/";
public static readonly string[] WebViewTrustedDomains = new[] { ... };
public const bool WebViewAllowHttp = false;

// Security Settings (compiled into binary)
public const int SecurityGracePeriodDays = 7;
public const int SecurityMaxActivationAttempts = 3;
public const int SecurityLockoutMinutes = 5;
```

To change these values, use build parameters:

```powershell
./build.ps1 `
    -ApiUrl "https://api.production.com" `
    -IFrameUrl "https://app.production.com" `
    -ApiTimeout 30 `
    -SecurityGracePeriodDays 14 `
    -Platform windows
```

---

## Settings Reference

### User-Configurable Settings (appsettings.json)

#### Application

| Setting | Type | Default | Description |
|---------|------|---------|-------------|
| `Name` | string | `ECoopSystem` | Application display name |
| `Version` | string | `1.0.0` | Application version |
| `MinimumLoadingTimeSeconds` | int | `5` | Minimum loading screen duration |
| `WindowWidth` | int | `1280` | Default window width in pixels |
| `WindowHeight` | int | `720` | Default window height in pixels |

#### Logging

| Setting | Type | Default | Description |
|---------|------|---------|-------------|
| `EnableDebugLogging` | bool | `false` | Enable detailed debug logging |
| `LogLevel` | string | `Warning` | Minimum log level (Debug/Info/Warning/Error) |

### Build-Time Settings (BuildConfiguration - Not User-Modifiable)

#### ApiSettings

| Setting | Type | Default | Description |
|---------|------|---------|-------------|
| `ApiUrl` | string | (build-time) | API server base URL (compiled) |
| `ApiTimeout` | int | `12` | HTTP request timeout in seconds (compiled) |
| `ApiMaxRetries` | int | `3` | Maximum retry attempts (compiled) |
| `ApiMaxResponseSizeBytes` | int | `1048576` | Maximum API response size (compiled) |

#### WebViewSettings

| Setting | Type | Default | Description |
|---------|------|---------|-------------|
| `IFrameUrl` | string | (build-time) | WebView client URL (compiled) |
| `WebViewTrustedDomains` | array | (build-time) | Allowed navigation domains (compiled) |
| `WebViewAllowHttp` | bool | `false` | Allow HTTP connections (compiled) |

#### Security

| Setting | Type | Default | Description |
|---------|------|---------|-------------|
| `SecurityGracePeriodDays` | int | `7` | Days before license re-verification (compiled) |
| `SecurityMaxActivationAttempts` | int | `3` | Failed attempts before lockout (compiled) |
| `SecurityLockoutMinutes` | int | `5` | Lockout duration (compiled) |
| `SecurityActivationLookbackMinutes` | int | `1` | Time window for counting failures (compiled) |
| `SecurityBackgroundVerificationIntervalMinutes` | int | `1` | Re-verification interval (compiled) |

---

## Configuration Priority

Settings are loaded/applied in this order:

1. **BuildConfiguration (Highest Priority)** - Compiled into binary at build time
   - API URLs, security settings, WebView domains
   - Cannot be changed after compilation
   
2. **appsettings.json** - User-configurable settings
   - Application preferences, logging
   - Can be modified by users

3. **appsettings.Development.json** (Debug builds only)
   - Development overrides for user settings
   - Ignored in Release builds

**Security Note:** Sensitive settings in BuildConfiguration always take precedence and cannot be overridden by user configuration files.

---

## Usage Examples

### Example 1: Customize User Preferences

Edit `appsettings.json` (users can do this):
```json
{
  "Application": {
    "Name": "My Custom Name",
    "WindowWidth": 1920,
    "WindowHeight": 1080
  },
  "Logging": {
    "EnableDebugLogging": true,
    "LogLevel": "Debug"
  }
}
```

### Example 2: Production Build with Custom API

Rebuild with production settings (requires developer/build access):
```powershell
./build.ps1 `
    -ApiUrl "https://api.production.com" `
    -IFrameUrl "https://app.production.com" `
    -WebViewTrustedDomains @("production.com", "api.production.com", "cdn.production.com") `
    -SecurityGracePeriodDays 14 `
    -Platform windows
```

### Example 3: Development Environment

Edit `appsettings.Development.json` (debug builds only):
```json
{
  "Application": {
    "MinimumLoadingTimeSeconds": 0
  },
  "Logging": {
    "EnableDebugLogging": true,
    "LogLevel": "Debug"
  }
}
```

**Note:** API URLs and security settings cannot be changed in JSON files - they must be set at build time.

---

## Accessing Configuration in Code

### User-Configurable Settings
```csharp
using ECoopSystem.Configuration;

// Get user-configurable settings
var config = ConfigurationLoader.Current;

// Access user preferences
var appName = config.Application.Name;
var windowWidth = config.Application.WindowWidth;
var logLevel = config.Logging.LogLevel;

// Reload if user modified appsettings.json
ConfigurationLoader.Reload();
```

### Build-Time Compiled Settings
```csharp
using ECoopSystem.Build;

// Access secure, compiled settings
var apiUrl = BuildConfiguration.ApiUrl;
var iframeUrl = BuildConfiguration.IFrameUrl;
var gracePeriod = BuildConfiguration.SecurityGracePeriodDays;
var trustedDomains = BuildConfiguration.WebViewTrustedDomains;

// These values are constants/readonly and cannot be changed at runtime
```

---

## Deployment

### For End Users (No Rebuild Required)
Users can modify these files next to the executable or in the application data folder:
- `appsettings.json` - UI preferences, logging settings
- `appsettings.Development.json` - Development overrides (debug builds only)

**Locations:**
- Windows: `%APPDATA%\ECoopSystem\appsettings.json`
- Linux: `~/.config/ECoopSystem/appsettings.json`
- Or: Same directory as executable

### For Developers/Deployers (Build Required)
To change API URLs, security settings, or trusted domains:

1. **Development/Staging:**
   ```powershell
   ./build.ps1 -ApiUrl "https://api.staging.com" -IFrameUrl "https://app.staging.com" -Platform windows
   ```

2. **Production:**
   ```powershell
   ./build.ps1 `
       -ApiUrl "https://api.production.com" `
       -IFrameUrl "https://app.production.com" `
       -SecurityGracePeriodDays 30 `
       -WebViewTrustedDomains @("production.com", "api.production.com") `
       -Platform windows
   ```

3. **Using Make (Linux/macOS):**
   ```bash
   make build \
       API_URL=https://api.production.com \
       IFRAME_URL=https://app.production.com \
       PLATFORM=linux
   ```

### CI/CD Integration
Use environment variables or secrets:
```yaml
- name: Build Production
  run: |
    ./build.ps1 `
      -ApiUrl "${{ secrets.PROD_API_URL }}" `
      -IFrameUrl "${{ secrets.PROD_IFRAME_URL }}" `
      -SecurityGracePeriodDays 30 `
      -Platform windows
```

---

## Security Notes

?? **Important Security Considerations:**

1. **Never commit production secrets** to source control
   - Use build-time parameters for production URLs
   - Use environment variables in CI/CD
   - Use secure configuration management

2. **Protect configuration files**
   - Store in secure locations
   - Set appropriate file permissions
   - Don't include sensitive data in plain text

3. **Validate configuration**
   - Application validates all loaded settings
   - Falls back to safe defaults on errors
   - Logs configuration issues

---

## Troubleshooting

### Configuration not loading
- Check file exists in application directory or `%APPDATA%/ECoopSystem/`
- Verify JSON syntax is valid
- Check file permissions

### Settings not applied
- Verify correct environment (Debug vs Release)
- Check configuration priority order
- Review application logs for errors

### Build-time vs Runtime
- Build-time parameters (build.ps1) take highest priority
- Runtime files (appsettings.json) allow easy updates
- Combine both for maximum flexibility

---

## Related Documentation

- [Build System](BUILD.md) - Build-time configuration
- [Security Guide](../SECURITY.md) - Security best practices
- [Quick Build Reference](../QUICK_BUILD.md) - Build commands

---

**Last Updated:** 2025-02-20
