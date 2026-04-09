# macOS Notes

# macOS Notes

macOS installer packaging is not implemented.

## Build

Use one of the supported macOS targets:

```powershell
./build.ps1 -Platform mac-intel
./build.ps1 -Platform mac-arm
```

Or with make:

```bash
make build PLATFORM=mac-intel
make build PLATFORM=mac-arm
```

Build archives are written to:

- `output/build/macos/`
