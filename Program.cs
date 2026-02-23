using System;
using System.Net.Http;
using System.Threading.Tasks;
using Avalonia;
using ECoopSystem.Build;
using ECoopSystem.Services;
using ECoopSystem.Stores;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace ECoopSystem
{
    internal class Program
    {
        private static System.Threading.Mutex? _mutex;
        private const string MutexName = "Global\\ECoopSystem-8F5A3D2C-1B4E-4C9A-A8F3-2D6E8C9B1A7F";

#if WINDOWS
        [STAThread]
#endif
        public static void Main(string[] args)
        {
            bool createdNew;
            _mutex = new System.Threading.Mutex(true, MutexName, out createdNew);

            if (!createdNew)
            {
                return;
            }

            try
            {
                AppDomain.CurrentDomain.UnhandledException += (sender, e) =>
                {
                    // Silently handle to prevent information disclosure
                };

                TaskScheduler.UnobservedTaskException += (sender, e) =>
                {
                    e.SetObserved();
                };

                BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
            }
            finally
            {
                _mutex?.ReleaseMutex();
                _mutex?.Dispose();
            }
        }

        public static AppBuilder BuildAvaloniaApp()
        {
            var configuration = new ConfigurationBuilder()
                .SetBasePath(AppContext.BaseDirectory)
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: false)
                .Build();

            var services = new ServiceCollection();
            services.AddSingleton<IConfiguration>(configuration);
            
            var keysDir = System.IO.Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "ECoopSystem",
                "dp-keys"
            );

            System.IO.Directory.CreateDirectory(keysDir);

            services.AddDataProtection()
                    .PersistKeysToFileSystem(new System.IO.DirectoryInfo(keysDir))
                    .SetApplicationName("ECoopSystem");

            services.AddSingleton<AppStateStore>();
            services.AddSingleton<SecretKeyStore>();
            
            services.AddHttpClient<LicenseService>(client =>
            {
                client.Timeout = TimeSpan.FromSeconds(BuildConfiguration.ApiTimeout);
            })
            .ConfigurePrimaryHttpMessageHandler(() =>
            {
                var handler = new HttpClientHandler();
                handler.ServerCertificateCustomValidationCallback = (message, cert, chain, sslPolicyErrors) =>
                {
                    return sslPolicyErrors == System.Net.Security.SslPolicyErrors.None;
                };
                return handler;
            });

            var provider = services.BuildServiceProvider();

            return AppBuilder.Configure<App>()
                    .UsePlatformDetect()
                    .WithInterFont()
                    .AfterSetup(_ =>
                    {
                        App.Services = provider;
                    });
        }
    }
}
