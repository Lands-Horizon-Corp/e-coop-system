using System;
using System.Diagnostics;
using System.IO;
using System.Text.Json;

namespace ECoopSystem.Stores;

public class AppStateStore
{
    private readonly string _filePath;
    
    public AppStateStore()
    {
        var dir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "ECoopSystem");

        Directory.CreateDirectory(dir);
        _filePath = Path.Combine(dir, "appstate.json");
    }

    public AppState Load()
    {
        if (!File.Exists(_filePath))
            return CreateInitial();

        var json = File.ReadAllText(_filePath);
        return JsonSerializer.Deserialize<AppState>(json) ?? CreateInitial();
    }

    public void Save(AppState state)
    {
        var json = JsonSerializer.Serialize(state, new JsonSerializerOptions
        {
            WriteIndented = true
        });
        File.WriteAllText(_filePath, json);
    }

    private AppState CreateInitial()
    {
        return new AppState
        {
            InstallationId = Guid.NewGuid().ToString("N"),
            InsallationUnixTime = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
        };
    }

}
