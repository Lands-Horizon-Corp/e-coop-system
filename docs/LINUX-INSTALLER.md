# Linux Installer Guide for ECoopSystem (Ubuntu .deb)

# Linux Installer (`.deb`)

## Prerequisites

- Ubuntu/Debian/WSL environment
- `.NET 9 SDK`
- `dpkg-deb` (`sudo apt install -y dpkg-dev`)

## Build

Basic:

```bash
bash build-linux-installer.sh 1.0.0
```

Custom endpoints:

```bash
bash build-linux-installer.sh 1.0.0 https://app.example.com https://api.example.com Release
```

PowerShell wrapper:

```powershell
./build-linux-installer.ps1 -Version 1.0.0
```

## Output

`output/installer/ecoopsystem_<version>_amd64.deb`

## Install and remove

```bash
sudo apt install ./output/installer/ecoopsystem_1.0.0_amd64.deb
ecoopsystem
sudo apt remove ecoopsystem
```

## Package layout

- App files: `/opt/ECoopSystem`
- Launcher: `/usr/bin/ecoopsystem`
- Desktop entry: `/usr/share/applications/ecoopsystem.desktop`
