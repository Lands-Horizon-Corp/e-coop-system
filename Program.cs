using System;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using Avalonia;
using ECoopSystem.Services;
using ECoopSystem.Stores;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http;

namespace ECoopSystem
{
    internal class Program
    {
        // Initialization code. Don't use any Avalonia, third-party APIs or any
        // SynchronizationContext-reliant code before AppMain is called: things aren't initialized
        // yet and stuff might break.
        [STAThread]
        public static void Main(string[] args) => BuildAvaloniaApp()
            .StartWithClassicDesktopLifetime(args);

        // Avalonia configuration, don't remove; also used by visual designer.
        public static AppBuilder BuildAvaloniaApp()
        {
            var services = new ServiceCollection();
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
            
            // Configure HttpClient for LicenseService with SSL/TLS validation
            services.AddHttpClient<LicenseService>(client =>
            {
                client.Timeout = TimeSpan.FromSeconds(12);
            })
            .ConfigurePrimaryHttpMessageHandler(() =>
            {
                var handler = new HttpClientHandler();
                
#if !DEBUG
                // Production: Enable strict SSL certificate validation
                handler.ServerCertificateCustomValidationCallback = (message, cert, chain, sslPolicyErrors) =>
                {
                    // Only accept valid certificates in production
                    if (sslPolicyErrors == System.Net.Security.SslPolicyErrors.None)
                        return true;
                    
                    // Log certificate validation errors
                    Debug.WriteLine($"SSL Certificate Error: {sslPolicyErrors}");
                    if (cert != null)
                    {
                        Debug.WriteLine($"Certificate Subject: {cert.Subject}");
                        Debug.WriteLine($"Certificate Issuer: {cert.Issuer}");
                    }
                    
                    return false;
                };
#else
                // Development: Allow self-signed certificates but log warnings
                handler.ServerCertificateCustomValidationCallback = (message, cert, chain, sslPolicyErrors) =>
                {
                    if (sslPolicyErrors != System.Net.Security.SslPolicyErrors.None)
                    {
                        Debug.WriteLine($"[DEV] SSL Warning: {sslPolicyErrors}");
                    }
                    return true; // Accept in development
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
