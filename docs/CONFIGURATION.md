# Configuration System Documentation

# Configuration Guide

## Overview

The app configuration is split into:

1. `appsettings.json` for user-facing application settings.
2. `Build/BuildConfiguration.cs` for secure/runtime configuration used by core services.

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

Example `.env`:

```env
IFRAME_URL=https://app.example.com/
API_URL=https://api.example.com/
WEBVIEW_TRUSTED_DOMAINS=app.example.com,api.example.com
```

## Security-related values

Security numeric settings and API limits are defined in `BuildConfiguration` constants and generated via build scripts.

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
