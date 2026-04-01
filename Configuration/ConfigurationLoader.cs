using System;
using System.IO;
using System.Text.Json;

namespace ECoopSystem.Configuration;

/// <summary>
/// Loads and manages user-configurable application settings from appsettings.json
/// Sensitive settings (API, WebView, Security) are compiled into BuildConfiguration
/// </summary>
public static class ConfigurationLoader
{
    private static AppConfiguration? _configuration;
    private static readonly object _lock = new();

    /// <summary>
    /// Gets the current application configuration (singleton)
    /// </summary>
    public static AppConfiguration Current
    {
        get
        {
            if (_configuration == null)
            {
                lock (_lock)
                {
                    _configuration ??= Load();
                }
            }
            return _configuration;
        }
    }

    /// <summary>
    /// Loads user-configurable settings from appsettings.json
    /// </summary>
    public static AppConfiguration Load()
    {
        var config = new AppConfiguration();

        try
        {
            // Load base configuration
            var baseConfigPath = GetConfigPath("appsettings.json");
            if (File.Exists(baseConfigPath))
            {
                var baseJson = File.ReadAllText(baseConfigPath);
                var baseConfig = JsonSerializer.Deserialize<AppConfiguration>(baseJson, GetJsonOptions());
                if (baseConfig != null)
                {
                    config = baseConfig;
                }
            }

            // Load environment-specific overrides
#if DEBUG
            var envConfigPath = GetConfigPath("appsettings.Development.json");
#else
            var envConfigPath = GetConfigPath("appsettings.Production.json");
#endif

            if (File.Exists(envConfigPath))
            {
                var envJson = File.ReadAllText(envConfigPath);
                var envConfig = JsonSerializer.Deserialize<AppConfiguration>(envJson, GetJsonOptions());
                if (envConfig != null)
                {
                    MergeConfiguration(config, envConfig);
                }
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Configuration: Failed to load settings: {ex.Message}");
            // Return default configuration on error
        }

        return config;
    }

    /// <summary>
    /// Reloads the configuration from disk
    /// </summary>
    public static void Reload()
    {
        lock (_lock)
        {
            _configuration = Load();
        }
    }

    private static string GetConfigPath(string filename)
    {
        // Try current directory first
        var currentDirPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, filename);
        if (File.Exists(currentDirPath))
            return currentDirPath;

        // Try app data directory
        var appDataPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "ECoopSystem",
            filename
        );
        if (File.Exists(appDataPath))
            return appDataPath;

        // Return current directory path as default
        return currentDirPath;
    }

    private static JsonSerializerOptions GetJsonOptions()
    {
        return new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            ReadCommentHandling = JsonCommentHandling.Skip,
            AllowTrailingCommas = true
        };
    }

    private static void MergeConfiguration(AppConfiguration target, AppConfiguration source)
    {
        // Merge Application settings
        if (!string.IsNullOrEmpty(source.Application.Name))
            target.Application.Name = source.Application.Name;
        if (!string.IsNullOrEmpty(source.Application.Version))
            target.Application.Version = source.Application.Version;
        if (source.Application.MinimumLoadingTimeSeconds > 0)
            target.Application.MinimumLoadingTimeSeconds = source.Application.MinimumLoadingTimeSeconds;
        if (source.Application.WindowWidth > 0)
            target.Application.WindowWidth = source.Application.WindowWidth;
        if (source.Application.WindowHeight > 0)
            target.Application.WindowHeight = source.Application.WindowHeight;

        // Merge Logging settings
        target.Logging.EnableDebugLogging = source.Logging.EnableDebugLogging;
        if (!string.IsNullOrEmpty(source.Logging.LogLevel))
            target.Logging.LogLevel = source.Logging.LogLevel;
    }
}
