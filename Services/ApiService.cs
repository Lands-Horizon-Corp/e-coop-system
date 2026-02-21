using ECoopSystem.Build;
using ECoopSystem.Configuration;

namespace ECoopSystem.Services;

public class ApiService
{
    public static string BaseUrl
    {
        get
        {
#if DEBUG
            // Development: Use configuration (appsettings.Development.json)
            return ConfigurationLoader.Current.ApiSettings.BaseUrl;
#else
            // Production: Use build-time configured URL if available, otherwise use configuration
            var buildUrl = BuildConfiguration.ApiUrl;
            if (!string.IsNullOrEmpty(buildUrl) && 
                !buildUrl.Contains("$(") && // Not a placeholder
                buildUrl != "https://e-coop-server-development.up.railway.app/")
            {
                return buildUrl;
            }
            return ConfigurationLoader.Current.ApiSettings.BaseUrl;
#endif
        }
    }
}
