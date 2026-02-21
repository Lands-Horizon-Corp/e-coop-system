# Configuration Quick Reference

## Update Settings Without Rebuilding

### Change API URL
Edit `appsettings.json`:
```json
{
  "ApiSettings": {
    "BaseUrl": "https://your-new-api.com/"
  }
}
```

### Change WebView URL
Edit `appsettings.json`:
```json
{
  "WebViewSettings": {
    "BaseUrl": "https://your-new-client.com/"
  }
}
```

### Add Trusted Domains
Edit `appsettings.json`:
```json
{
  "WebViewSettings": {
    "TrustedDomains": [
      "yourdomain.com",
      "api.yourdomain.com",
      "admin.yourdomain.com"
    ]
  }
}
```

### Change Security Settings
Edit `appsettings.json`:
```json
{
  "Security": {
    "GracePeriodDays": 14,
    "MaxActivationAttempts": 5,
    "LockoutMinutes": 10
  }
}
```

## File Locations

Configuration files can be placed in:
1. Application directory (next to .exe)
2. `%APPDATA%/ECoopSystem/` (Windows)
3. `~/.config/ECoopSystem/` (Linux/macOS)

## Environment-Specific

- **Debug builds** ? Uses `appsettings.Development.json`
- **Release builds** ? Uses `appsettings.json`

## Access in Code

```csharp
using ECoopSystem.Configuration;

var config = ConfigurationLoader.Current;
var url = config.ApiSettings.BaseUrl;
```

## Reload Configuration

```csharp
ConfigurationLoader.Reload();
```

---

For full documentation, see [docs/CONFIGURATION.md](docs/CONFIGURATION.md)
