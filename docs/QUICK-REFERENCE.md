# Quick Reference

## Run locally

```bash
dotnet restore
dotnet run
```

## Build

```powershell
./build.ps1 -Platform windows
./build.ps1 -Platform linux
```

```bash
make build
make build PLATFORM=windows IFRAME_URL=http://localhost:3000 API_URL=http://localhost:5000
```

`make build` defaults:

- `PLATFORM=all` (windows + linux + macos)
- `CONFIG=Release`

Packaged artifacts:

- `output/build/windows/*.zip`
- `output/build/linux/*.zip`
- `output/build/macos/*.zip`

## Installer

```powershell
./build-windows-installer.ps1 -Version 1.0.0
```

```bash
bash build-linux-installer.sh 1.0.0
```

```bash
make buildinstaller
make buildinstaller PLATFORM=linux VERSION=1.0.0
```

`buildinstaller` supports Windows and Linux currently; macOS installer is skipped.

## Common environment overrides

Use these in `.env` (or pass via build args) before building:

- `IFRAME_URL`
- `API_URL`
- `WEBVIEW_TRUSTED_DOMAINS`
- `APP_NAME`, `APP_LOGO`
- `API_TIMEOUT`, `API_MAX_RETRIES`, `API_MAX_RESPONSE_SIZE_BYTES`
- `WEBVIEW_ALLOW_HTTP`
- `SECURITY_GRACE_PERIOD_DAYS`, `SECURITY_MAX_ACTIVATION_ATTEMPTS`
- `SECURITY_LOCKOUT_MINUTES`, `SECURITY_ACTIVATION_LOOKBACK_MINUTES`, `SECURITY_BACKGROUND_VERIFICATION_INTERVAL_MINUTES`

`API_URL` format:

`https://your-server.com/web/api/v1/license`

## Useful diagnostics

```bash
dotnet --info
dotnet --list-sdks
dotnet --list-runtimes
```

Linux dependency check:

```bash
ldd ./ECoopSystem
```
