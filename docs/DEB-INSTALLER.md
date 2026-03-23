# Creating a Debian (`.deb`) Installer for ECoopSystem

This guide creates an Ubuntu/Debian installer package for `ECoopSystem`.

## Target Environment

- Build host: Ubuntu 24 (VirtualBox)
- App framework: `.NET 9`
- Package format: `amd64 .deb`

## Prerequisites (Ubuntu)

```bash
sudo apt update
sudo apt install -y dotnet-sdk-9.0 dpkg-dev desktop-file-utils
```

## Build the `.deb`

From the repository root:

```bash
chmod +x scripts/package-deb.sh
./scripts/package-deb.sh --version 1.0.0
```

Output:

- `output/installer/ECoopSystem_1.0.0_amd64.deb`

## Install and Test

```bash
sudo apt install ./output/installer/ECoopSystem_1.0.0_amd64.deb
```

Run with either:

```bash
ecoopsystem
```

or from desktop launcher (`ECoopSystem`).

## Uninstall

```bash
sudo apt remove ecoopsystem
```

## Script Options

```bash
./scripts/package-deb.sh \
  --version 1.0.0 \
  --configuration Release \
  --output output/installer
```

Use an existing publish folder:

```bash
./scripts/package-deb.sh --publish-dir bin/Release/net9.0/linux-x64/publish --version 1.0.0
```

Build a framework-dependent package:

```bash
./scripts/package-deb.sh --framework-dependent --version 1.0.0
```

## Package Layout

The script installs:

- App files: `/opt/ecoopsystem/`
- CLI launcher: `/usr/bin/ecoopsystem`
- Desktop entry: `/usr/share/applications/ecoopsystem.desktop`
- Icon: `/usr/share/icons/hicolor/256x256/apps/ecoopsystem.png`

## Runtime Dependencies

The package declares common Avalonia/WebView Linux dependencies, including:

- `libgtk-3-0`
- `libnss3`
- `libasound2`
- `libgbm1`
- `libx11-6` and related X11 libraries

For framework-dependent builds, `dotnet-runtime-9.0` is also required.
