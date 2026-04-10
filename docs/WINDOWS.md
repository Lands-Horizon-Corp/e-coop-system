# Windows Deployment Guide for ECoopSystem

# Windows Guide

## Requirements

- Windows 10/11 x64
- `.NET 9 SDK` (development)
- Inno Setup (for installer creation)

## Build

```powershell
./build.ps1 -Platform windows
```

Direct publish option:

```powershell
dotnet publish -c Release -r win-x64 --self-contained
```

## Build installer

```powershell
./build-windows-installer.ps1 -Version 1.0.0
```

Output:

`output/installer/`

## Data location

`%APPDATA%\ECoopSystem\`

## Troubleshooting

### App does not start

Run from terminal in publish folder:

```powershell
./ECoopSystem.exe
```

### SmartScreen block

Use **More info** ? **Run anyway** for trusted internal builds.

### Runtime/dependency errors

- Prefer self-contained publish for distribution.
- Rebuild and distribute the full `publish` directory, not only the executable.
