using System;
using System.Collections.Generic;

namespace ECoopSystem.Utilities;

/// <summary>
/// Contains encrypted sensitive strings used throughout the application.
/// All strings are encrypted at build time and decrypted at runtime.
/// </summary>
public static class EncryptedStrings
{
    // API Endpoints (encrypted)
    private const string Enc_ApiActivateEndpoint = "LKJ3kNmV8QxYzP2rT5vW9cBnHgFd7sA1"; // Placeholder - replace with actual
    private const string Enc_ApiVerifyEndpoint = "MNB4lPqX9RyZaQ3sU6wX0dCoIhGe8tB2"; // Placeholder - replace with actual
    
    // Default URLs (encrypted)
    private const string Enc_DefaultApiUrl = "QRS5mRtY0SzAbR4tV7xY1eDpJiHf9uC3"; // Placeholder - replace with actual
    private const string Enc_DefaultClientUrl = "TUV6nSwZ1TaAcS5uW8yZ2fEqKjIg0vD4"; // Placeholder - replace with actual
    private const string Enc_DevelopmentApiUrl = "WXY7oUxA2UbBdT6vX9zA3gFrLkJh1wE5"; // Placeholder - replace with actual
    private const string Enc_DevelopmentClientUrl = "ZAB8pVyB3VcCeU7wY0zB4hGsMLKi2xF6"; // Placeholder - replace with actual
    
    // Configuration Keys (encrypted)
    private const string Enc_ConfigKey_ApiSettings = "CDE9qWzC4WdDfV8xZ1aC5iHtNmLj3yG7"; // Placeholder
    private const string Enc_ConfigKey_BaseUrl = "FGH0rXaD5XeEgW9yA2bD6jIuOnMk4zH8"; // Placeholder
    private const string Enc_ConfigKey_WebViewSettings = "IJK1sYbE6YfFhX0zB3cE7kJvPoNl5aI9"; // Placeholder
    
    // Data Protection Purposes (encrypted)
    private const string Enc_DataProtection_SecretKey = "LMN2tZcF7ZgGiY1aC4dF8lKwQpOm6bJ0"; // Placeholder
    private const string Enc_DataProtection_AppState = "OPQ3uAdG8AhHjZ2bD5eG9mLxRqPn7cK1"; // Placeholder
    
    // File Paths (encrypted)
    private const string Enc_SecretKeyFilename = "RST4vBeH9BiIkA3cE6fH0nMyStQo8dL2"; // Placeholder
    private const string Enc_AppStateFilename = "UVW5wCfI0CjJlB4dF7gI1oNzTuRp9eM3"; // Placeholder
    private const string Enc_AppDataFolderName = "XYZ6xDgJ1DkKmC5eG8hJ2pOaUvSq0fN4"; // Placeholder
    
    // Decrypted property accessors
    public static string ApiActivateEndpoint => Enc_ApiActivateEndpoint.DecryptString();
    public static string ApiVerifyEndpoint => Enc_ApiVerifyEndpoint.DecryptString();
    public static string DefaultApiUrl => Enc_DefaultApiUrl.DecryptString();
    public static string DefaultClientUrl => Enc_DefaultClientUrl.DecryptString();
    public static string DevelopmentApiUrl => Enc_DevelopmentApiUrl.DecryptString();
    public static string DevelopmentClientUrl => Enc_DevelopmentClientUrl.DecryptString();
    
    public static string ConfigKey_ApiSettings => Enc_ConfigKey_ApiSettings.DecryptString();
    public static string ConfigKey_BaseUrl => Enc_ConfigKey_BaseUrl.DecryptString();
    public static string ConfigKey_WebViewSettings => Enc_ConfigKey_WebViewSettings.DecryptString();
    
    public static string DataProtection_SecretKey => Enc_DataProtection_SecretKey.DecryptString();
    public static string DataProtection_AppState => Enc_DataProtection_AppState.DecryptString();
    
    public static string SecretKeyFilename => Enc_SecretKeyFilename.DecryptString();
    public static string AppStateFilename => Enc_AppStateFilename.DecryptString();
    public static string AppDataFolderName => Enc_AppDataFolderName.DecryptString();
}
