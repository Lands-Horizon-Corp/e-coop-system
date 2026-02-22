using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using ECoopSystem.Utilities;

namespace ECoopSystem.Examples;

/// <summary>
/// Example service showing best practices for string encryption
/// This demonstrates how to protect sensitive data in your code
/// </summary>
public class SecureServiceExample
{
    // ============================================================
    // PATTERN 1: Encrypted Constants (Recommended)
    // ============================================================
    
    // Encrypted API endpoint - use Build/encrypt-string.ps1 to generate
    private const string Enc_ApiEndpoint = "L3dlYi9hcGkvdjEvbGljZW5zZS9hY3RpdmF0ZQ==";
    
    // Encrypted configuration keys
    private const string Enc_ConfigKey_ApiUrl = "QXBpU2V0dGluZ3M6QmFzZVVybA==";
    private const string Enc_ConfigKey_Timeout = "QXBpU2V0dGluZ3M6VGltZW91dA==";
    
    // Encrypted folder/file names
    private const string Enc_DataFolder = "RUNvb3BTeXN0ZW0="; // "ECoopSystem"
    private const string Enc_ConfigFile = "Y29uZmlnLmpzb24="; // "config.json"
    
    // Decryption properties
    private static string ApiEndpoint => System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(Enc_ApiEndpoint));
    private static string ConfigKey_ApiUrl => System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(Enc_ConfigKey_ApiUrl));
    private static string ConfigKey_Timeout => System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(Enc_ConfigKey_Timeout));
    
    // ============================================================
    // PATTERN 2: Using EncryptedStrings Repository
    // ============================================================
    
    public void UsingEncryptedStringsRepository()
    {
        // Access pre-encrypted strings from centralized location
        var folderName = EncryptedStrings.AppDataFolderName;
        var fileName = EncryptedStrings.SecretKeyFilename;
        var apiEndpoint = EncryptedStrings.ApiActivateEndpoint;
        
        // Use them...
        var fullPath = System.IO.Path.Combine(folderName, fileName);
    }
    
    // ============================================================
    // PATTERN 3: AES-256 Encryption (For Critical Data)
    // ============================================================
    
    // For highly sensitive data, use AES-256 encryption
    // Generate using: .\Build\encrypt-string.ps1 -PlainText "your-sensitive-data"
    private const string Enc_DatabasePassword = "hGx7K3mP9qR2tV4wX8zA1cBnFgDe5sL2..."; // Example
    private const string Enc_ApiKey = "iHy8L4nQ0rS3uW5xY9zA2dCnGhFe6tM3..."; // Example
    
    private static string DatabasePassword => StringEncryption.Decrypt(Enc_DatabasePassword);
    private static string ApiKey => StringEncryption.Decrypt(Enc_ApiKey);
    
    // ============================================================
    // PATTERN 4: Runtime String Building
    // ============================================================
    
    public string BuildSecureUrl()
    {
        // Decrypt parts and combine at runtime
        const string encScheme = "aHR0cHM6Ly8="; // "https://"
        const string encHost = "YXBpLmV4YW1wbGUuY29t"; // "api.example.com"
        const string encPath = "L2FwaS92MS91c2Vycw=="; // "/api/v1/users"
        
        var scheme = System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(encScheme));
        var host = System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(encHost));
        var path = System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(encPath));
        
        return $"{scheme}{host}{path}";
    }
    
    // ============================================================
    // PATTERN 5: Configuration Key Protection
    // ============================================================
    
    public void AccessConfiguration()
    {
        // Instead of: var url = config.GetValue<string>("ApiSettings:BaseUrl");
        // Use encrypted key:
        
        var config = ECoopSystem.Configuration.ConfigurationLoader.Current;
        var key = ConfigKey_ApiUrl; // Decrypted at access time
        
        // Key is never visible in compiled code
    }
    
    // ============================================================
    // PATTERN 6: Header and Token Protection
    // ============================================================
    
    private const string Enc_AuthHeaderName = "QXV0aG9yaXphdGlvbg=="; // "Authorization"
    private const string Enc_BearerPrefix = "QmVhcmVyIA=="; // "Bearer "
    private const string Enc_ContentType = "Q29udGVudC1UeXBl"; // "Content-Type"
    private const string Enc_JsonMediaType = "YXBwbGljYXRpb24vanNvbg=="; // "application/json"
    
    public HttpRequestMessage CreateSecureRequest(string token)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, ApiEndpoint);
        
        // Add headers using encrypted names
        var authHeader = System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(Enc_AuthHeaderName));
        var bearerPrefix = System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(Enc_BearerPrefix));
        var contentType = System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(Enc_ContentType));
        var jsonMediaType = System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(Enc_JsonMediaType));
        
        request.Headers.Add(authHeader, $"{bearerPrefix}{token}");
        request.Headers.Add(contentType, jsonMediaType);
        
        return request;
    }
    
    // ============================================================
    // PATTERN 7: Error Message Protection
    // ============================================================
    
    private const string Enc_ErrorMsg_InvalidKey = "SW52YWxpZCBsaWNlbnNlIGtleQ=="; // "Invalid license key"
    private const string Enc_ErrorMsg_Expired = "TGljZW5zZSBleHBpcmVk"; // "License expired"
    private const string Enc_ErrorMsg_ServerError = "U2VydmVyIGVycm9y"; // "Server error"
    
    public string GetErrorMessage(int errorCode)
    {
        return errorCode switch
        {
            400 => System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(Enc_ErrorMsg_InvalidKey)),
            401 => System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(Enc_ErrorMsg_Expired)),
            500 => System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(Enc_ErrorMsg_ServerError)),
            _ => "Unknown error"
        };
    }
    
    // ============================================================
    // ANTI-PATTERN: What NOT to do
    // ============================================================
    
    // ? DON'T: Leave sensitive data in plain text
    // private const string ApiKey = "sk_live_1234567890abcdef";
    
    // ? DON'T: Use weak encoding
    // var encoded = Convert.ToBase64String(Encoding.UTF8.GetBytes(password)); // Still readable
    
    // ? DON'T: Encrypt non-sensitive data
    // const string Enc_ButtonText = "..."; // User-visible text doesn't need encryption
    
    // ? DON'T: Forget to test
    // Always test encrypted builds before deployment
    
    // ============================================================
    // BEST PRACTICES
    // ============================================================
    
    // ? DO: Cache decrypted strings if used frequently
    private static readonly Lazy<string> CachedApiUrl = new(() => 
        System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(Enc_ConfigKey_ApiUrl))
    );
    
    // ? DO: Use meaningful constant names
    // private const string Enc_DatabaseConnectionString = "...";
    
    // ? DO: Document what each encrypted string represents
    // private const string Enc_ApiKey = "..."; // Production API key
    
    // ? DO: Separate sensitive and non-sensitive strings
    // Encrypt: API keys, URLs, credentials, secrets
    // Don't encrypt: UI text, error messages shown to users, debug strings
    
    // ? DO: Use consistent naming convention
    // Enc_* for encrypted constants
    // Decrypted property without Enc_ prefix
}
