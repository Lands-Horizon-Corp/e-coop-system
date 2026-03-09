using System;
using System;
using System.Diagnostics;
using System.IO;
using System.Text.Json;
using Microsoft.AspNetCore.DataProtection;

namespace ECoopSystem.Stores;

public class AppStateStore
{
    private static readonly string FolderName = System.Text.Encoding.UTF8.GetString(Convert.FromBase64String("RUNvb3BTeXN0ZW0="));
    private static readonly string FileName = System.Text.Encoding.UTF8.GetString(Convert.FromBase64String("YXBwc3RhdGUuZGF0"));
    private static readonly string Purpose = System.Text.Encoding.UTF8.GetString(Convert.FromBase64String("RUNvb3BTeXN0ZW0uQXBwU3RhdGUudjE="));
    
    private readonly string _filePath;
    private readonly ISecureStorage _secureStorage;
    
    public AppStateStore(IDataProtectionProvider provider)
    {
        var dir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            FolderName);

        Directory.CreateDirectory(dir);
        _filePath = Path.Combine(dir, FileName);

        _secureStorage = SecureStorageFactory.Create(provider, Purpose);
    }

    public AppState Load()
    {
        try
        {
            if (OperatingSystem.IsLinux())
                Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] AppStateStore: Load() called");

            if (!File.Exists(_filePath))
            {
                if (OperatingSystem.IsLinux())
                    Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] AppStateStore: No file exists, creating initial");

                return CreateInitial();
            }

            if (OperatingSystem.IsLinux())
                Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] AppStateStore: Reading file: {_filePath}");

            try
            {
                var fileData = File.ReadAllText(_filePath);
                
                if (OperatingSystem.IsLinux())
                    Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] AppStateStore: File read, length: {fileData.Length}");

                if (string.IsNullOrWhiteSpace(fileData))
                {
                    Debug.WriteLine("AppState: Empty file detected, creating new state");
                    if (OperatingSystem.IsLinux())
                        Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] AppStateStore: Empty file, creating initial");

                    return CreateInitial();
                }

                string json;

                if (OperatingSystem.IsLinux())
                    Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] AppStateStore: Calling Unprotect...");

                try
                {
                    json = _secureStorage.Unprotect(fileData);
                    
                    if (OperatingSystem.IsLinux())
                        Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] AppStateStore: Unprotect succeeded, JSON length: {json?.Length ?? 0}");
                }
                catch (Exception ex)
                {
                    if (OperatingSystem.IsLinux())
                    {
                        Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] AppStateStore: Unprotect FAILED: {ex.Message}");
                        Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] AppStateStore: Exception type: {ex.GetType().Name}");
                        Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] AppStateStore: Stack: {ex.StackTrace}");
                    }

                    Debug.WriteLine($"AppState: Failed to decrypt data: {ex.Message}");
                    File.Delete(_filePath);
                    return CreateInitial();
                }

                if (string.IsNullOrWhiteSpace(json))
                {
                    Debug.WriteLine("AppState: Empty JSON detected, creating new state");
                    if (OperatingSystem.IsLinux())
                        Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] AppStateStore: Empty JSON, creating initial");

                    return CreateInitial();
                }

                if (OperatingSystem.IsLinux())
                    Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] AppStateStore: Deserializing JSON...");

                var state = JsonSerializer.Deserialize<AppState>(json);

                if (state == null || string.IsNullOrWhiteSpace(state.InstallationId) || state.InstallationUnixTime <= 0)
                {
                    Debug.WriteLine("AppState: Invalid state data detected, creating new state");
                    if (OperatingSystem.IsLinux())
                        Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] AppStateStore: Invalid state, creating initial");

                    return CreateInitial();
                }

                if (OperatingSystem.IsLinux())
                    Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] AppStateStore: Load succeeded");

                return state;
            }
            catch (Exception ex)
            {
                if (OperatingSystem.IsLinux())
                {
                    Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] AppStateStore: Load inner exception: {ex.Message}");
                    Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] AppStateStore: Exception type: {ex.GetType().Name}");
                }

                Debug.WriteLine($"AppState: Failed to load (possibly tampered): {ex.Message}");
                try
                {
                    File.Delete(_filePath);
                }
                catch
                {
                    // Ignore
                }
                return CreateInitial();
            }
        }
        catch (Exception ex)
        {
            if (OperatingSystem.IsLinux())
            {
                Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] AppStateStore: Load outer exception: {ex.Message}");
                Console.WriteLine(ex.ToString());
            }
            throw;
        }
    }

    public void Save(AppState state)
    {
        try
        {
            if (state == null) throw new ArgumentNullException(nameof(state));
            if (string.IsNullOrWhiteSpace(state.InstallationId)) throw new InvalidOperationException("InstallationId cannot be empty");

            var json = JsonSerializer.Serialize(state, new JsonSerializerOptions { WriteIndented = false });
            var protectedData = _secureStorage.Protect(json);
            File.WriteAllText(_filePath, protectedData);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"AppState: Failed to save: {ex.Message}");
            throw;
        }
    }

    private AppState CreateInitial()
    {
        var newState = new AppState
        {
            InstallationId = Guid.NewGuid().ToString("N"),
            InstallationUnixTime = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
        };

        Save(newState);

        return newState;
    }
}
