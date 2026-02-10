using ECoopSystem.Services;
using Microsoft.AspNetCore.DataProtection;
using System;
using System.IO;

namespace ECoopSystem.Stores;

public sealed class SecretKeyStore
{
    private readonly string _filePath;
    private readonly IDataProtector _protector;

    public SecretKeyStore()
    {
        var dir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), 
            "ECoopSystem");

        Directory.CreateDirectory(dir);
        _filePath = Path.Combine(dir, "secret.dat");

        _protector = DataProtectionProviderFactory.CreateProtector();
    }

    public bool HasSecret() => File.Exists(_filePath);

    public void Save(string secretKey)
    {
        var protectedValue = _protector.Protect(secretKey);
        File.WriteAllText(_filePath, protectedValue);
    }
    
    public string? Load()
    {
        if (!File.Exists(_filePath))
            return null;

        try
        {
            var protectedValue = File.ReadAllText(_filePath);
            return _protector.Unprotect(protectedValue);
        }
        catch
        {
            return null;
        }
    }

    public void Delete()
    {
        if (File.Exists(_filePath))
            File.Delete(_filePath);
    }
}
