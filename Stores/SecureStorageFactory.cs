using Microsoft.AspNetCore.DataProtection;
using System;

namespace ECoopSystem.Stores;

/// <summary>
/// Factory for creating platform-specific secure storage implementations
/// </summary>
public static class SecureStorageFactory
{
    public static ISecureStorage Create(IDataProtectionProvider provider, string purpose)
    {
        if (OperatingSystem.IsWindows())
        {
            return new WindowsSecureStorage(provider, purpose);
        }
        else if (OperatingSystem.IsLinux())
        {
            return new LinuxSecureStorage(purpose);
        }
        else if (OperatingSystem.IsMacOS())
        {
            return new MacOsSecureStorage(purpose);
        }
        else
        {
            throw new PlatformNotSupportedException("Secure storage is not supported on this platform");
        }
    }
}
