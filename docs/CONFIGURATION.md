# Configuration System Documentation

## Overview

ECoopSystem now uses a flexible configuration system based on `appsettings.json` files. This allows you to change settings without recompiling the application.

---

## Configuration Files

### `appsettings.json` (Production/Base)
Main configuration file used in production builds.

### `appsettings.Development.json` (Development)
Overrides for development environment. Applied automatically in Debug builds.

### `appsettings.Production.json` (Optional)
Additional overrides for production environment. Applied in Release builds.

---

## Configuration Structure

```json
{
  "ApiSettings": {
    "BaseUrl": "https://api.yourserver.com/",
    "Timeout": 12,
    "MaxRetries": 3,
    "MaxResponseSizeBytes": 1048576
  },
  "WebViewSettings": {
    "BaseUrl": "https://app.yourserver.com/",
    "TrustedDomains": [
      "yourserver.com",
      "api.yourserver.com"
    ],
    "AllowHttp": false
  },
  "Security": {
    "GracePeriodDays": 7,
    "MaxActivationAttempts": 3,
    "LockoutMinutes": 5,
    "ActivationLookbackMinutes": 1
  },
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

---

## Settings Reference

### ApiSettings

| Setting | Type | Default | Description |
|---------|------|---------|-------------|
| `BaseUrl` | string | `https://api.example.com/` | API server base URL |
| `Timeout` | int | `12` | HTTP request timeout in seconds |
| `MaxRetries` | int | `3` | Maximum retry attempts for failed requests |
| `MaxResponseSizeBytes` | int | `1048576` | Maximum API response size (1MB) |

### WebViewSettings

| Setting | Type | Default | Description |
|---------|------|---------|-------------|
| `BaseUrl` | string | `https://app.example.com/` | WebView client URL |
| `TrustedDomains` | array | see below | List of domains the WebView can navigate to |
| `AllowHttp` | bool | `false` | Whether to allow HTTP connections (HTTPS only in production) |

**Default Trusted Domains:**
- `dev-client.example.com`
- `app.example.com`
- `api.example.com`

### Security

| Setting | Type | Default | Description |
|---------|------|---------|-------------|
| `GracePeriodDays` | int | `7` | Days before license re-verification required |
| `MaxActivationAttempts` | int | `3` | Failed activation attempts before lockout |
| `LockoutMinutes` | int | `5` | Lockout duration after too many failures |
| `ActivationLookbackMinutes` | int | `1` | Time window for counting failed attempts |

### Application

| Setting | Type | Default | Description |
|---------|------|---------|-------------|
| `Name` | string | `ECoopSystem` | Application display name |
| `Version` | string | `1.0.0` | Application version |
| `MinimumLoadingTimeSeconds` | int | `5` | Minimum loading screen duration |
| `WindowWidth` | int | `1280` | Default window width in pixels |
| `WindowHeight` | int | `720` | Default window height in pixels |

### Logging

| Setting | Type | Default | Description |
|---------|------|---------|-------------|
| `EnableDebugLogging` | bool | `false` | Enable detailed debug logging |
| `LogLevel` | string | `Warning` | Minimum log level (Debug/Info/Warning/Error) |

---

## Configuration Priority

Settings are loaded in this order (later overrides earlier):

1. **appsettings.json** (base configuration)
2. **appsettings.Development.json** or **appsettings.Production.json** (environment-specific)
3. **Build-time configuration** (from build.ps1 parameters)

---

## Usage Examples

### Example 1: Custom Production Configuration

Create `appsettings.json`:
```json
{
  "ApiSettings": {
    "BaseUrl": "https://api.production.com/"
  },
  "WebViewSettings": {
    "BaseUrl": "https://app.production.com/",
    "TrustedDomains": [
      "production.com",
      "api.production.com"
    ]
  }
}
```

### Example 2: Development Overrides

Create `appsettings.Development.json`:
```json
{
  "ApiSettings": {
    "BaseUrl": "https://localhost:5000/",
    "Timeout": 30
  },
  "WebViewSettings": {
    "AllowHttp": true
  },
  "Logging": {
    "EnableDebugLogging": true,
    "LogLevel": "Debug"
  }
}
```

### Example 3: Staging Environment

Create `appsettings.Production.json`:
```json
{
  "ApiSettings": {
    "BaseUrl": "https://api.staging.com/"
  },
  "WebViewSettings": {
    "BaseUrl": "https://app.staging.com/"
  }
}
```

---

## Accessing Configuration in Code

```csharp
using ECoopSystem.Configuration;

// Get current configuration
var config = ConfigurationLoader.Current;

// Access settings
var apiUrl = config.ApiSettings.BaseUrl;
var timeout = config.ApiSettings.Timeout;
var trustedDomains = config.WebViewSettings.TrustedDomains;

// Reload configuration (if file changed)
ConfigurationLoader.Reload();
```

---

## Deployment

### Development
- Include `appsettings.json` and `appsettings.Development.json`
- Debug builds automatically use Development overrides

### Production
- Include `appsettings.json` only (or with `appsettings.Production.json`)
- Release builds use Production configuration
- Build-time parameters override file settings

### Custom Deployment
1. Copy your custom `appsettings.json` to:
   - Application directory
   - OR: `%APPDATA%/ECoopSystem/appsettings.json`
2. Application will load it automatically

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
