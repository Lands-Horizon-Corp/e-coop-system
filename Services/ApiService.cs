using ECoopSystem.Build;

namespace ECoopSystem.Services;

/// <summary>
/// API service using build-time compiled configuration (not user-modifiable)
/// </summary>
public class ApiService
{
    public static string BaseUrl => BuildConfiguration.ApiUrl;
    public static int Timeout => BuildConfiguration.ApiTimeout;
    public static int MaxRetries => BuildConfiguration.ApiMaxRetries;
    public static int MaxResponseSizeBytes => BuildConfiguration.ApiMaxResponseSizeBytes;
}
