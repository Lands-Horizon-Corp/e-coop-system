# ECoopSystem Build System

# Build Guide

## Prerequisites

- `.NET 9 SDK`
- PowerShell (`pwsh`) for `build.ps1`
- `make` for `Makefile` workflows

## Main build commands

### PowerShell (`build.ps1`)

```powershell
./build.ps1 -Platform windows
./build.ps1 -Platform linux
```

Common options:

- `-IFrameUrl`
- `-ApiUrl`
- `-Platform` (`windows`, `linux`, `linux-deb`, `linux-arm`, `mac-intel`, `mac-arm`)
- `-Configuration` (`Debug`, `Release`)

### Make (`Makefile`)

```bash
make build
make build PLATFORM=linux IFRAME_URL=http://localhost:3000 API_URL=http://localhost:5000
```

`Makefile` defaults now come from `Build/BuildConfiguration.cs` when possible:

- `IFRAME_URL`
- `API_URL`
- `APP_NAME`
- `APP_LOGO`
- API/security numeric build settings

Key defaults:

- `PLATFORM=all` (builds `windows`, `linux`, `macos`)
- `CONFIG=Release`
- `VERSION=1.0.0` (used by `buildinstaller`)

Useful targets:

- `make build`
- `make buildinstaller`
- `make clean`
- `make help`

## Output location

Primary publish output (dotnet):

`bin/<Configuration>/net9.0/<runtime-id>/publish/`

Makefile packaged output:

- `output/build/windows/<AppName>-windows-<Config>.zip`
- `output/build/linux/<AppName>-linux-<Config>.zip`
- `output/build/macos/<AppName>-macos-<Config>.zip`

Installer output folders are prepared as:

- `output/installer/windows/`
- `output/installer/linux/`
- `output/installer/macos/`

Examples:

- Windows: `bin/Release/net9.0/win-x64/publish/`
- Linux: `bin/Release/net9.0/linux-x64/publish/`

## Important packaging note

Do not distribute only the main executable. Distribute the full publish directory because WebView/CEF dependencies are required at runtime.

## Installer builds

- Windows installer: `./build-windows-installer.ps1`
- Linux `.deb` installer: `bash ./build-linux-installer.sh 1.0.0`

Or via `Makefile` orchestration:

```bash
make buildinstaller
make buildinstaller PLATFORM=windows VERSION=1.0.0
make buildinstaller PLATFORM=linux VERSION=1.0.0
```

Notes:

- `buildinstaller` currently supports Windows and Linux.
- macOS installer generation is intentionally skipped for now.

See `docs/INSTALLER.md` for installer details.
