using System;
using System.Security.Cryptography;
using System.Text;

namespace ECoopSystem.Utilities;

/// <summary>
/// Provides string encryption/decryption for protecting sensitive data in compiled code
/// </summary>
public static class StringEncryption
{
    private static readonly byte[] Key = DeriveKey();
    private static readonly byte[] IV = DeriveIV();

    /// <summary>
    /// Decrypts an encrypted string at runtime
    /// </summary>
    public static string Decrypt(string encryptedBase64)
    {
        if (string.IsNullOrEmpty(encryptedBase64))
            return string.Empty;

        try
        {
            var encryptedBytes = Convert.FromBase64String(encryptedBase64);
            
            using var aes = Aes.Create();
            aes.Key = Key;
            aes.IV = IV;
            aes.Mode = CipherMode.CBC;
            aes.Padding = PaddingMode.PKCS7;

            using var decryptor = aes.CreateDecryptor(aes.Key, aes.IV);
            var decryptedBytes = decryptor.TransformFinalBlock(encryptedBytes, 0, encryptedBytes.Length);
            
            return Encoding.UTF8.GetString(decryptedBytes);
        }
        catch
        {
            return string.Empty;
        }
    }

    /// <summary>
    /// Encrypts a string for embedding in source code (use in build tools)
    /// </summary>
    public static string Encrypt(string plainText)
    {
        if (string.IsNullOrEmpty(plainText))
            return string.Empty;

        using var aes = Aes.Create();
        aes.Key = Key;
        aes.IV = IV;
        aes.Mode = CipherMode.CBC;
        aes.Padding = PaddingMode.PKCS7;

        using var encryptor = aes.CreateEncryptor(aes.Key, aes.IV);
        var plainBytes = Encoding.UTF8.GetBytes(plainText);
        var encryptedBytes = encryptor.TransformFinalBlock(plainBytes, 0, plainBytes.Length);
        
        return Convert.ToBase64String(encryptedBytes);
    }

    private static byte[] DeriveKey()
    {
        // Derive from multiple machine-independent but hard-to-reverse sources
        var data = "ECoopSystem.SecureKey.2026.LandsHorizon.v1";
        using var sha = SHA256.Create();
        return sha.ComputeHash(Encoding.UTF8.GetBytes(data));
    }

    private static byte[] DeriveIV()
    {
        var data = "ECoopSystem.IV.2026.SecureInit";
        using var sha = SHA256.Create();
        var hash = sha.ComputeHash(Encoding.UTF8.GetBytes(data));
        var iv = new byte[16];
        Array.Copy(hash, iv, 16);
        return iv;
    }

    /// <summary>
    /// Helper extension method for easy decryption
    /// </summary>
    public static string DecryptString(this string encryptedBase64) => Decrypt(encryptedBase64);
}
