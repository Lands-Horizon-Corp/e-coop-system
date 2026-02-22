using System;
using System.Diagnostics;
using System.IO;
using System.Text.Json;
using Microsoft.AspNetCore.DataProtection;

namespace ECoopSystem.Stores;

public class AppStateStore
{
    private readonly string _filePath;
    private readonly IDataProtector _protector;
    
    public AppStateStore(IDataProtectionProvider provider)
    {
        // Encrypted folder name and file name
        const string encFolderName = "RUNvb3BTeXN0ZW0="; // "ECoopSystem"
        const string encFileName = "YXBwc3RhdGUuZGF0"; // "appstate.dat"
        const string encPurpose = "RUNvb3BTeXN0ZW0uQXBwU3RhdGUudjE="; // "ECoopSystem.AppState.v1"
        
        var folderName = System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(encFolderName));
        var fileName = System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(encFileName));
        var purpose = System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(encPurpose));
        
        var dir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            folderName);

        Directory.CreateDirectory(dir);
        _filePath = Path.Combine(dir, fileName);

        _protector = provider.CreateProtector(purpose);
    }

    public AppState Load()
    {
        if (!File.Exists(_filePath))
            return CreateInitial();

        try
        {
            var protectedData = File.ReadAllText(_filePath);
            var json = _protector.Unprotect(protectedData);
            var state = JsonSerializer.Deserialize<AppState>(json);
            
            if (state == null)
            {
                Debug.WriteLine("AppState: Deserialization returned null, creating new state");
                return CreateInitial();
            }

            if (string.IsNullOrWhiteSpace(state.InstallationId) || 
                state.InstallationUnixTime <= 0)
            {
                Debug.WriteLine("AppState: Invalid state data detected, creating new state");
                return CreateInitial();
            }

            return state;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"AppState: Failed to load (possibly tampered): {ex.Message}");
            return CreateInitial();
        }
    }

    public void Save(AppState state)
    {
        try
        {
            if (state == null)
                throw new ArgumentNullException(nameof(state));

            if (string.IsNullOrWhiteSpace(state.InstallationId))
                throw new InvalidOperationException("InstallationId cannot be empty");

            var json = JsonSerializer.Serialize(state, new JsonSerializerOptions
            {
                WriteIndented = false
            });

            var protectedData = _protector.Protect(json);
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
