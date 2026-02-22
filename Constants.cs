using ECoopSystem.Build;
using ECoopSystem.Configuration;

namespace ECoopSystem;

/// <summary>
/// Application constants.
/// - User-configurable values come from appsettings.json via ConfigurationLoader
/// - Security-critical values are compiled into BuildConfiguration at build time
/// </summary>
public static class Constants
{
    // License key length is fixed and cannot be configured
    public const int LicenseKeyLength = 127;

    // Window dimensions - user-configurable from appsettings.json
    public static int WindowWidth => ConfigurationLoader.Current.Application.WindowWidth;
    public static int WindowHeight => ConfigurationLoader.Current.Application.WindowHeight;

    // Security settings - compiled into binary at build time (not user-modifiable)
    public static int GracePeriodDays => BuildConfiguration.SecurityGracePeriodDays;
    public static int MaxActivationAttempts => BuildConfiguration.SecurityMaxActivationAttempts;
    public static int LockoutMinutes => BuildConfiguration.SecurityLockoutMinutes;
    public static int ActivationLookbackMinutes => BuildConfiguration.SecurityActivationLookbackMinutes;
    public static int BackgroundVerificationIntervalMinutes => BuildConfiguration.SecurityBackgroundVerificationIntervalMinutes;

    // Application settings - user-configurable from appsettings.json
    public static int MinimumLoadingTimeSeconds => ConfigurationLoader.Current.Application.MinimumLoadingTimeSeconds;
}
