using Microsoft.AspNetCore.DataProtection;
using System;
using System.IO;

namespace ECoopSystem.Stores;

public sealed class SecretKeyStore
{
    private static readonly string FolderName = System.Text.Encoding.UTF8.GetString(Convert.FromBase64String("RUNvb3BTeXN0ZW0="));
    private static readonly string FileName = System.Text.Encoding.UTF8.GetString(Convert.FromBase64String("c2VjcmV0LmRhdA=="));
    private static readonly string Purpose = System.Text.Encoding.UTF8.GetString(Convert.FromBase64String("RUNvb3BTeXN0ZW0uU2VjcmV0S2V5LnYx"));
    
    private readonly string _filePath;
    private readonly IDataProtector _protector;

    public SecretKeyStore(IDataProtectionProvider provider)
    {
        var dir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), 
            FolderName);

        Directory.CreateDirectory(dir);
        _filePath = Path.Combine(dir, FileName);

        _protector = provider.CreateProtector(Purpose);
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
            Delete();
            return null;
        }
    }

    public void Delete()
    {
        if (File.Exists(_filePath))
            File.Delete(_filePath);
    }
}
