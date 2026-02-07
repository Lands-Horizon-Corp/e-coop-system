using ECoopSystem.Services;
using Microsoft.AspNetCore.DataProtection;
using System;
using System.Diagnostics;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace ECoopSystem.Stores;

public sealed class SecretKeyStore
{
    private readonly string _filePath;
    private readonly AppState _state;

    public SecretKeyStore(AppState state)
    {
        _state = state;

        var dir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), 
            "ECoopSystem");

        Directory.CreateDirectory(dir);
        _filePath = Path.Combine(dir, "secret.dat");

        Debug.WriteLine("AppState path: " + _filePath);
    }

    public bool HasSecret() => File.Exists(_filePath);

    public void Save(string secretKey)
    {
        try
        {
            Debug.WriteLine("SecretKeyStore.Save() called");
            Debug.WriteLine("Secret path: " + _filePath);

            var key = DeriveKey();
            var nonce = RandomNumberGenerator.GetBytes(12);
            var plaintext = Encoding.UTF8.GetBytes(secretKey);

            var ciphertext = new byte[plaintext.Length];
            var tag = new byte[16];

            using (var aes = new AesGcm(key, 16))
            {
                aes.Encrypt(nonce, plaintext, ciphertext, tag);
            }

            var payload = new SecretPayload
            {
                NonceB64 = Convert.ToBase64String(nonce),
                CipherB64 = Convert.ToBase64String(ciphertext),
                TagB64 = Convert.ToBase64String(tag)
            };

            File.WriteAllText(_filePath, JsonSerializer.Serialize(payload));
            Console.WriteLine("Secret saved successfully!");
        }
        catch (Exception ex)
        {
            Console.WriteLine("SecretKeyStore.Save FAILED: " + ex);
            throw; // keep for debugging; remove later
        }
    }

    public string? Load()
    {
        if (!File.Exists(_filePath))
            return null;

        try
        {
            var payload = JsonSerializer.Deserialize<SecretPayload>(File.ReadAllText(_filePath));
            if (payload == null)
                return null;

            var nonce = Convert.FromBase64String(payload.NonceB64);
            var cipherText = Convert.FromBase64String(payload.CipherB64);
            var tag = Convert.FromBase64String(payload.TagB64);

            var key = DeriveKey();
            var plainText = new byte[cipherText.Length];

            using (var aes = new AesGcm(key, 16))
            {
                aes.Decrypt(nonce, cipherText, tag, plainText);
            }

            return Encoding.UTF8.GetString(plainText);
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

    private byte[] DeriveKey()
    {
        var fp = FingerprintService.ComputeFingerprint(_state);
        using var sha = SHA256.Create();
        return sha.ComputeHash(Encoding.UTF8.GetBytes("ECoopSystem|" + fp));
    }

    private sealed class SecretPayload
    {
        public string NonceB64 { get; set; } = "";
        public string CipherB64 { get; set; } = "";
        public string TagB64 { get; set; } = "";
    }
}
