using System;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
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
        private static System.Threading.Mutex? _mutex;
        private const string MutexName = "Global\\ECoopSystem-8F5A3D2C-1B4E-4C9A-A8F3-2D6E8C9B1A7F";

#if WINDOWS
        [STAThread]
#endif
        public static void Main(string[] args)
        {
            // Check for single instance
            bool createdNew;
            _mutex = new System.Threading.Mutex(true, MutexName, out createdNew);

            if (!createdNew)
            {
                // Another instance is already running
                Debug.WriteLine("Another instance of ECoopSystem is already running.");
                
                // Log to file if possible
                try
                {
                    var logDir = Path.Combine(
                        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                        "ECoopSystem",
                        "logs"
                    );
                    Directory.CreateDirectory(logDir);
                    var logFile = Path.Combine(logDir, $"single-instance-{DateTime.Now:yyyyMMdd}.log");
                    File.AppendAllText(logFile, $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] Another instance already running, exiting.\n");
                }
                catch { /* Ignore logging errors */ }
                
                return;
            }

            try
            {
                // Set up global exception handlers
                AppDomain.CurrentDomain.UnhandledException += (sender, e) =>
                {
                    var ex = e.ExceptionObject as Exception;
                    Debug.WriteLine($"[FATAL] Unhandled exception: {ex}");
                    
                    // Try to log to file if possible
                    try
                    {
                        var logDir = Path.Combine(
                            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                            "ECoopSystem",
                            "logs"
                        );
                        Directory.CreateDirectory(logDir);
                        var crashLog = Path.Combine(logDir, $"crash-{DateTime.Now:yyyyMMdd-HHmmss}.log");
                        File.WriteAllText(crashLog, $"Unhandled Exception:\n{ex}");
                    }
                    catch { /* Ignore logging errors */ }
                };

                TaskScheduler.UnobservedTaskException += (sender, e) =>
                {
                    Debug.WriteLine($"[ERROR] Unobserved task exception: {e.Exception}");
                    e.SetObserved(); // Prevent process termination
                };

                BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[FATAL] Application crashed: {ex}");
                
                // Try to show message box or log
                try
                {
                    var logDir = Path.Combine(
                        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                        "ECoopSystem",
                        "logs"
                    );
                    Directory.CreateDirectory(logDir);
                    var crashLog = Path.Combine(logDir, $"crash-{DateTime.Now:yyyyMMdd-HHmmss}.log");
                    File.WriteAllText(crashLog, $"Fatal Exception in Main:\n{ex}");
                }
                catch { /* Ignore logging errors */ }
                
                
                throw;
            }
            finally
            {
                // Release mutex on exit
                _mutex?.ReleaseMutex();
                _mutex?.Dispose();
            }
        }

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
                
                // Add file logging in Release builds for diagnostics
                var logDir = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                    "ECoopSystem",
                    "logs"
                );
                Directory.CreateDirectory(logDir);
                
                builder.AddSimpleConsole(options =>
                {
                    options.SingleLine = true;
                    options.TimestampFormat = "yyyy-MM-dd HH:mm:ss ";
                });
                
                // Add custom file logger
                builder.Services.AddSingleton<ILoggerProvider, FileLoggerProvider>(sp => 
                    new FileLoggerProvider(Path.Combine(logDir, $"ecoopsystem-{DateTime.Now:yyyyMMdd}.log")));
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
