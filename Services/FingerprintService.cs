using ECoopSystem.Stores;
using System;
using System.Security.Cryptography;
using System.Text;

namespace ECoopSystem.Services;

public static class FingerprintService
{
    public static string ComputeFingerprint(AppState state)
    {
        var machineId = MachineIdProvider.GetMachineId();
        var raw = $"{machineId}:{state.InstallationId}:{state.InsallationUnixTime}";

        using var sha = SHA256.Create();
        var hash = sha.ComputeHash(Encoding.UTF8.GetBytes(raw));

        return Convert.ToHexString(hash).ToLowerInvariant();
    }
}
