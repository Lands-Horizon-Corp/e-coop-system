namespace ECoopSystem.Services;

public class ApiService
{
    public static string BaseUrl
    {
        get
        {
#if DEBUG
            return "https://e-coop-server-development.up.railway.app/";
#else
            return "https://e-coop-server-production.up.railway.app/"; // TODO: Replace with actual production URL
#endif
        }
    }
}
