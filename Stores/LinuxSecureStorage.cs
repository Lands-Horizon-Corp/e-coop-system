using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;

namespace ECoopSystem.Stores;

/// <summary>
/// Linux secure storage implementation using libsecret (GNOME Keyring/KWallet)
/// Falls back to encrypted file storage if libsecret is unavailable
/// </summary>
public class LinuxSecureStorage : ISecureStorage
{
    private readonly string _purpose;
    private readonly string _label;
    private const string Schema = "org.landshorizon.ecoopsystem";

    public LinuxSecureStorage(string purpose)
    {
        _purpose = purpose;
        _label = $"ECoopSystem-{purpose}";
    }

    public string Protect(string plainText)
    {
        // Use fallback encryption as primary method to avoid process-related crashes
        // libsecret integration can be enabled later once tested thoroughly
        return EncryptWithMachineKey(plainText);
        
        /* Disabled for stability - re-enable after testing
        try
        {
            // Try to use libsecret first
            if (TryStoreWithLibsecret(plainText))
            {
                // Return a marker indicating data is in keyring
                return $"KEYRING:{_label}";
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Linux SecureStorage: libsecret not available, using fallback: {ex.Message}");
        }

        // Fallback: Encrypt using machine-bound key
        return EncryptWithMachineKey(plainText);
        */
    }

    public string Unprotect(string protectedText)
    {
        if (string.IsNullOrEmpty(protectedText))
            throw new CryptographicException("Protected data cannot be null or empty");

        var encryptedValue = protectedText;

        try
        {
            if (OperatingSystem.IsLinux())
                Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] LinuxSecureStorage: Unprotect called, data length: {encryptedValue.Length}");

            // Check if data was stored in keyring (legacy format)
            if (encryptedValue.StartsWith("KEYRING:"))
            {
                if (OperatingSystem.IsLinux())
                    Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] LinuxSecureStorage: KEYRING format detected");

                try
                {
                    var label = encryptedValue.Substring(8);
                    var retrieved = RetrieveWithLibsecret(label);
                    if (retrieved != null)
                        return retrieved;
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Linux SecureStorage: Failed to retrieve from keyring: {ex.Message}");
                    if (OperatingSystem.IsLinux())
                        Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] LinuxSecureStorage: Keyring retrieval failed: {ex.Message}");
                }

                throw new CryptographicException("Failed to retrieve from keyring");
            }

            if (OperatingSystem.IsLinux())
                Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] LinuxSecureStorage: Using DecryptWithMachineKey...");

            // Default: Decrypt using machine-bound key
            var result = DecryptWithMachineKey(encryptedValue);
            
            if (OperatingSystem.IsLinux())
                Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] LinuxSecureStorage: Unprotect succeeded");

            return result;
        }
        catch (Exception ex)
        {
            if (OperatingSystem.IsLinux())
            {
                Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] LinuxSecureStorage: Unprotect FAILED: {ex.Message}");
                Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] LinuxSecureStorage: Exception type: {ex.GetType().Name}");
            }
            throw;
        }
    }

    private bool TryStoreWithLibsecret(string value)
    {
        Process? process = null;
        try
        {
            // Use secret-tool command line utility (part of libsecret)
            var startInfo = new ProcessStartInfo
            {
                FileName = "secret-tool",
                Arguments = $"store --label=\"{_label}\" application \"{Schema}\" purpose \"{_purpose}\"",
                RedirectStandardInput = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            process = Process.Start(startInfo);
            if (process == null)
                return false;

            // Write the value to stdin
            process.StandardInput.Write(value);
            process.StandardInput.Flush();
            process.StandardInput.Close();
            
            // Wait for completion with timeout
            if (!process.WaitForExit(5000))
            {
                try { process.Kill(); } catch { }
                return false;
            }

            return process.ExitCode == 0;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"LinuxSecureStorage: secret-tool store failed: {ex.Message}");
            return false;
        }
        finally
        {
            process?.Dispose();
        }
    }

    private string? RetrieveWithLibsecret(string label)
    {
        Process? process = null;
        try
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = "secret-tool",
                Arguments = $"lookup application \"{Schema}\" purpose \"{_purpose}\"",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            process = Process.Start(startInfo);
            if (process == null)
                return null;

            var output = process.StandardOutput.ReadToEnd();
            
            // Wait for completion with timeout
            if (!process.WaitForExit(5000))
            {
                try { process.Kill(); } catch { }
                return null;
            }

            return process.ExitCode == 0 ? output : null;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"LinuxSecureStorage: secret-tool lookup failed: {ex.Message}");
            return null;
        }
        finally
        {
            process?.Dispose();
        }
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
            Debug.WriteLine($"LinuxSecureStorage: Encryption failed: {ex}");
            throw new CryptographicException("Failed to encrypt data", ex);
        }
    }

    private string DecryptWithMachineKey(string protectedText)
    {
        try
        {
            if (OperatingSystem.IsLinux())
                Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] LinuxSecureStorage: DecryptWithMachineKey - Getting machine key...");

            var key = GetMachineKey();
            
            if (OperatingSystem.IsLinux())
                Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] LinuxSecureStorage: DecryptWithMachineKey - Converting from Base64...");

            var combined = Convert.FromBase64String(protectedText);
            
            if (OperatingSystem.IsLinux())
                Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] LinuxSecureStorage: DecryptWithMachineKey - Calling SafeAes.Decrypt...");

            var plainBytes = SafeAes.Decrypt(key, combined);
            
            if (OperatingSystem.IsLinux())
                Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] LinuxSecureStorage: DecryptWithMachineKey - Converting to string...");

            return Encoding.UTF8.GetString(plainBytes);
        }
        catch (Exception ex)
        {
            if (OperatingSystem.IsLinux())
            {
                Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] LinuxSecureStorage: DecryptWithMachineKey FAILED: {ex.Message}");
                Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] LinuxSecureStorage: Stack: {ex.StackTrace}");
            }

            Debug.WriteLine($"LinuxSecureStorage: Decryption failed: {ex}");
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
                Debug.WriteLine("LinuxSecureStorage: Warning - empty machine ID, using fallback");
                machineId = $"{Environment.MachineName}:{Environment.UserName}:fallback";
            }
            
            using var sha256 = SHA256.Create();
            var keyMaterial = Encoding.UTF8.GetBytes($"{machineId}:{_purpose}");
            return sha256.ComputeHash(keyMaterial);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"LinuxSecureStorage: GetMachineKey failed: {ex}");
            // Last resort fallback
            using var sha256 = SHA256.Create();
            return sha256.ComputeHash(Encoding.UTF8.GetBytes($"emergency-fallback:{_purpose}"));
        }
    }

    private string GetMachineId()
    {
        try
        {
            // Try /etc/machine-id first (systemd)
            if (File.Exists("/etc/machine-id"))
            {
                var id = File.ReadAllText("/etc/machine-id").Trim();
                if (!string.IsNullOrWhiteSpace(id))
                {
                    Debug.WriteLine($"LinuxSecureStorage: Using /etc/machine-id");
                    return id;
                }
            }

            // Try /var/lib/dbus/machine-id (older systems)
            if (File.Exists("/var/lib/dbus/machine-id"))
            {
                var id = File.ReadAllText("/var/lib/dbus/machine-id").Trim();
                if (!string.IsNullOrWhiteSpace(id))
                {
                    Debug.WriteLine($"LinuxSecureStorage: Using /var/lib/dbus/machine-id");
                    return id;
                }
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"LinuxSecureStorage: Failed to read machine-id: {ex.Message}");
        }

        // Fallback to hostname + user
        var fallback = $"{Environment.MachineName}:{Environment.UserName}";
        Debug.WriteLine($"LinuxSecureStorage: Using fallback machine ID");
        return fallback;
    }
}
