# Linux Guide

# Linux Guide

## Supported target

- Primary target: `linux-x64`
- Optional target: `linux-arm64`

## Prerequisites

- `.NET 9 SDK`
- GTK and browser/runtime dependencies required by Avalonia + CEF

Ubuntu/Debian example:

```bash
sudo apt update
sudo apt install -y dotnet-sdk-9.0 libgtk-3-0 libnss3 libasound2 libxss1
```

## Build

```bash
dotnet publish -c Release -r linux-x64 --self-contained
```

Or use repository build helpers:

```bash
make build PLATFORM=linux
```

## Run

```bash
cd bin/Release/net9.0/linux-x64/publish
chmod +x ECoopSystem
./ECoopSystem
```

## Data location

`~/.config/ECoopSystem/`

## Troubleshooting

### Missing dependency

```bash
ldd ./ECoopSystem
```

Install missing libraries reported as `not found`.

### Permission denied

```bash
chmod +x ./ECoopSystem
```

### Debug logging

```bash
DOTNET_LOGGING_LEVEL=Debug ./ECoopSystem
```

### Wayland display issues

```bash
GDK_BACKEND=x11 ./ECoopSystem
```
