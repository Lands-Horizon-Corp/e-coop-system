# Quick Reference: Cross-Platform Development

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

- `IFRAME_URL`
- `API_URL`
- `WEBVIEW_TRUSTED_DOMAINS`

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
