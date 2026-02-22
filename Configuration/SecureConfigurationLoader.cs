using System;
using System.Text.Json;
using ECoopSystem.Utilities;

namespace ECoopSystem.Configuration;

/// <summary>
/// Secure configuration loader with support for encrypted configuration values.
/// Extends ConfigurationLoader to decrypt sensitive configuration data.
/// </summary>
public static class SecureConfigurationLoader
{
    /// <summary>
    /// Load configuration with encrypted values support
    /// </summary>
    public static AppConfiguration LoadSecure()
    {
        var config = ConfigurationLoader.Current;
        
        // If URLs in config are encrypted (start with "encrypted:"), decrypt them
        if (config.ApiSettings.BaseUrl.StartsWith("encrypted:"))
        {
            var encrypted = config.ApiSettings.BaseUrl.Substring(10); // Remove "encrypted:" prefix
            config.ApiSettings.BaseUrl = StringEncryption.Decrypt(encrypted);
        }
        
        if (config.WebViewSettings.BaseUrl.StartsWith("encrypted:"))
        {
            var encrypted = config.WebViewSettings.BaseUrl.Substring(10);
            config.WebViewSettings.BaseUrl = StringEncryption.Decrypt(encrypted);
        }
        
        // Decrypt trusted domains if they're encrypted
        for (int i = 0; i < config.WebViewSettings.TrustedDomains.Count; i++)
        {
            if (config.WebViewSettings.TrustedDomains[i].StartsWith("encrypted:"))
            {
                var encrypted = config.WebViewSettings.TrustedDomains[i].Substring(10);
                config.WebViewSettings.TrustedDomains[i] = StringEncryption.Decrypt(encrypted);
            }
        }
        
        return config;
    }
    
    /// <summary>
    /// Encrypt a configuration value for use in appsettings.json
    /// </summary>
    public static string EncryptConfigValue(string plainText)
    {
        var encrypted = StringEncryption.Encrypt(plainText);
        return $"encrypted:{encrypted}";
    }
}

/// <summary>
/// Example usage of encrypted configuration values
/// </summary>
public class EncryptedConfigExample
{
    public static void Example()
    {
        // Example 1: Create encrypted appsettings.json
        Console.WriteLine("=== Example: Encrypted Configuration ===\n");
        
        // Encrypt sensitive values
        var encryptedApiUrl = SecureConfigurationLoader.EncryptConfigValue("https://api.production.com/");
        var encryptedClientUrl = SecureConfigurationLoader.EncryptConfigValue("https://client.production.com/");
        
        Console.WriteLine("Add these to appsettings.json:");
        Console.WriteLine($"  \"BaseUrl\": \"{encryptedApiUrl}\"");
        Console.WriteLine($"  \"BaseUrl\": \"{encryptedClientUrl}\"");
        Console.WriteLine();
        
        // Example 2: Load and decrypt
        var config = SecureConfigurationLoader.LoadSecure();
        Console.WriteLine($"Decrypted API URL: {config.ApiSettings.BaseUrl}");
        Console.WriteLine($"Decrypted Client URL: {config.WebViewSettings.BaseUrl}");
    }
}

/// <summary>
/// Example appsettings.json with encrypted values:
/// 
/// {
///   "ApiSettings": {
///     "BaseUrl": "encrypted:gH7xK3mP9qR2tV4wX8zA1cBnFgDe5sL2...",
///     "Timeout": 12
///   },
///   "WebViewSettings": {
///     "BaseUrl": "encrypted:iHy8L4nQ0rS3uW5xY9zA2dCnGhFe6tM3...",
///     "TrustedDomains": [
///       "encrypted:jIz9M5oR1sT4vX6yZ0aB3eCoDgGf7uN4...",
///       "encrypted:kJa0N6pS2tU5wY7zA1bC4fDpEhHg8vO5..."
///     ]
///   }
/// }
/// 
/// When loaded, all "encrypted:" prefixed values are automatically decrypted.
/// </summary>
