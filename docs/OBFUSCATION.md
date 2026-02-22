# String Encryption & Code Obfuscation Guide

## Overview

ECoopSystem now includes built-in string encryption and code obfuscation support to protect sensitive data and intellectual property.

## Features

### 1. String Encryption
- ? Encrypts URLs, API endpoints, and sensitive strings
- ? AES-256-CBC encryption
- ? Strings decrypted only at runtime
- ? Keys derived from application-specific data (not stored in code)

### 2. Code Obfuscation
- ? Symbol renaming (classes, methods, properties)
- ? Control flow obfuscation
- ? Constant encryption
- ? Anti-tampering protection
- ? Anti-debugging measures
- ? Resource encryption

---

## Quick Start

### 1. Encrypt a String

**Using PowerShell:**
```powershell
.\Build\encrypt-string.ps1 -PlainText "https://api.example.com/"
```

**Using Python:**
```bash
python Build/encrypt-string.py "https://api.example.com/"
```

**Output:**
```
Original: https://api.example.com/
Encrypted: 7x9K2mP5qR8tV...

Use in C# code:
StringEncryption.Decrypt("7x9K2mP5qR8tV...")
```

### 2. Build with Encryption

```powershell
.\Build\build-secure.ps1 `
    -IFrameUrl "https://client.example.com" `
    -ApiUrl "https://api.example.com" `
    -Platform windows
```

This will:
1. ? Encrypt all URLs automatically
2. ? Generate `SecureBuildConfiguration.cs` with encrypted strings
3. ? Build the application
4. ? Apply code obfuscation (if ConfuserEx is installed)

### 3. Setup Code Obfuscation (Optional)

**Download ConfuserEx:**
1. Visit: https://github.com/mkaring/ConfuserEx/releases
2. Download latest release (ConfuserEx 2.x for .NET Core/9)
3. Extract to `.\Tools\ConfuserEx\`

**Or use Alternative:**
- **IntelliLock** - https://www.eziriz.com/intellilock.htm
- **.NET Reactor** - https://www.eziriz.com/dotnet_reactor.htm
- **Eazfuscator.NET** - https://www.gapotchenko.com/eazfuscator.net

---

## String Encryption Usage

### In Code

```csharp
using ECoopSystem.Utilities;

// Encrypt during development
var encrypted = StringEncryption.Encrypt("sensitive-data");
Console.WriteLine(encrypted); // Copy this to your code

// Use encrypted strings in production
private const string EncryptedApiKey = "hGx7K3mP9qR2tV...";
public string ApiKey => StringEncryption.Decrypt(EncryptedApiKey);
```

### Pre-encrypted String Repository

Use `EncryptedStrings.cs` for commonly used strings:

```csharp
using ECoopSystem.Utilities;

// Access pre-encrypted strings
var apiUrl = EncryptedStrings.DefaultApiUrl;
var endpoint = EncryptedStrings.ApiActivateEndpoint;
var folderName = EncryptedStrings.AppDataFolderName;
```

---

## Obfuscation Configuration

### ConfuserEx Settings

Edit `confuser.crproj` to customize:

```xml
<rule pattern="true" inherit="false">
  <!-- Anti-tampering -->
  <protection id="anti tamper" />
  
  <!-- Control flow obfuscation -->
  <protection id="ctrl flow" />
  
  <!-- Method call hiding -->
  <protection id="ref proxy" />
  
  <!-- Symbol renaming -->
  <protection id="rename">
    <argument name="mode" value="decodable" />
    <argument name="renPublic" value="true" />
  </protection>
  
  <!-- String/constant encryption -->
  <protection id="constants">
    <argument name="mode" value="dynamic" />
  </protection>
  
  <!-- Resource encryption -->
  <protection id="resources" />
</rule>
```

### Protection Levels

| Protection | Description | Performance Impact |
|------------|-------------|-------------------|
| **Anti Tamper** | Detects code modifications | Low |
| **Ctrl Flow** | Makes code logic harder to follow | Medium |
| **Ref Proxy** | Hides method calls | Low |
| **Rename** | Renames symbols to meaningless names | None |
| **Constants** | Encrypts string/numeric constants | Low-Medium |
| **Resources** | Encrypts embedded resources | Low |
| **Anti Debug** | Prevents debugger attachment | Low |
| **Anti Dump** | Prevents memory dumping | Low |

---

## Build Workflow

### Development Build (No Encryption)
```powershell
dotnet build -c Debug
```
- Uses plain strings
- No obfuscation
- Fast compilation

### Production Build (With Encryption)
```powershell
.\Build\build-secure.ps1 `
    -IFrameUrl "https://production.com" `
    -ApiUrl "https://api.production.com" `
    -Platform windows `
    -Configuration Release
```
- ? Strings encrypted
- ? Obfuscation applied
- ? Optimized for deployment

### Skip Obfuscation (Faster Builds)
```powershell
.\Build\build-secure.ps1 -SkipObfuscation -Platform windows
```

---

## What Gets Protected?

### Automatically Encrypted:
? API URLs (from build parameters)  
? WebView/Client URLs  
? Application name  
? Build configuration strings  

### Manually Encrypt:
- API keys (if any)
- Connection strings
- License validation endpoints
- Sensitive configuration values

### Obfuscated:
? All class names  
? Method names  
? Property names  
? String literals  
? Control flow logic  
? Embedded resources  

---

## Verification

### Test Encrypted Strings

**Encrypt:**
```powershell
.\Build\encrypt-string.ps1 -PlainText "test-data"
# Output: 7x9K2mP5qR8tV...
```

**Decrypt (verify):**
```bash
python Build/encrypt-string.py --decrypt "7x9K2mP5qR8tV..."
# Output: test-data
```

### Inspect Obfuscated Code

Use tools like:
- **dnSpy** - https://github.com/dnSpy/dnSpy
- **ILSpy** - https://github.com/icsharpcode/ILSpy
- **dotPeek** - https://www.jetbrains.com/decompiler/

**Before obfuscation:**
```csharp
public class LicenseService {
    public async Task<ActivateResult> ActivateAsync(string licenseKey)
}
```

**After obfuscation:**
```csharp
public class A {
    public async Task<B> a(string b)
}
```

---

## Best Practices

### 1. String Encryption
? **DO:**
- Encrypt production URLs
- Encrypt API endpoints
- Encrypt sensitive configuration keys
- Use `EncryptedStrings.cs` for shared encrypted values

? **DON'T:**
- Encrypt debug/development strings (affects debugging)
- Encrypt user-visible UI text
- Encrypt non-sensitive data (adds overhead)

### 2. Code Obfuscation
? **DO:**
- Apply to Release builds only
- Test obfuscated builds thoroughly
- Keep original builds for debugging
- Document obfuscation settings

? **DON'T:**
- Obfuscate Debug builds
- Apply to open-source dependencies
- Skip testing after obfuscation

### 3. Security Layers

**Defense in Depth:**
1. String encryption (compile-time)
2. Code obfuscation (post-build)
3. Data Protection API (runtime)
4. Certificate pinning (network)
5. License validation (application)

---

## Troubleshooting

### Encrypted string returns empty
- Ensure encryption key derivation matches exactly
- Check for whitespace in encrypted string
- Verify Base64 encoding is valid

### Obfuscation breaks application
- Exclude ViewModels with data binding: `[Obfuscation(Exclude = true)]`
- Exclude serialized classes
- Check ConfuserEx logs for errors

### Build-secure.ps1 errors
- Ensure PowerShell 7+ is installed
- Run: `Set-ExecutionPolicy -ExecutionPolicy RemoteSigned -Scope CurrentUser`
- Check .NET SDK is installed

### Python script missing dependencies
```bash
pip install cryptography
```

---

## Performance Impact

| Feature | Impact | Notes |
|---------|--------|-------|
| String Encryption | **Low** (< 1ms per string) | Decrypted once, cached |
| Code Obfuscation | **None** | Applied at build time |
| Anti-Tampering | **Low** | One-time check at startup |
| Anti-Debug | **Low** | Periodic checks |

---

## CI/CD Integration

### GitHub Actions Example

```yaml
name: Secure Build

on:
  push:
    tags:
      - 'v*'

jobs:
  build:
    runs-on: windows-latest
    steps:
      - uses: actions/checkout@v3
      
      - name: Setup .NET
        uses: actions/setup-dotnet@v3
        with:
          dotnet-version: '9.0.x'
      
      - name: Build with Encryption
        run: |
          .\Build\build-secure.ps1 `
            -IFrameUrl "${{ secrets.PROD_IFRAME_URL }}" `
            -ApiUrl "${{ secrets.PROD_API_URL }}" `
            -Platform windows `
            -SkipObfuscation
      
      - name: Upload Artifact
        uses: actions/upload-artifact@v3
        with:
          name: ECoopSystem-Windows-Secure
          path: bin/Release/net9.0/win-x64/publish/
```

---

## Additional Tools

### Recommended Obfuscators

1. **ConfuserEx 2** (Free, Open Source)
   - GitHub: https://github.com/mkaring/ConfuserEx
   - Best for: .NET Core/5/6/7/8/9
   - License: MIT

2. **Obfuscar** (Free, Open Source)
   - GitHub: https://github.com/obfuscar/obfuscar
   - Best for: Basic obfuscation
   - License: MIT

3. **Eazfuscator.NET** (Commercial)
   - Website: https://www.gapotchenko.com/eazfuscator.net
   - Best for: Enterprise applications
   - Free for personal use

4. **.NET Reactor** (Commercial)
   - Website: https://www.eziriz.com/dotnet_reactor.htm
   - Best for: Maximum protection
   - Trial available

### String Encryption Alternatives

For even stronger protection, consider:
- **Runtime string decryption** - Decrypt strings only when needed
- **Code virtualization** - Convert code to VM instructions
- **Native compilation** - Use NativeAOT for .NET 9

---

## Security Notes

?? **Important:**

1. **Obfuscation is not encryption** - It makes reverse engineering harder, not impossible
2. **Test thoroughly** - Obfuscation can break reflection-based code
3. **Keep source safe** - Obfuscation protects compiled code, not source
4. **Multiple layers** - Combine encryption + obfuscation + runtime protection

---

## Related Documentation

- [Build System](BUILD.md) - Standard build process
- [Configuration](CONFIGURATION.md) - Application configuration
- [Security Best Practices](../SECURITY.md) - Additional security measures

---

**Last Updated:** 2025-02-20
