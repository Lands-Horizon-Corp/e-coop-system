# Configuration Guide

## Overview

The app configuration is split into:

1. `appsettings.json` for user-facing application settings.
2. `Build/BuildConfiguration.cs` for secure/runtime configuration used by core services.

`AppConfiguration` is the strongly-typed C# model used to deserialize `appsettings.json` in `ConfigurationLoader`.  
So `appsettings.json` is the file, and `AppConfiguration` is the in-code schema for it.

## `appsettings.json` (user editable)

Primary sections:

- `Application`
  - `Name`
  - `Version`
  - `MinimumLoadingTimeSeconds`
  - `WindowWidth`
  - `WindowHeight`
- `Logging`
  - `EnableDebugLogging`
  - `LogLevel`

Example:

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

## Runtime overrides (`BuildConfiguration`)

`BuildConfiguration` supports environment and `.env` values for:

- `IFRAME_URL`
- `API_URL`
- `WEBVIEW_TRUSTED_DOMAINS` (comma-separated)
- `APP_NAME`
- `APP_LOGO`
- `API_TIMEOUT`
- `API_MAX_RETRIES`
- `API_MAX_RESPONSE_SIZE_BYTES`
- `WEBVIEW_ALLOW_HTTP`
- `SECURITY_GRACE_PERIOD_DAYS`
- `SECURITY_MAX_ACTIVATION_ATTEMPTS`
- `SECURITY_LOCKOUT_MINUTES`
- `SECURITY_ACTIVATION_LOOKBACK_MINUTES`
- `SECURITY_BACKGROUND_VERIFICATION_INTERVAL_MINUTES`

Resolution order for each setting:

1. OS environment variable
2. `.env` file value
3. Built-in default value in `BuildConfiguration`

Example `.env`:

```env
IFRAME_URL=https://app.example.com/
API_URL=https://api.example.com/
WEBVIEW_TRUSTED_DOMAINS=app.example.com,api.example.com
APP_NAME=ECoopSystem
APP_LOGO=Assets/Images/logo.png
API_TIMEOUT=12
API_MAX_RETRIES=3
API_MAX_RESPONSE_SIZE_BYTES=1048576
WEBVIEW_ALLOW_HTTP=false
SECURITY_GRACE_PERIOD_DAYS=7
SECURITY_MAX_ACTIVATION_ATTEMPTS=3
SECURITY_LOCKOUT_MINUTES=5
SECURITY_ACTIVATION_LOOKBACK_MINUTES=1
SECURITY_BACKGROUND_VERIFICATION_INTERVAL_MINUTES=1
```

## Security-related values

Security numeric settings and API limits are exposed from `BuildConfiguration` and can be overridden through environment variables/`.env`.

Key values include:

- `ApiTimeout`
- `ApiMaxRetries`
- `ApiMaxResponseSizeBytes`
- `SecurityGracePeriodDays`
- `SecurityMaxActivationAttempts`
- `SecurityLockoutMinutes`

## Recommended practice

- Keep `appsettings.json` for UX/logging settings.
- Use environment variables or `.env` for deployment endpoint overrides.
- Do not commit production endpoint values to source control.
