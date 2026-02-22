using System;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using Avalonia;
using ECoopSystem.Build;
using ECoopSystem.Services;
using ECoopSystem.Stores;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace ECoopSystem
{
    internal class Program
    {
#if WINDOWS
        [STAThread]
#endif
        public static void Main(string[] args) => BuildAvaloniaApp()
            .StartWithClassicDesktopLifetime(args);

        public static AppBuilder BuildAvaloniaApp()
        {
            var configuration = new ConfigurationBuilder()
                .SetBasePath(AppContext.BaseDirectory)
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: false)
#if DEBUG
                .AddJsonFile("appsettings.Development.json", optional: true, reloadOnChange: false)
#endif
                .Build();

            var services = new ServiceCollection();
            services.AddSingleton<IConfiguration>(configuration);
            
            services.AddLogging(builder =>
            {
#if DEBUG
                builder.AddDebug();
                builder.AddConsole();
                builder.SetMinimumLevel(LogLevel.Debug);
#else
                builder.SetMinimumLevel(LogLevel.Information);
#endif
            });
            
            var keysDir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "ECoopSystem",
                "dp-keys"
            );

            Directory.CreateDirectory(keysDir);

            services.AddDataProtection()
                    .PersistKeysToFileSystem(new DirectoryInfo(keysDir))
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
                
#if !DEBUG
                handler.ServerCertificateCustomValidationCallback = (message, cert, chain, sslPolicyErrors) =>
                {
                    if (sslPolicyErrors == System.Net.Security.SslPolicyErrors.None)
                        return true;
                    
                    Debug.WriteLine($"SSL Certificate Error: {sslPolicyErrors}");
                    if (cert != null)
                    {
                        Debug.WriteLine($"Certificate Subject: {cert.Subject}");
                        Debug.WriteLine($"Certificate Issuer: {cert.Issuer}");
                    }
                    
                    return false;
                };
#else
                handler.ServerCertificateCustomValidationCallback = (message, cert, chain, sslPolicyErrors) =>
                {
                    if (sslPolicyErrors != System.Net.Security.SslPolicyErrors.None)
                    {
                        Debug.WriteLine($"[DEV] SSL Warning: {sslPolicyErrors}");
                    }
                    return true;
                };
#endif
                
                return handler;
            });

            var provider = services.BuildServiceProvider();

            return AppBuilder.Configure<App>()
                    .UsePlatformDetect()
                    .WithInterFont()
                    .LogToTrace()
                    .AfterSetup(_ =>
                    {
                        App.Services = provider;
                    });
        }
    }
}
