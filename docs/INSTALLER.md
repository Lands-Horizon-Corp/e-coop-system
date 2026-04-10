# Building Installers for ECoopSystem

# Installer Guide

This project currently supports:

- Windows installer (`.exe`) via Inno Setup
- Linux installer (`.deb`) via `dpkg-deb`

macOS installer packaging is not implemented yet.

## Makefile orchestration

You can run installer builds through `Makefile`:

```bash
make buildinstaller
make buildinstaller PLATFORM=windows VERSION=1.0.0
make buildinstaller PLATFORM=linux VERSION=1.0.0
```

Behavior:

- `PLATFORM=all` attempts `windows linux macos`
- macOS installer step is skipped with an informational message
- Windows artifacts are copied to `output/installer/windows/`
- Linux artifacts are copied to `output/installer/linux/`

## Windows installer

### Prerequisite

Install Inno Setup and ensure `iscc` is available in `PATH`.

### Build command

```powershell
./build-windows-installer.ps1 -Version 1.0.0
```

Optional parameters:

- `-IFrameUrl`
- `-ApiUrl`
- `-Configuration`
- `-SkipBuild`
- `-OpenOutput`

Output directory:

`output/installer/`

## Linux `.deb` installer

### Prerequisite

Install `dpkg-deb` (package `dpkg-dev`) on Ubuntu/Debian/WSL.

### Build command

```bash
bash build-linux-installer.sh 1.0.0
```

With custom endpoints:

```bash
bash build-linux-installer.sh 1.0.0 https://app.example.com https://api.example.com Release
```

Output file:

`output/installer/ecoopsystem_<version>_amd64.deb`

## Related docs

- `docs/WINDOWS.md`
- `docs/LINUX.md`
- `docs/LINUX-INSTALLER.md`
