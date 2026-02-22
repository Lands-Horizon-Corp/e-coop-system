# String Encryption & Obfuscation Quick Reference

## Encrypt a String

### PowerShell
```powershell
.\Build\encrypt-string.ps1 -PlainText "your-sensitive-data"
```

### Python
```bash
python Build/encrypt-string.py "your-sensitive-data"
```

## Build with Encryption

### Windows
```powershell
.\Build\build-secure.ps1 -IFrameUrl "https://example.com" -ApiUrl "https://api.example.com" -Platform windows
```

### Skip Obfuscation (Faster)
```powershell
.\Build\build-secure.ps1 -IFrameUrl "https://example.com" -Platform windows -SkipObfuscation
```

## Common Encrypted Strings

| Plain Text | Encrypted (Base64) | Usage |
|------------|-------------------|-------|
| `ECoopSystem` | `RUNvb3BTeXN0ZW0=` | Folder name |
| `secret.dat` | `c2VjcmV0LmRhdA==` | Secret key file |
| `appstate.dat` | `YXBwc3RhdGUuZGF0` | App state file |
| `/web/api/v1/license/activate` | `L3dlYi9hcGkvdjEvbGljZW5zZS9hY3RpdmF0ZQ==` | Activation endpoint |
| `/web/api/v1/license/verify` | `L3dlYi9hcGkvdjEvbGljZW5zZS92ZXJpZnk=` | Verification endpoint |

## Use Encrypted Strings in Code

```csharp
using System;

// Simple Base64 encoding (for non-critical strings)
const string enc = "RUNvb3BTeXN0ZW0=";
var plain = System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(enc));

// AES-256 encryption (for critical strings)
using ECoopSystem.Utilities;
const string enc2 = "hGx7K3mP9qR2tV..."; // From encrypt-string tool
var plain2 = StringEncryption.Decrypt(enc2);
```

## Obfuscation Tools

### ConfuserEx 2 (Recommended)
- **Download:** https://github.com/mkaring/ConfuserEx/releases
- **Install to:** `.\Tools\ConfuserEx\`
- **Free & Open Source**

### Alternative Tools
- **Obfuscar** - https://github.com/obfuscar/obfuscar
- **Eazfuscator.NET** - https://www.gapotchenko.com/eazfuscator.net
- **.NET Reactor** - https://www.eziriz.com/dotnet_reactor.htm

## Protection Checklist

### Before Release:
- [ ] Encrypt all URL strings
- [ ] Encrypt API endpoints
- [ ] Encrypt file names and folder names
- [ ] Encrypt data protection purpose strings
- [ ] Build with `build-secure.ps1`
- [ ] Run code obfuscation
- [ ] Test obfuscated build
- [ ] Verify no plain text sensitive data in binary

### Verify Protection:
```powershell
# Check for plain text URLs in binary
Select-String -Path "bin\Release\net9.0\win-x64\publish\ECoopSystem.exe" -Pattern "api.example.com"

# Should return no results if properly encrypted
```

## Security Levels

### Level 1: Basic (Current)
- ? Base64 encoded strings
- ? Data protection for stored data
- Time: 0 min

### Level 2: Standard (Recommended)
- ? Base64 encoded strings
- ? AES-256 encrypted critical strings
- ? Symbol renaming obfuscation
- Time: 5 min

### Level 3: Advanced
- ? All Level 2 protections
- ? Control flow obfuscation
- ? Anti-tampering
- ? Anti-debugging
- Time: 10-15 min

### Level 4: Maximum
- ? All Level 3 protections
- ? Code virtualization
- ? Native compilation (NativeAOT)
- ? Custom packer
- Time: 30+ min

## Performance Impact

| Protection | Startup | Runtime | Binary Size |
|-----------|---------|---------|-------------|
| None | 0ms | 0% | 100% |
| Base64 | +1ms | +0.1% | +0% |
| AES-256 | +5ms | +0.5% | +0.1% |
| Obfuscation | +10ms | +1-3% | +5-10% |
| Full Protection | +20ms | +3-5% | +15-20% |

---

For detailed documentation, see [docs/OBFUSCATION.md](docs/OBFUSCATION.md)
