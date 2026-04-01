# Linux Installer Guide for ECoopSystem (Ubuntu .deb)

This guide explains how to build and distribute a `.deb` installer for Ubuntu.

## Overview

Target format:
- `.deb` (Ubuntu/Debian package)

Output file:
- `output/installer/ecoopsystem_<version>_amd64.deb`

---

## Quick Start

### Build on Ubuntu / WSL
```bash
bash build-linux-installer.sh 1.0.0
```

With custom URLs/config:
```bash
bash build-linux-installer.sh 1.0.0 "https://your-app-url.com" "https://your-api-url.com" Release
```

### Build from PowerShell (WSL guidance)
```powershell
.\build-linux-installer.ps1 -Version 1.0.0
```

---

## Prerequisites

Install required tools in Ubuntu/WSL:
```bash
sudo apt update
sudo apt install -y dpkg-dev
```

Also ensure `.NET 9 SDK` is installed.

---

## Installation on Ubuntu

Install package:
```bash
sudo apt install ./output/installer/ecoopsystem_1.0.0_amd64.deb
```

Run app:
```bash
ecoopsystem
```

Uninstall:
```bash
sudo apt remove ecoopsystem
```

---

## Build Parameters

`build-linux-installer.sh` arguments:

1. `Version` (default: `1.0.0`)
2. `IFrameUrl` (default: development URL)
3. `ApiUrl` (default: development URL)
4. `Configuration` (default: `Release`)

Example:
```bash
bash build-linux-installer.sh 1.0.0 "https://prod-app.com" "https://prod-api.com" Release
```

---

## Notes

- This workflow is Ubuntu-focused and does not generate AppImage.
- Package installs app files under `/opt/ECoopSystem` and launcher at `/usr/bin/ecoopsystem`.
