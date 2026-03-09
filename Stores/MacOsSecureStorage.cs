using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;

namespace ECoopSystem.Stores;

/// <summary>
/// macOS secure storage implementation using Keychain Services
/// </summary>
public class MacOsSecureStorage : ISecureStorage
{
    private readonly string _purpose;
    private readonly string _serviceName;
    private readonly string _accountName;

    public MacOsSecureStorage(string purpose)
    {
        _purpose = purpose;
        _serviceName = "org.landshorizon.ecoopsystem";
        _accountName = purpose;
    }

    public string Protect(string plainText)
    {
        // Use fallback encryption as primary method to avoid process-related crashes
        // Keychain integration can be enabled later once tested thoroughly
        return EncryptWithMachineKey(plainText);
        
        /* Disabled for stability - re-enable after testing
        try
        {
            // Try to use macOS Keychain via security command
            if (TryStoreInKeychain(plainText))
            {
                // Return a marker indicating data is in keychain
                return $"KEYCHAIN:{_accountName}";
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"macOS SecureStorage: Keychain not available, using fallback: {ex.Message}");
        }

        // Fallback: Return encrypted data
        return EncryptWithMachineKey(plainText);
        */
    }

    public string Unprotect(string protectedText)
    {
        // Check if data was stored in keychain (legacy format)
        if (protectedText.StartsWith("KEYCHAIN:"))
        {
            try
            {
                var accountName = protectedText.Substring(9);
                var retrieved = RetrieveFromKeychain(accountName);
                if (retrieved != null)
                    return retrieved;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"macOS SecureStorage: Failed to retrieve from keychain: {ex.Message}");
            }

            throw new CryptographicException("Failed to retrieve from keychain");
        }

        // Default: Decrypt data
        return DecryptWithMachineKey(protectedText);
    }

    private bool TryStoreInKeychain(string value)
    {
        Process? deleteProcess = null;
        Process? addProcess = null;
        
        try
        {
            // Delete existing entry first (if any)
            var deleteInfo = new ProcessStartInfo
            {
                FileName = "security",
                Arguments = $"delete-generic-password -s \"{_serviceName}\" -a \"{_accountName}\"",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            deleteProcess = Process.Start(deleteInfo);
            if (deleteProcess != null)
            {
                if (!deleteProcess.WaitForExit(5000))
                {
                    try { deleteProcess.Kill(); } catch { }
                }
                deleteProcess.Dispose();
            }
            
            // Add new entry
            var addInfo = new ProcessStartInfo
            {
                FileName = "security",
                Arguments = $"add-generic-password -s \"{_serviceName}\" -a \"{_accountName}\" -w \"{EscapeForShell(value)}\" -U",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            addProcess = Process.Start(addInfo);
            if (addProcess == null)
                return false;

            if (!addProcess.WaitForExit(5000))
            {
                try { addProcess.Kill(); } catch { }
                return false;
            }

            return addProcess.ExitCode == 0;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"macOS SecureStorage: Keychain store failed: {ex.Message}");
            return false;
        }
        finally
        {
            deleteProcess?.Dispose();
            addProcess?.Dispose();
        }
    }

    private string? RetrieveFromKeychain(string accountName)
    {
        Process? process = null;
        try
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = "security",
                Arguments = $"find-generic-password -s \"{_serviceName}\" -a \"{accountName}\" -w",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            process = Process.Start(startInfo);
            if (process == null)
                return null;

            var output = process.StandardOutput.ReadToEnd().Trim();
            
            if (!process.WaitForExit(5000))
            {
                try { process.Kill(); } catch { }
                return null;
            }

            return process.ExitCode == 0 ? output : null;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"macOS SecureStorage: Keychain lookup failed: {ex.Message}");
            return null;
        }
        finally
        {
            process?.Dispose();
        }
    }

    private string EscapeForShell(string input)
    {
        // Escape special characters for shell
        return input.Replace("\"", "\\\"").Replace("$", "\\$").Replace("`", "\\`");
    }

    private string EncryptWithMachineKey(string plainText)
    {
        try
        {
            var key = GetMachineKey();
            var plainBytes = Encoding.UTF8.GetBytes(plainText);
            var combined = SafeAes.Encrypt(key, plainBytes);
            return Convert.ToBase64String(combined);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"macOS SecureStorage: Encryption failed: {ex}");
            throw new CryptographicException("Failed to encrypt data", ex);
        }
    }

    private string DecryptWithMachineKey(string protectedText)
    {
        try
        {
            var key = GetMachineKey();
            var combined = Convert.FromBase64String(protectedText);
            var plainBytes = SafeAes.Decrypt(key, combined);
            return Encoding.UTF8.GetString(plainBytes);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"macOS SecureStorage: Decryption failed: {ex}");
            throw new CryptographicException("Failed to decrypt data", ex);
        }
    }

    private byte[] GetMachineKey()
    {
        try
        {
            // Derive a key from machine-specific identifiers
            var machineId = GetMachineId();
            
            if (string.IsNullOrWhiteSpace(machineId))
            {
                Debug.WriteLine("macOS SecureStorage: Warning - empty machine ID, using fallback");
                machineId = $"{Environment.MachineName}:{Environment.UserName}:fallback";
            }
            
            using var sha256 = SHA256.Create();
            var keyMaterial = Encoding.UTF8.GetBytes($"{machineId}:{_purpose}");
            return sha256.ComputeHash(keyMaterial);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"macOS SecureStorage: GetMachineKey failed: {ex}");
            // Last resort fallback
            using var sha256 = SHA256.Create();
            return sha256.ComputeHash(Encoding.UTF8.GetBytes($"emergency-fallback:{_purpose}"));
        }
    }

    private string GetMachineId()
    {
        Process? process = null;
        try
        {
            // Get macOS hardware UUID
            var startInfo = new ProcessStartInfo
            {
                FileName = "ioreg",
                Arguments = "-rd1 -c IOPlatformExpertDevice",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            process = Process.Start(startInfo);
            if (process != null)
            {
                var output = process.StandardOutput.ReadToEnd();
                
                if (!process.WaitForExit(5000))
                {
                    try { process.Kill(); } catch { }
                    return $"{Environment.MachineName}:{Environment.UserName}";
                }

                // Extract IOPlatformUUID
                var lines = output.Split('\n');
                foreach (var line in lines)
                {
                    if (line.Contains("IOPlatformUUID"))
                    {
                        var parts = line.Split('=');
                        if (parts.Length == 2)
                        {
                            return parts[1].Trim().Trim('"');
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"macOS SecureStorage: Failed to get machine ID: {ex.Message}");
        }
        finally
        {
            process?.Dispose();
        }

        // Fallback to hostname + user
        return $"{Environment.MachineName}:{Environment.UserName}";
    }
}
