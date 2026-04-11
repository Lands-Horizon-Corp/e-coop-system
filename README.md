# ECoopSystem

Cross-platform desktop app built with `Avalonia UI` and `.NET 9`.

## What this project includes

- Desktop UI using Avalonia
- Embedded web content via CEF-based WebView
- Secure local storage for app secrets/state
- Build scripts for Windows, Linux, and packaging workflows

## Quick start

### Prerequisites

- `.NET 9 SDK`
- Windows 10/11 x64 or Linux x64

### Run locally

```bash
cp .env.example .env
dotnet restore
dotnet run
```

## Build and publish

### Make build (`Makefile`)

```bash
make build
make build PLATFORM=windows IFRAME_URL=http://localhost:3000 API_URL=http://localhost:5000
```

`make build` defaults:

- `PLATFORM=all` (builds `windows`, `linux`, `macos`)
- `CONFIG=Release`
- Values are read from `.env` (or build arguments) and baked into `BuildConfiguration` during build.

Packaged build output (zip):

- `output/build/windows/*.zip`
- `output/build/linux/*.zip`
- `output/build/macos/*.zip`

### Output

Published files are generated at:

`bin/Release/net9.0/<runtime-id>/publish/`

> Keep the full publish directory. Single-file distribution is not used because WebView/CEF requires external runtime files.

## Installer builds

- Windows installer: `build-windows-installer.ps1` / `build-windows-installer.sh`
- Linux `.deb` package: `build-linux-installer.sh` / `build-linux-installer.ps1`

### Makefile installer command

```bash
make buildinstaller
make buildinstaller PLATFORM=windows VERSION=1.0.0
make buildinstaller PLATFORM=linux VERSION=1.0.0
```

Notes:

- `buildinstaller` currently supports Windows and Linux.
- macOS installer generation is skipped for now.
- Installer outputs are organized under:
  - `output/installer/windows/`
  - `output/installer/linux/`
  - `output/installer/macos/`

Examples:

```powershell
./build-windows-installer.ps1 -Version 1.0.0
```

```bash
bash build-linux-installer.sh 1.0.0
```

## Configuration model

This project uses two configuration layers:

1. `appsettings.json` for user-facing app options (window size, logging, app name/version).
2. `BuildConfiguration` for secure build-time-baked configuration.

Important build-time values are provided via `.env` (or build args):

- `IFRAME_URL`
- `API_URL`
- `WEBVIEW_TRUSTED_DOMAINS` (comma-separated)
- `APP_NAME`, `APP_LOGO`
- `API_TIMEOUT`, `API_MAX_RETRIES`, `API_MAX_RESPONSE_SIZE_BYTES`
- `WEBVIEW_ALLOW_HTTP`
- `SECURITY_GRACE_PERIOD_DAYS`, `SECURITY_MAX_ACTIVATION_ATTEMPTS`
- `SECURITY_LOCKOUT_MINUTES`, `SECURITY_ACTIVATION_LOOKBACK_MINUTES`, `SECURITY_BACKGROUND_VERIFICATION_INTERVAL_MINUTES`

See `docs/CONFIGURATION.md`.

## Documentation

- [Build](docs/BUILD.md)
- [Configuration](docs/CONFIGURATION.md)
- [Installer](docs/INSTALLER.md)
- [Windows guide](docs/WINDOWS.md)
- [Linux guide](docs/LINUX.md)
- [Linux installer](docs/LINUX-INSTALLER.md)
- [macOS notes](docs/MACOS.md)
- [Quick reference](docs/QUICK-REFERENCE.md)

## Support

- Issues: https://github.com/Lands-Horizon-Corp/e-coop-system/issues
- Repository: https://github.com/Lands-Horizon-Corp/e-coop-system
