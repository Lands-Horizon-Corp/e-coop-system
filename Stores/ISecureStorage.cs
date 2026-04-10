namespace ECoopSystem.Stores;

/// <summary>
/// Platform-agnostic interface for secure storage operations
/// </summary>
public interface ISecureStorage
{
    /// <summary>
    /// Encrypts and stores data securely
    /// </summary>
    string Protect(string plainText);
    
    /// <summary>
    /// Decrypts and retrieves data
    /// </summary>
    string Unprotect(string protectedText);
}
