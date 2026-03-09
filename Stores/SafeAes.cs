using System;
using System;
using System.Diagnostics;
using System.Security.Cryptography;

namespace ECoopSystem.Stores;

/// <summary>
/// Safe AES wrapper that handles platform-specific issues
/// </summary>
internal static class SafeAes
{
    public static Aes CreateAes()
    {
        try
        {
            // Try to create AES instance
            var aes = Aes.Create();
            
            // Verify it works by setting a dummy key
            aes.KeySize = 256;
            aes.BlockSize = 128;
            aes.Mode = CipherMode.CBC;
            aes.Padding = PaddingMode.PKCS7;
            
            return aes;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"SafeAes: Failed to create standard AES: {ex}");
            
            // Try explicit implementation
            try
            {
                var aes = new AesCryptoServiceProvider();
                aes.KeySize = 256;
                aes.BlockSize = 128;
                aes.Mode = CipherMode.CBC;
                aes.Padding = PaddingMode.PKCS7;
                return aes;
            }
            catch (Exception ex2)
            {
                Debug.WriteLine($"SafeAes: Failed to create AesCryptoServiceProvider: {ex2}");
                
                // Last resort: AesManaged (slower but more compatible)
                var aes = new AesManaged();
                aes.KeySize = 256;
                aes.BlockSize = 128;
                aes.Mode = CipherMode.CBC;
                aes.Padding = PaddingMode.PKCS7;
                return aes;
            }
        }
    }
    
    public static byte[] Encrypt(byte[] key, byte[] plainBytes)
    {
        if (key == null || key.Length != 32)
            throw new ArgumentException("Key must be 32 bytes for AES-256", nameof(key));
            
        if (plainBytes == null || plainBytes.Length == 0)
            throw new ArgumentException("Data cannot be empty", nameof(plainBytes));
        
        using var aes = CreateAes();
        aes.Key = key;
        aes.GenerateIV();
        
        byte[] encryptedBytes;
        using (var encryptor = aes.CreateEncryptor())
        {
            encryptedBytes = encryptor.TransformFinalBlock(plainBytes, 0, plainBytes.Length);
        }
        
        // Combine IV + encrypted data
        var result = new byte[aes.IV.Length + encryptedBytes.Length];
        Buffer.BlockCopy(aes.IV, 0, result, 0, aes.IV.Length);
        Buffer.BlockCopy(encryptedBytes, 0, result, aes.IV.Length, encryptedBytes.Length);
        
        return result;
    }
    
    public static byte[] Decrypt(byte[] key, byte[] combined)
    {
        try
        {
            if (OperatingSystem.IsLinux())
                Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] SafeAes: Decrypt - Validating inputs...");

            if (key == null || key.Length != 32)
                throw new ArgumentException("Key must be 32 bytes for AES-256", nameof(key));
                
            if (combined == null || combined.Length < 16)
                throw new ArgumentException("Encrypted data is too short", nameof(combined));
            
            if (OperatingSystem.IsLinux())
                Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] SafeAes: Decrypt - Creating AES...");

            using var aes = CreateAes();
            
            if (OperatingSystem.IsLinux())
                Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] SafeAes: Decrypt - AES type: {aes.GetType().Name}");

            aes.Key = key;
            
            // Extract IV (first 16 bytes for AES)
            var ivLength = 16;
            if (combined.Length < ivLength)
                throw new CryptographicException("Invalid encrypted data - too short for IV");
            
            if (OperatingSystem.IsLinux())
                Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] SafeAes: Decrypt - Extracting IV...");

            var iv = new byte[ivLength];
            var encryptedBytes = new byte[combined.Length - ivLength];
            
            Buffer.BlockCopy(combined, 0, iv, 0, ivLength);
            Buffer.BlockCopy(combined, ivLength, encryptedBytes, 0, encryptedBytes.Length);
            
            aes.IV = iv;
            
            if (OperatingSystem.IsLinux())
                Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] SafeAes: Decrypt - Creating decryptor...");

            byte[] plainBytes;
            using (var decryptor = aes.CreateDecryptor())
            {
                if (OperatingSystem.IsLinux())
                    Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] SafeAes: Decrypt - Calling TransformFinalBlock...");

                plainBytes = decryptor.TransformFinalBlock(encryptedBytes, 0, encryptedBytes.Length);
            }
            
            if (OperatingSystem.IsLinux())
                Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] SafeAes: Decrypt - Success, plaintext length: {plainBytes.Length}");

            return plainBytes;
        }
        catch (Exception ex)
        {
            if (OperatingSystem.IsLinux())
            {
                Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] SafeAes: Decrypt FAILED: {ex.Message}");
                Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] SafeAes: Exception type: {ex.GetType().Name}");
                Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] SafeAes: Stack: {ex.StackTrace}");
            }
            throw;
        }
    }
}
