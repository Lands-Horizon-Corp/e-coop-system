using Microsoft.Win32;
using System;
using System.Diagnostics;
using System.IO;

namespace ECoopSystem.Services;

public static class MachineIdProvider
{
    public static string GetMachineId()
    {
        try
        {
            if (OperatingSystem.IsWindows())
            {
                using var key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Cryptography");
                var value = key?.GetValue("MachineGuid")?.ToString();
                if (!string.IsNullOrEmpty(value))
                    return value;
            }
            else if (OperatingSystem.IsLinux())
            {
                var path = "/etc/machine-id";
                if (File.Exists(path))
                {
                    var value = File.ReadAllText(path).Trim();
                    if (!string.IsNullOrEmpty(value))
                        return value;
                }
            }
            else if (OperatingSystem.IsMacOS())
            {
                var psi = new ProcessStartInfo
                {
                    FileName = "sh",
                    Arguments = "-c \"ioreg -rd1 -c IOPlatformExpertDevice | awk -F\\\" '/IOPlatformUUID/ {print $4}'\"",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                using var p = Process.Start(psi);
                if (p != null)
                {
                    var output = p.StandardOutput.ReadToEnd().Trim();
                    p.WaitForExit(1500);
                    if (!string.IsNullOrEmpty(output))
                        return output;
                }
            }
        }
        catch
        {

        }

        return Environment.MachineName;
    }
}
