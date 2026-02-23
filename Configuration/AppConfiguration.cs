namespace ECoopSystem.Configuration;

/// <summary>
/// Application-wide settings (User-configurable via appsettings.json)
/// NOTE: Security-critical settings (API URLs, security parameters) are in BuildConfiguration
/// and compiled into the binary at build time - they cannot be modified by end users.
/// </summary>
public class ApplicationSettings
{
    /// <summary>
    /// Application display name
    /// </summary>
    public string Name { get; set; } = "ECoopSystem";

    /// <summary>
    /// Application version
    /// </summary>
    public string Version { get; set; } = "1.0.0";

    /// <summary>
    /// Minimum loading time in seconds (to hide UI transitions)
    /// </summary>
    public int MinimumLoadingTimeSeconds { get; set; } = 5;

    /// <summary>
    /// Default window width in pixels
    /// </summary>
    public int WindowWidth { get; set; } = 1280;

    /// <summary>
    /// Default window height in pixels
    /// </summary>
    public int WindowHeight { get; set; } = 720;
}

/// <summary>
/// Logging configuration settings (User-configurable)
/// </summary>
public class LoggingSettings
{
    /// <summary>
    /// Whether to enable debug logging
    /// </summary>
    public bool EnableDebugLogging { get; set; } = false;

    /// <summary>
    /// Minimum log level (Debug, Info, Warning, Error)
    /// </summary>
    public string LogLevel { get; set; } = "Warning";
}

/// <summary>
/// Root configuration class containing user-configurable settings only
/// Sensitive settings (API, WebView, Security) are now in BuildConfiguration
/// </summary>
public class AppConfiguration
{
    public ApplicationSettings Application { get; set; } = new();
    public LoggingSettings Logging { get; set; } = new();
}
