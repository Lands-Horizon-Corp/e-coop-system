# Quick Reference: Cross-Platform Development

## Platform Detection

```csharp
// Runtime detection
if (OperatingSystem.IsWindows()) { /* Windows code */ }
if (OperatingSystem.IsLinux()) { /* Linux code */ }
if (OperatingSystem.IsMacOS()) { /* macOS code */ }

// Alternative
if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) { }
if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux)) { }
```

## File Paths

```csharp
// ? CORRECT - Cross-platform
var path = Path.Combine(
    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
    "ECoopSystem",
    "file.dat"
);
// Windows: C:\Users\Username\AppData\Roaming\ECoopSystem\file.dat
// Linux: /home/username/.config/ECoopSystem/file.dat

// ? WRONG - Windows-specific
var path = @"C:\Users\Username\AppData\Roaming\ECoopSystem\file.dat";
var path = "C:\\Users\\Username\\AppData\\...";
```

## Process Execution

```csharp
// ? CORRECT - Platform-aware
if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
{
    Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
}
else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
{
    Process.Start("xdg-open", url);
}

// ? WRONG - Windows-only
Process.Start(url); // Won't work on Linux
```

## Conditional Compilation

```csharp
// In .csproj (define constant)
<PropertyGroup Condition="'$(OS)' == 'Windows_NT'">
    <DefineConstants>WINDOWS</DefineConstants>
</PropertyGroup>

// In code
#if WINDOWS
[STAThread]
#endif
public static void Main(string[] args) { }
```

## Registry Access (Windows-only)

```csharp
// ? CORRECT - Guarded
if (OperatingSystem.IsWindows())
{
    using var key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\...");
    // Registry code
}

// ? WRONG - Crashes on Linux
using var key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\...");
```

## Environment Variables

```csharp
// ? CORRECT - Cross-platform
var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
// Windows: C:\Users\Username
// Linux: /home/username

var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
// Windows: C:\Users\Username\AppData\Roaming
// Linux: /home/username/.config

// ? WRONG - Windows-specific
var appData = Environment.GetEnvironmentVariable("APPDATA");
```

## Line Endings

```csharp
// ? CORRECT - Platform-independent
var newLine = Environment.NewLine;
File.WriteAllText(path, $"Line 1{Environment.NewLine}Line 2");

// ? WRONG - Windows-specific
var text = "Line 1\r\nLine 2"; // \r\n is Windows-specific
```

## Building and Publishing

```bash
# Windows
dotnet publish -c Release -r win-x64 --self-contained

# Linux
dotnet publish -c Release -r linux-x64 --self-contained

# Framework-dependent (requires .NET installed)
dotnet publish -c Release -r linux-x64 --no-self-contained

# Single file (larger but simpler)
dotnet publish -c Release -r linux-x64 --self-contained -p:PublishSingleFile=true
```

## Testing Locally

```bash
# Run on current platform
dotnet run

# Run with hot reload
dotnet watch

# Run tests
dotnet test

# Check for runtime errors
dotnet run --configuration Release
```

## Common Pitfalls

### 1. Path Separators
```csharp
// ? WRONG
var path = "folder\\subfolder\\file.txt"; // Windows-only

// ? CORRECT
var path = Path.Combine("folder", "subfolder", "file.txt");
```

### 2. Case Sensitivity
```csharp
// Linux filesystems are case-sensitive!
// ? Might work on Windows, fail on Linux
File.Exists("MyFile.txt") // but file is named "myfile.txt"

// ? Always use exact casing
File.Exists("myfile.txt")
```

### 3. Executable Permissions (Linux)
```bash
# After publishing on/for Linux, always:
chmod +x ./ECoopSystem
```

### 4. Native Libraries
```csharp
// ? Use platform-specific library loading
[DllImport("user32.dll")] // Windows-only
[DllImport("libX11.so")] // Linux-only

// Better: Check platform first
if (OperatingSystem.IsWindows())
{
    // Use Windows API
}
```

## Development Environment

### VS Code on Linux
```json
// .vscode/launch.json
{
    "version": "0.2.0",
    "configurations": [
        {
            "name": ".NET Core Launch",
            "type": "coreclr",
            "request": "launch",
            "program": "${workspaceFolder}/bin/Debug/net9.0/ECoopSystem.dll",
            "args": [],
            "cwd": "${workspaceFolder}",
            "console": "internalConsole"
        }
    ]
}
```

### Debugging
```bash
# Enable debug logging
export DOTNET_LOGGING_LEVEL=Debug
export AVALONIA_LOGGING_LEVEL=Debug

dotnet run
```

## CI/CD Example

```yaml
# .github/workflows/build.yml
name: Build

on: [push, pull_request]

jobs:
  build-windows:
    runs-on: windows-latest
    steps:
      - uses: actions/checkout@v3
      - uses: actions/setup-dotnet@v3
        with:
          dotnet-version: '9.0.x'
      - run: dotnet restore
      - run: dotnet build -c Release
      - run: dotnet publish -c Release -r win-x64 --self-contained

  build-linux:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v3
      - uses: actions/setup-dotnet@v3
        with:
          dotnet-version: '9.0.x'
      - run: dotnet restore
      - run: dotnet build -c Release
      - run: dotnet publish -c Release -r linux-x64 --self-contained
      - run: chmod +x ./bin/Release/net9.0/linux-x64/publish/ECoopSystem
```

## Performance Tips

### Linux-Specific
```bash
# Enable tiered compilation (faster startup)
export DOTNET_TieredCompilation=1

# Optimize for throughput
export DOTNET_gcServer=1

# Set memory limits
export DOTNET_GCHeapCount=2
```

## Troubleshooting Commands

### Check Dependencies (Linux)
```bash
# Check what libraries are missing
ldd ./ECoopSystem

# Check library paths
ldconfig -p | grep libname
```

### Check .NET Installation
```bash
# All platforms
dotnet --info
dotnet --list-sdks
dotnet --list-runtimes
```

### Monitor Application
```bash
# Linux - Resource usage
top -p $(pgrep ECoopSystem)

# Linux - Network connections
netstat -anp | grep ECoopSystem

# All platforms - .NET diagnostics
dotnet-trace collect --process-id $(pgrep ECoopSystem)
```

## Resources

- [.NET on Linux](https://learn.microsoft.com/en-us/dotnet/core/install/linux)
- [Avalonia Cross-Platform](https://docs.avaloniaui.net/)
- [Platform Detection](https://learn.microsoft.com/en-us/dotnet/api/system.operatingsystem)
- [Path Handling](https://learn.microsoft.com/en-us/dotnet/api/system.io.path)

## Quick Tests

```bash
# Test if app runs
dotnet run

# Test publishing
dotnet publish -c Release -r linux-x64 --self-contained

# Test on fresh VM/container
docker run -it mcr.microsoft.com/dotnet/sdk:9.0 bash
# Copy and test your published app
```
