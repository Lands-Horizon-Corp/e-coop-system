using ECoopSystem.Configuration;

namespace ECoopSystem;

/// <summary>
/// Application constants. Most values now come from appsettings.json via ConfigurationLoader.
/// </summary>
public static class Constants
{
    // License key length is fixed and cannot be configured
    public const int LicenseKeyLength = 127;

    // Window dimensions - uses configuration if available, otherwise defaults
    public static int WindowWidth => ConfigurationLoader.Current.Application.WindowWidth;
    public static int WindowHeight => ConfigurationLoader.Current.Application.WindowHeight;

    // Security settings - from configuration
    public static int GracePeriodDays => ConfigurationLoader.Current.Security.GracePeriodDays;
    public static int MaxActivationAttempts => ConfigurationLoader.Current.Security.MaxActivationAttempts;
    public static int LockoutMinutes => ConfigurationLoader.Current.Security.LockoutMinutes;
    public static int ActivationLookbackMinutes => ConfigurationLoader.Current.Security.ActivationLookbackMinutes;

    // Application settings - from configuration
    public static int MinimumLoadingTimeSeconds => ConfigurationLoader.Current.Application.MinimumLoadingTimeSeconds;
}
