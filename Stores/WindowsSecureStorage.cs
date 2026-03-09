using Microsoft.AspNetCore.DataProtection;

namespace ECoopSystem.Stores;

/// <summary>
/// Windows secure storage implementation using DPAPI
/// </summary>
public class WindowsSecureStorage : ISecureStorage
{
    private readonly IDataProtector _protector;

    public WindowsSecureStorage(IDataProtectionProvider provider, string purpose)
    {
        _protector = provider.CreateProtector(purpose);
    }

    public string Protect(string plainText)
    {
        return _protector.Protect(plainText);
    }

    public string Unprotect(string protectedText)
    {
        return _protector.Unprotect(protectedText);
    }
}
