using Microsoft.AspNetCore.DataProtection;
using System;
using System.IO;

namespace ECoopSystem.Services;

public static class DataProtectionProviderFactory
{
    public static IDataProtector CreateProtector()
    {
        var keysDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "ECoopSystem",
            "dp-keys");

        Directory.CreateDirectory(keysDir);

        var provider = DataProtectionProvider.Create(new DirectoryInfo(keysDir));
        return provider.CreateProtector("ECoopSystem.SecretKey.v1");
    }
}
