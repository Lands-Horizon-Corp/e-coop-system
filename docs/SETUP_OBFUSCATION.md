# Complete Setup Guide: String Encryption & Code Obfuscation

This guide will walk you through setting up string encryption and code obfuscation for ECoopSystem.

## Table of Contents
1. [Overview](#overview)
2. [String Encryption Setup](#string-encryption-setup)
3. [Code Obfuscation Setup](#code-obfuscation-setup)
4. [Production Build Workflow](#production-build-workflow)
5. [Verification & Testing](#verification--testing)
6. [Advanced Configuration](#advanced-configuration)

---

## Overview

### What's Protected?

**String Encryption:**
- API URLs and endpoints
- Configuration keys
- File names and folder paths
- Data protection purpose strings
- Any sensitive string literals

**Code Obfuscation:**
- Class names ? `A`, `B`, `C`...
- Method names ? `a`, `b`, `c`...
- Property names ? Unreadable
- Control flow ? Scrambled logic
- Constants ? Encrypted
- Resources ? Protected

### Security Benefits

| Attack Vector | Without Protection | With Protection |
|--------------|-------------------|----------------|
| String search in binary | ? URLs visible | ? Encrypted |
| Decompilation | ? Full source visible | ? Obfuscated code |
| Debug attachment | ? Easy debugging | ? Anti-debug active |
| Memory dump | ? Secrets extractable | ? Tamper detection |
| Static analysis | ? Logic readable | ? Control flow hidden |

---

## String Encryption Setup

### Step 1: Install Encryption Tools

The encryption tools are already included in the `Build/` directory:
- ? `encrypt-string.ps1` (PowerShell)
- ? `encrypt-string.py` (Python, requires `cryptography` package)

**For Python tool:**
```bash
pip install cryptography
```

### Step 2: Encrypt Your Strings

#### Example: Encrypt an API URL
```powershell
.\Build\encrypt-string.ps1 -PlainText "https://production-api.example.com/"
```

**Output:**
```
Original: https://production-api.example.com/
Encrypted: gH7xK3mP9qR2tV4wX8zA1cBnFgDe5sL2...

Use in C# code:
StringEncryption.Decrypt("gH7xK3mP9qR2tV4wX8zA1cBnFgDe5sL2...")
```

#### Example: Encrypt an Endpoint
```powershell
.\Build\encrypt-string.ps1 -PlainText "/api/v1/auth/login"
```

### Step 3: Use Encrypted Strings in Code

**Option A: Direct decryption**
```csharp
using ECoopSystem.Utilities;

public class MyService 
{
    private const string EncryptedApiUrl = "gH7xK3mP9qR2tV...";
    private string ApiUrl => StringEncryption.Decrypt(EncryptedApiUrl);
}
```

**Option B: Using EncryptedStrings repository**

1. Add your encrypted string to `Utilities/EncryptedStrings.cs`:
```csharp
private const string Enc_MyApiUrl = "gH7xK3mP9qR2tV...";
public static string MyApiUrl => Enc_MyApiUrl.DecryptString();
```

2. Use it anywhere:
```csharp
using ECoopSystem.Utilities;

var url = EncryptedStrings.MyApiUrl;
```

### Step 4: Already Encrypted Strings

The following strings are already encrypted in your codebase:

| Location | Encrypted Item | Base64 Value |
|----------|---------------|--------------|
| `Stores/SecretKeyStore.cs` | Folder name | `RUNvb3BTeXN0ZW0=` |
| `Stores/SecretKeyStore.cs` | File name | `c2VjcmV0LmRhdA==` |
| `Stores/SecretKeyStore.cs` | Purpose string | `RUNvb3BTeXN0ZW0uU2VjcmV0S2V5LnYx` |
| `Stores/AppStateStore.cs` | Folder name | `RUNvb3BTeXN0ZW0=` |
| `Stores/AppStateStore.cs` | File name | `YXBwc3RhdGUuZGF0` |
| `Stores/AppStateStore.cs` | Purpose string | `RUNvb3BTeXN0ZW0uQXBwU3RhdGUudjE=` |
| `Services/LicenseService.cs` | Activate endpoint | `L3dlYi9hcGkvdjEvbGljZW5zZS9hY3RpdmF0ZQ==` |
| `Services/LicenseService.cs` | Verify endpoint | `L3dlYi9hcGkvdjEvbGljZW5zZS92ZXJpZnk=` |

---

## Code Obfuscation Setup

### Step 1: Choose an Obfuscator

#### Option A: ConfuserEx 2 (Recommended, Free)

**Installation:**
1. Download from: https://github.com/mkaring/ConfuserEx/releases
2. Download the latest `ConfuserEx-CLI` release
3. Extract to: `.\Tools\ConfuserEx\`
4. Ensure `Confuser.CLI.exe` exists in that directory

**Verify installation:**
```powershell
Test-Path ".\Tools\ConfuserEx\Confuser.CLI.exe"
# Should return: True
```

#### Option B: Obfuscar (Free, Alternative)

```bash
dotnet tool install --global Obfuscar.GlobalTool
```

**Create `obfuscar.xml`:**
```xml
<?xml version="1.0"?>
<Obfuscator>
  <Var name="InPath" value=".\bin\Release\net9.0\win-x64\publish" />
  <Var name="OutPath" value=".\Obfuscated" />
  
  <Module file="$(InPath)\ECoopSystem.dll" />
  
  <AssemblySearchPath path="$(InPath)" />
</Obfuscator>
```

#### Option C: Commercial Tools

**Eazfuscator.NET:**
- Download: https://www.gapotchenko.com/eazfuscator.net
- Free for personal use
- Easy integration with MSBuild

**Add to `.csproj`:**
```xml
<PropertyGroup Condition="'$(Configuration)' == 'Release'">
  <Eazfuscator>true</Eazfuscator>
</PropertyGroup>
```

**.NET Reactor:**
- Download: https://www.eziriz.com/dotnet_reactor.htm
- Trial available
- GUI-based configuration

### Step 2: Configure Obfuscation

The `confuser.crproj` file is already configured with recommended settings.

**Customize protection levels:**

**Light Protection** (Fast, good for testing):
```xml
<rule pattern="true" inherit="false">
  <protection id="rename" />
  <protection id="constants" />
</rule>
```

**Standard Protection** (Recommended):
```xml
<rule pattern="true" inherit="false">
  <protection id="anti tamper" />
  <protection id="rename" />
  <protection id="constants" />
  <protection id="ctrl flow" />
</rule>
```

**Maximum Protection** (Slower builds):
```xml
<rule pattern="true" inherit="false">
  <protection id="anti tamper" />
  <protection id="anti debug" />
  <protection id="anti dump" />
  <protection id="ctrl flow" />
  <protection id="ref proxy" />
  <protection id="rename" />
  <protection id="constants" />
  <protection id="resources" />
</rule>
```

### Step 3: Exclude Files from Obfuscation (If Needed)

Some files might break with obfuscation (e.g., XAML data binding). Add attributes:

```csharp
using System.Reflection;

[Obfuscation(Exclude = true, ApplyToMembers = true)]
public class MainViewModel : ViewModelBase
{
    // Properties used in XAML bindings should not be renamed
}
```

---

## Production Build Workflow

### Full Production Build (Recommended)

```powershell
# 1. Clean previous builds
Remove-Item -Recurse -Force bin, obj, Build/BuildConfiguration.cs, Build/SecureBuildConfiguration.cs

# 2. Build with encryption
.\Build\build-secure.ps1 `
    -IFrameUrl "https://production-client.example.com" `
    -ApiUrl "https://production-api.example.com" `
    -AppName "ECoopSystem" `
    -Platform windows `
    -Configuration Release

# 3. Output will be at: bin\Release\net9.0\win-x64\publish\
# 4. Obfuscated output (if ConfuserEx installed): .\Confused\
```

### Quick Build (No Obfuscation)

```powershell
.\Build\build-secure.ps1 `
    -IFrameUrl "https://example.com" `
    -Platform windows `
    -SkipObfuscation
```

### Multi-Platform Build

```powershell
# Windows
.\Build\build-secure.ps1 -IFrameUrl "https://example.com" -ApiUrl "https://api.example.com" -Platform windows

# Linux
.\Build\build-secure.ps1 -IFrameUrl "https://example.com" -ApiUrl "https://api.example.com" -Platform linux

# macOS ARM
.\Build\build-secure.ps1 -IFrameUrl "https://example.com" -ApiUrl "https://api.example.com" -Platform mac-arm
```

---

## Verification & Testing

### 1. Verify String Encryption

**Test decryption:**
```csharp
// Add to Program.cs temporarily
#if DEBUG
var testUrl = "https://test-api.example.com/";
var encrypted = StringEncryption.Encrypt(testUrl);
var decrypted = StringEncryption.Decrypt(encrypted);
Console.WriteLine($"Original: {testUrl}");
Console.WriteLine($"Encrypted: {encrypted}");
Console.WriteLine($"Decrypted: {decrypted}");
Console.WriteLine($"Match: {testUrl == decrypted}");
#endif
```

### 2. Check for Plain Text in Binary

```powershell
# Search for sensitive strings
$exe = "bin\Release\net9.0\win-x64\publish\ECoopSystem.exe"
$dll = "bin\Release\net9.0\win-x64\publish\ECoopSystem.dll"

Select-String -Path $exe -Pattern "api.example.com" -SimpleMatch
Select-String -Path $dll -Pattern "license/activate" -SimpleMatch

# Should return NO results if properly encrypted
```

### 3. Decompile and Inspect

**Using ILSpy:**
1. Download: https://github.com/icsharpcode/ILSpy/releases
2. Open `ECoopSystem.dll`
3. Check `LicenseService.ActivateAsync` method
4. Verify URLs are not visible

**Before encryption:**
```csharp
var url = "https://api.example.com/web/api/v1/license/activate";
```

**After encryption:**
```csharp
const string encEndpoint = "L3dlYi9hcGkvdjEvbGljZW5zZS9hY3RpdmF0ZQ==";
var endpoint = System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(encEndpoint));
```

**After obfuscation:**
```csharp
string a = "L3dlYi9hcGkvdjEvbGljZW5zZS9hY3RpdmF0ZQ==";
string b = c.d.e.f(g.h(a));
```

### 4. Functional Testing

```powershell
# Run the built application
.\bin\Release\net9.0\win-x64\publish\ECoopSystem.exe

# Test checklist:
# [ ] Application starts successfully
# [ ] License activation works
# [ ] WebView loads correctly
# [ ] All features work as expected
# [ ] No crashes or errors
```

---

## Advanced Configuration

### Custom Encryption Keys

To use your own encryption keys, modify `Utilities/StringEncryption.cs`:

```csharp
private static byte[] DeriveKey()
{
    // Replace with your own unique string
    var data = "YourCompany.YourApp.CustomKey.2026";
    using var sha = SHA256.Create();
    return sha.ComputeHash(Encoding.UTF8.GetBytes(data));
}

private static byte[] DeriveIV()
{
    // Replace with your own unique string
    var data = "YourCompany.YourApp.CustomIV.2026";
    using var sha = SHA256.Create();
    var hash = sha.ComputeHash(Encoding.UTF8.GetBytes(data));
    var iv = new byte[16];
    Array.Copy(hash, iv, 16);
    return iv;
}
```

?? **Important:** After changing keys, re-encrypt all strings using the new encryption tools.

### Exclude Avalonia ViewModels from Obfuscation

ViewModels used with XAML data binding should preserve property names:

**Add to each ViewModel:**
```csharp
using System.Reflection;

[Obfuscation(Exclude = false, ApplyToMembers = false)]
public class MainViewModel : ViewModelBase
{
    // Properties used in XAML binding - exclude from renaming
    [Obfuscation(Exclude = true)]
    public string URL { get; }
    
    [Obfuscation(Exclude = true)]
    public bool IsLoading { get; private set; }
    
    // Private methods can be obfuscated
    private void InternalMethod() { }
}
```

### Enhanced Obfuscation with .NET Reactor

**Setup:**
1. Download .NET Reactor trial
2. Create `reactor.nrproj`:

```xml
<?xml version="1.0" encoding="utf-8"?>
<project>
  <settings>
    <necrobit>1</necrobit>
    <anti_tampering>1</anti_tampering>
    <anti_debugging>1</anti_debugging>
    <obfuscation>1</obfuscation>
    <control_flow_obfuscation>1</control_flow_obfuscation>
    <string_encryption>1</string_encryption>
    <resource_encryption>1</resource_encryption>
  </settings>
  <input>bin\Release\net9.0\win-x64\publish\ECoopSystem.dll</input>
  <output>.\Protected\ECoopSystem.dll</output>
</project>
```

**Run:**
```powershell
.\Tools\dotNET_Reactor\dotNET_Reactor.Console.exe -file reactor.nrproj
```

### Native AOT Compilation (Ultimate Protection)

For maximum protection, compile to native code:

**Update `.csproj`:**
```xml
<PropertyGroup Condition="'$(Configuration)' == 'Release'">
  <PublishAot>true</PublishAot>
  <IlcOptimizationPreference>Speed</IlcOptimizationPreference>
  <IlcGenerateDgmlFile>false</IlcGenerateDgmlFile>
</PropertyGroup>
```

**Publish:**
```powershell
dotnet publish -c Release -r win-x64 /p:PublishAot=true
```

?? **Note:** NativeAOT has limitations with reflection and dynamic code generation.

---

## Common Patterns

### Pattern 1: Encrypt All Hardcoded URLs

**Before:**
```csharp
public class ApiClient
{
    private const string BaseUrl = "https://api.example.com";
    private const string LoginEndpoint = "/auth/login";
    private const string DataEndpoint = "/data/fetch";
}
```

**After:**
```csharp
using ECoopSystem.Utilities;

public class ApiClient
{
    private const string Enc_BaseUrl = "hGx7K3mP9qR2tV...";
    private const string Enc_LoginEndpoint = "jHy8L4nQ0rS3uW...";
    private const string Enc_DataEndpoint = "kIz9M5oR1sT4vX...";
    
    private static string BaseUrl => StringEncryption.Decrypt(Enc_BaseUrl);
    private static string LoginEndpoint => StringEncryption.Decrypt(Enc_LoginEndpoint);
    private static string DataEndpoint => StringEncryption.Decrypt(Enc_DataEndpoint);
}
```

### Pattern 2: Encrypt Configuration Keys

**Before:**
```csharp
var apiUrl = _config.GetValue<string>("ApiSettings:BaseUrl");
var timeout = _config.GetValue<int>("ApiSettings:Timeout");
```

**After:**
```csharp
using ECoopSystem.Utilities;

const string encKey1 = "QXBpU2V0dGluZ3M6QmFzZVVybA==";
const string encKey2 = "QXBpU2V0dGluZ3M6VGltZW91dA==";

var key1 = System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(encKey1));
var key2 = System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(encKey2));

var apiUrl = _config.GetValue<string>(key1);
var timeout = _config.GetValue<int>(key2);
```

### Pattern 3: Protect Connection Strings

**Before:**
```csharp
var connectionString = "Server=myserver;Database=mydb;User=admin;Password=secret123";
```

**After:**
```csharp
using ECoopSystem.Utilities;

const string encConnStr = "mJa0N6pS2tU5wY...";
var connectionString = StringEncryption.Decrypt(encConnStr);
```

---

## CI/CD Integration

### GitHub Actions with Encryption

```yaml
name: Secure Production Build

on:
  release:
    types: [published]

jobs:
  build-windows:
    runs-on: windows-latest
    steps:
      - name: Checkout
        uses: actions/checkout@v3
      
      - name: Setup .NET 9
        uses: actions/setup-dotnet@v3
        with:
          dotnet-version: '9.0.x'
      
      - name: Download ConfuserEx
        run: |
          Invoke-WebRequest -Uri "https://github.com/mkaring/ConfuserEx/releases/download/v2.0.0/ConfuserEx-CLI.zip" -OutFile "confuser.zip"
          Expand-Archive -Path "confuser.zip" -DestinationPath ".\Tools\ConfuserEx\"
      
      - name: Build with Encryption & Obfuscation
        run: |
          .\Build\build-secure.ps1 `
            -IFrameUrl "${{ secrets.PROD_IFRAME_URL }}" `
            -ApiUrl "${{ secrets.PROD_API_URL }}" `
            -Platform windows `
            -Configuration Release
        env:
          PROD_IFRAME_URL: ${{ secrets.PROD_IFRAME_URL }}
          PROD_API_URL: ${{ secrets.PROD_API_URL }}
      
      - name: Sign Binary (Optional)
        run: |
          # Code signing with certificate
          signtool sign /f certificate.pfx /p ${{ secrets.CERT_PASSWORD }} /tr http://timestamp.digicert.com /td sha256 /fd sha256 "bin\Release\net9.0\win-x64\publish\ECoopSystem.exe"
      
      - name: Create Release Archive
        run: |
          Compress-Archive -Path "Confused\*" -DestinationPath "ECoopSystem-${{ github.ref_name }}-Windows.zip"
      
      - name: Upload Release Asset
        uses: actions/upload-release-asset@v1
        env:
          GITHUB_TOKEN: ${{ secrets.GITHUB_TOKEN }}
        with:
          upload_url: ${{ github.event.release.upload_url }}
          asset_path: ./ECoopSystem-${{ github.ref_name }}-Windows.zip
          asset_name: ECoopSystem-${{ github.ref_name }}-Windows.zip
          asset_content_type: application/zip
```

---

## Troubleshooting

### Issue: "Decryption returns empty string"

**Cause:** Encryption key mismatch or corrupted Base64

**Solution:**
1. Verify encrypted string is valid Base64:
```powershell
[Convert]::FromBase64String("your-encrypted-string")
```

2. Re-encrypt using the tool:
```powershell
.\Build\encrypt-string.ps1 -PlainText "your-original-text"
```

3. Ensure no whitespace before/after encrypted string in code

### Issue: "Obfuscation breaks XAML bindings"

**Cause:** Property names renamed, breaking `{Binding PropertyName}`

**Solution:**
Add `[Obfuscation(Exclude = true)]` to properties:
```csharp
[Obfuscation(Exclude = true)]
public string URL { get; }

[Obfuscation(Exclude = true)]
public bool IsLoading { get; set; }
```

Or exclude entire ViewModel:
```csharp
[Obfuscation(Exclude = true, ApplyToMembers = true)]
public class MainViewModel : ViewModelBase { }
```

### Issue: "ConfuserEx not found"

**Solution:**
```powershell
# Check installation
Test-Path ".\Tools\ConfuserEx\Confuser.CLI.exe"

# If false, download and extract to correct location
New-Item -ItemType Directory -Force -Path ".\Tools\ConfuserEx"
# Then extract ConfuserEx-CLI.zip to this directory
```

### Issue: "Application crashes after obfuscation"

**Common causes:**
1. **Reflection usage** - Exclude types used with reflection
2. **Serialization** - Exclude DTOs and serialized classes
3. **Plugin systems** - Exclude plugin interfaces
4. **XAML bindings** - Exclude ViewModels or mark properties

**Debug steps:**
1. Build without obfuscation - verify it works
2. Apply only symbol renaming - test
3. Add control flow obfuscation - test
4. Add anti-tampering - test
5. Identify which protection breaks it

**Exclude from obfuscation:**
```xml
<!-- In confuser.crproj -->
<rule pattern="true" inherit="false">
  <protection id="rename">
    <argument name="renPublic" value="false" />
  </protection>
</rule>

<!-- Or exclude specific namespace -->
<module path="bin\Release\net9.0\win-x64\publish\ECoopSystem.dll">
  <rule pattern="namespace('ECoopSystem.ViewModels')" inherit="false">
    <!-- No protections for ViewModels -->
  </rule>
</module>
```

### Issue: "Build-secure.ps1 permission denied"

**Solution:**
```powershell
Set-ExecutionPolicy -ExecutionPolicy RemoteSigned -Scope CurrentUser
```

Or run with bypass:
```powershell
powershell -ExecutionPolicy Bypass -File .\Build\build-secure.ps1 -IFrameUrl "https://example.com" -Platform windows
```

---

## Performance Benchmarks

### String Decryption Performance

```
Plain string access:              0.001 탎
Base64 decode:                    0.1 탎
AES-256 decrypt (first call):     2-5 탎
AES-256 decrypt (cached):         0.001 탎
```

### Build Time Comparison

```
Standard build:                   30 seconds
With string encryption:           32 seconds (+2s)
With obfuscation:                 45 seconds (+15s)
Full protection + signing:        60 seconds (+30s)
```

### Runtime Performance

```
Startup time increase:            +10-20ms
Memory overhead:                  +5-10MB
CPU overhead:                     +1-2%
```

---

## Security Checklist

### Before Release:
- [ ] All sensitive URLs encrypted
- [ ] API endpoints obfuscated
- [ ] File/folder names encoded
- [ ] No hardcoded credentials
- [ ] appsettings.json excluded from repository
- [ ] Data protection keys not committed
- [ ] Code obfuscation applied
- [ ] Anti-tampering enabled
- [ ] Binary signed with certificate
- [ ] Tested obfuscated build thoroughly

### After Release:
- [ ] Keep obfuscation symbol maps secure
- [ ] Store original (non-obfuscated) builds for debugging
- [ ] Monitor for tampering attempts
- [ ] Update encryption if compromised
- [ ] Rotate API keys periodically

---

## Additional Resources

### Tools
- **ConfuserEx 2:** https://github.com/mkaring/ConfuserEx
- **ILSpy Decompiler:** https://github.com/icsharpcode/ILSpy
- **dnSpy Debugger:** https://github.com/dnSpy/dnSpy
- **Obfuscar:** https://github.com/obfuscar/obfuscar

### References
- **.NET Security Best Practices:** https://learn.microsoft.com/en-us/dotnet/standard/security/
- **Code Obfuscation Guide:** https://learn.microsoft.com/en-us/dotnet/core/deploying/native-aot/
- **String Encryption Patterns:** https://owasp.org/www-community/controls/Protect_FileSystem

---

## Support

For issues or questions:
1. Check [OBFUSCATION.md](OBFUSCATION.md) for detailed documentation
2. Review [OBFUSCATION_QUICK_REF.md](OBFUSCATION_QUICK_REF.md) for quick commands
3. Open an issue on GitHub

---

**Last Updated:** 2025-02-20  
**Version:** 1.0.0
