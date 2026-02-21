using System.Collections.Generic;

namespace ECoopSystem.Configuration;

/// <summary>
/// API-related configuration settings
/// </summary>
public class ApiSettings
{
    /// <summary>
    /// Base URL for the API server
    /// </summary>
    public string BaseUrl { get; set; } = "https://e-coop-server-development.up.railway.app/";

    /// <summary>
    /// Request timeout in seconds
    /// </summary>
    public int Timeout { get; set; } = 12;

    /// <summary>
    /// Maximum number of retry attempts for failed requests
    /// </summary>
    public int MaxRetries { get; set; } = 3;

    /// <summary>
    /// Maximum response size in bytes (default: 1MB)
    /// </summary>
    public int MaxResponseSizeBytes { get; set; } = 1024 * 1024;
}

/// <summary>
/// WebView-related configuration settings
/// </summary>
public class WebViewSettings
{
    /// <summary>
    /// Base URL for the WebView client application
    /// </summary>
    public string BaseUrl { get; set; } = "https://e-coop-client-development.up.railway.app/";

    /// <summary>
    /// List of trusted domains that the WebView can navigate to
    /// </summary>
    public List<string> TrustedDomains { get; set; } = new()
    {
        "e-coop-client-development.up.railway.app",
        "example.com",
        "app.example.com",
        "api.example.com"
    };

    /// <summary>
    /// Whether to allow HTTP connections (should be false in production)
    /// </summary>
    public bool AllowHttp { get; set; } = false;
}

/// <summary>
/// Security-related configuration settings
/// </summary>
public class SecuritySettings
{
    /// <summary>
    /// Number of days before license verification is required
    /// </summary>
    public int GracePeriodDays { get; set; } = 7;

    /// <summary>
    /// Maximum number of failed activation attempts before lockout
    /// </summary>
    public int MaxActivationAttempts { get; set; } = 3;

    /// <summary>
    /// Duration of lockout after too many failed attempts (in minutes)
    /// </summary>
    public int LockoutMinutes { get; set; } = 5;

    /// <summary>
    /// Time window for counting failed activation attempts (in minutes)
    /// </summary>
    public int ActivationLookbackMinutes { get; set; } = 1;

    /// <summary>
    /// Interval for background license re-verification (in minutes)
    /// </summary>
    public int BackgroundVerificationIntervalMinutes { get; set; } = 60;
}

/// <summary>
/// Application-wide settings
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
/// Logging configuration settings
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
/// Root configuration class containing all application settings
/// </summary>
public class AppConfiguration
{
    public ApiSettings ApiSettings { get; set; } = new();
    public WebViewSettings WebViewSettings { get; set; } = new();
    public SecuritySettings Security { get; set; } = new();
    public ApplicationSettings Application { get; set; } = new();
    public LoggingSettings Logging { get; set; } = new();
}
