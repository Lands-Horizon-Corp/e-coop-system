using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Media;
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

        private static void CleanCefCache()
        {
            try
            {
                // On Linux, aggressively kill any orphaned CEF processes first
                if (OperatingSystem.IsLinux())
                {
                    try
                    {
                        Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] Killing orphaned CEF processes...");
                        
                        // Kill any CefGlue browser processes
                        var killCefGlue = new System.Diagnostics.ProcessStartInfo
                        {
                            FileName = "pkill",
                            Arguments = "-9 -f \"CefGlue|libcef\"",
                            RedirectStandardOutput = true,
                            RedirectStandardError = true,
                            UseShellExecute = false,
                            CreateNoWindow = true
                        };
                        
                        using (var process = System.Diagnostics.Process.Start(killCefGlue))
                        {
                            process?.WaitForExit(2000);
                        }
                        
                        // Give processes time to fully terminate and release resources
                        Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] Waiting for CEF processes to terminate...");
                        System.Threading.Thread.Sleep(1000);
                        
                        Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] CEF processes cleaned");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] Warning: Could not kill CEF processes: {ex.Message}");
                    }
                }

                var baseDir = AppContext.BaseDirectory;
                
                // Clean all CEF-related directories
                var cacheDirectories = new[]
                {
                    Path.Combine(baseDir, "CEF"),
                    Path.Combine(baseDir, "cache"),
                    Path.Combine(baseDir, "GPUCache"),
                    Path.Combine(baseDir, "blob_storage"),
                    Path.Combine(baseDir, "databases"),
                    Path.Combine(baseDir, "Local Storage"),
                    Path.Combine(baseDir, "Session Storage"),
                    Path.Combine(baseDir, "IndexedDB"),
                    Path.Combine(baseDir, "Code Cache"),
                    Path.Combine(baseDir, "DawnCache"),
                    Path.Combine(baseDir, "Service Worker"),
                    Path.Combine(baseDir, ".fontconfig"),
                    Path.Combine(baseDir, ".cache"),
                };

                foreach (var dir in cacheDirectories)
                {
                    if (Directory.Exists(dir))
                    {
                        try
                        {
                            Directory.Delete(dir, recursive: true);
                        }
                        catch
                        {
                            // Ignore
                        }
                    }
                }

                // Clean specific CEF files that can cause segfaults
                var cefFiles = new[]
                {
                    Path.Combine(baseDir, "debug.log"),
                    Path.Combine(baseDir, "Cookies"),
                    Path.Combine(baseDir, "Cookies-journal"),
                    Path.Combine(baseDir, "TransportSecurity"),
                    Path.Combine(baseDir, "SingletonLock"),
                    Path.Combine(baseDir, "SingletonSocket"),
                    Path.Combine(baseDir, "SingletonCookie"),
                    Path.Combine(baseDir, ".com.google.Chrome.XXXXXX"),
                };

                foreach (var file in cefFiles)
                {
                    if (File.Exists(file))
                    {
                        try
                        {
                            File.Delete(file);
                        }
                        catch
                        {
                            // Ignore
                        }
                    }
                }

                // Clean any file matching CEF patterns
                try
                {
                    var patterns = new[] { "debug*.log", "SingletonLock*", "SingletonSocket*", ".com.google.Chrome.*" };
                    foreach (var pattern in patterns)
                    {
                        var files = Directory.GetFiles(baseDir, pattern, SearchOption.TopDirectoryOnly);
                        foreach (var file in files)
                        {
                            try
                            {
                                File.Delete(file);
                            }
                            catch
                            {
                                // Ignore
                            }
                        }
                    }
                }
                catch
                {
                    // Ignore
                }

                // Clean user data directories
                var userDataLocations = new[]
                {
                    Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "ECoopSystem"),
                    Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "ECoopSystem"),
                    Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Xilium.CefGlue"),
                    Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Xilium.CefGlue"),
                };

                // Clean font cache on Linux to prevent HarfBuzz segfaults
                if (OperatingSystem.IsLinux())
                {
                    var homeDir = Environment.GetEnvironmentVariable("HOME");
                    if (!string.IsNullOrEmpty(homeDir))
                    {
                        var fontCacheDirs = new[]
                        {
                            Path.Combine(homeDir, ".cache", "fontconfig"),
                            Path.Combine(homeDir, ".fontconfig"),
                        };

                        foreach (var fontCacheDir in fontCacheDirs)
                        {
                            if (Directory.Exists(fontCacheDir))
                            {
                                try
                                {
                                    Directory.Delete(fontCacheDir, recursive: true);
                                }
                                catch
                                {
                                    // Ignore
                                }
                            }
                        }
                    }
                }

                foreach (var userDataDir in userDataLocations)
                {
                    if (Directory.Exists(userDataDir))
                    {
                        // Don't delete the entire directory, just CEF-related subdirectories
                        var subdirs = new[] { "CEF", "GPUCache", "cache", "blob_storage", "databases", "Local Storage", "Session Storage", "IndexedDB", "Code Cache" };
                        foreach (var subdir in subdirs)
                        {
                            var path = Path.Combine(userDataDir, subdir);
                            if (Directory.Exists(path))
                            {
                                try
                                {
                                    Directory.Delete(path, recursive: true);
                                }
                                catch
                                {
                                    // Ignore
                                }
                            }
                        }

                        // Clean lock files
                        var lockFiles = new[] { "SingletonLock", "SingletonSocket", "debug.log" };
                        foreach (var lockFile in lockFiles)
                        {
                            var path = Path.Combine(userDataDir, lockFile);
                            if (File.Exists(path))
                            {
                                try
                                {
                                    File.Delete(path);
                                }
                                catch
                                {
                                    // Ignore
                                }
                            }
                        }
                    }
                }
            }
            catch
            {
                // Silently ignore cache cleanup errors
            }
        }

        [STAThread]
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
                // Add detailed console logging for Linux debugging
                if (OperatingSystem.IsLinux())
                {
                    Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] ECoopSystem starting...");
                    Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] .NET Version: {Environment.Version}");
                    Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] Working Directory: {Environment.CurrentDirectory}");
                    Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] User: {Environment.UserName}");
                }

                // Clean CEF cache on startup to prevent segmentation faults from corrupted cache
                try
                {
                    if (OperatingSystem.IsLinux())
                        Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] Cleaning CEF cache...");
                    
                    CleanCefCache();
                    
                    if (OperatingSystem.IsLinux())
                        Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] CEF cache cleaned");
                }
                catch (Exception ex)
                {
                    if (OperatingSystem.IsLinux())
                        Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] Warning: Failed to clean CEF cache: {ex.Message}");
                }

                AppDomain.CurrentDomain.UnhandledException += (sender, e) =>
                {
                    if (OperatingSystem.IsLinux())
                    {
                        Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] UNHANDLED EXCEPTION:");
                        Console.WriteLine(e.ExceptionObject?.ToString() ?? "null");
                    }
                };

                TaskScheduler.UnobservedTaskException += (sender, e) =>
                {
                    if (OperatingSystem.IsLinux())
                    {
                        Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] UNOBSERVED TASK EXCEPTION:");
                        Console.WriteLine(e.Exception?.ToString() ?? "null");
                    }
                    e.SetObserved();
                };

                if (OperatingSystem.IsLinux())
                    Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] Building Avalonia app...");

                BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
                
                if (OperatingSystem.IsLinux())
                    Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] Application exited normally");
            }
            catch (Exception ex)
            {
                if (OperatingSystem.IsLinux())
                {
                    Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] FATAL ERROR in Main:");
                    Console.WriteLine(ex.ToString());
                }
                throw;
            }
            finally
            {
                _mutex?.ReleaseMutex();
                _mutex?.Dispose();
            }
        }

        public static AppBuilder BuildAvaloniaApp()
        {
            try
            {
                if (OperatingSystem.IsLinux())
                    Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] Building configuration...");

                var configuration = new ConfigurationBuilder()
                    .SetBasePath(AppContext.BaseDirectory)
                    .AddJsonFile("appsettings.json", optional: true, reloadOnChange: false)
                    .Build();

                if (OperatingSystem.IsLinux())
                    Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] Setting up services...");

                var services = new ServiceCollection();
                services.AddSingleton<IConfiguration>(configuration);
                
                var keysDir = System.IO.Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                    "ECoopSystem",
                    "dp-keys"
                );

                if (OperatingSystem.IsLinux())
                    Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] Keys directory: {keysDir}");

                System.IO.Directory.CreateDirectory(keysDir);

                if (OperatingSystem.IsLinux())
                    Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] Configuring DataProtection...");

                services.AddDataProtection()
                        .PersistKeysToFileSystem(new System.IO.DirectoryInfo(keysDir))
                        .SetApplicationName("ECoopSystem");

                if (OperatingSystem.IsLinux())
                    Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] Registering stores...");

                services.AddSingleton<AppStateStore>();
                services.AddSingleton<SecretKeyStore>();
                
                if (OperatingSystem.IsLinux())
                    Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] Configuring HTTP client...");

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

                if (OperatingSystem.IsLinux())
                    Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] Building service provider...");

                var provider = services.BuildServiceProvider();

                if (OperatingSystem.IsLinux())
                    Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] Configuring Avalonia...");

                var builder = AppBuilder.Configure<App>()
                    .UsePlatformDetect()
                    .With(new X11PlatformOptions
                    {
                        EnableMultiTouch = false,
                        UseDBusMenu = false
                    });

                // Only set font on Windows to avoid Linux font corruption issues
                if (OperatingSystem.IsWindows())
                {
                    builder = builder.With(new FontManagerOptions
                    {
                        DefaultFamilyName = "Segoe UI"
                    });
                }

                if (OperatingSystem.IsLinux())
                    Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] Setting up Avalonia services...");

                return builder.AfterSetup(_ =>
                    {
                        if (OperatingSystem.IsLinux())
                            Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] AfterSetup callback...");
                        
                        App.Services = provider;
                    })
                    .With(new SkiaOptions { MaxGpuResourceSizeBytes = 0 })
                    .LogToTrace();
            }
            catch (Exception ex)
            {
                if (OperatingSystem.IsLinux())
                {
                    Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] ERROR in BuildAvaloniaApp:");
                    Console.WriteLine(ex.ToString());
                }
                throw;
            }
        }
    }
}
