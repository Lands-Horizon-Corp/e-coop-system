using Microsoft.AspNetCore.DataProtection;
using System;
using System.IO;

namespace ECoopSystem.Stores;

public sealed class SecretKeyStore
{
    private readonly string _filePath;
    private readonly IDataProtector _protector;

    public SecretKeyStore(IDataProtectionProvider provider)
    {
        // Encrypted folder name and file name
        const string encFolderName = "RUNvb3BTeXN0ZW0="; // "ECoopSystem"
        const string encFileName = "c2VjcmV0LmRhdA=="; // "secret.dat"
        const string encPurpose = "RUNvb3BTeXN0ZW0uU2VjcmV0S2V5LnYx"; // "ECoopSystem.SecretKey.v1"
        
        var folderName = System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(encFolderName));
        var fileName = System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(encFileName));
        var purpose = System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(encPurpose));
        
        var dir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), 
            folderName);

        Directory.CreateDirectory(dir);
        _filePath = Path.Combine(dir, fileName);

        _protector = provider.CreateProtector(purpose);
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
