using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Media;
using ECoopSystem.Build;
using ECoopSystem.Services;
using ECoopSystem.Stores;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using WebViewControl;

namespace ECoopSystem
{
    internal class Program
    {
        private static System.Threading.Mutex? _mutex;
        private const string MutexName = "Global\\ECoopSystem-8F5A3D2C-1B4E-4C9A-A8F3-2D6E8C9B1A7F";

        private static bool IsCefSubprocess(string[] args)
        {
            return args.Any(a => a.StartsWith("--type=", StringComparison.OrdinalIgnoreCase));
        }

        private static bool IsEnabled(string envKey, bool defaultValue)
        {
            var raw = Environment.GetEnvironmentVariable(envKey);
            if (string.IsNullOrWhiteSpace(raw))
                return defaultValue;

            if (bool.TryParse(raw, out var parsedBool))
                return parsedBool;

            return raw == "1" || raw.Equals("yes", StringComparison.OrdinalIgnoreCase) || raw.Equals("on", StringComparison.OrdinalIgnoreCase);
        }

        private static void ValidateCefRuntimeFiles()
        {
            try
            {
                var baseDir = AppContext.BaseDirectory;
                var requiredFiles = new[]
                {
                    "libcef.dll",
                    "chrome_elf.dll",
                    "icudtl.dat",
                    "resources.pak",
                    "snapshot_blob.bin",
                    "v8_context_snapshot.bin",
                    "libEGL.dll",
                    "libGLESv2.dll",
                    "vk_swiftshader.dll",
                    "vk_swiftshader_icd.json"
                };

                foreach (var file in requiredFiles)
                {
                    var fullPath = Path.Combine(baseDir, file);
                    Log($"CEF file check: {file} => {(File.Exists(fullPath) ? "OK" : "MISSING")}");
                }

                var localesPath = Path.Combine(baseDir, "locales");
                Log($"CEF folder check: locales => {(Directory.Exists(localesPath) ? "OK" : "MISSING")}");

                var subprocessCandidates = new[]
                {
                    Path.Combine(baseDir, "CefGlueBrowserProcess.exe"),
                    Path.Combine(baseDir, "CefSharp.BrowserSubprocess.exe"),
                    Path.Combine(baseDir, "WebView.BrowserSubprocess.exe"),
                    Path.Combine(baseDir, "Xilium.CefGlue.BrowserSubprocess.exe")
                };

                var found = subprocessCandidates.Where(File.Exists).ToArray();
                Log(found.Length > 0
                    ? $"CEF subprocess executable detected: {string.Join(", ", found.Select(Path.GetFileName))}"
                    : "CEF subprocess executable not found (wrapper may reuse main exe).");
            }
            catch (Exception ex)
            {
                Log($"CEF runtime file validation failed: {ex}");
            }
        }

        private static void Log(string message)
        {
            Console.WriteLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] [Program] [PID:{Environment.ProcessId}] {message}");
        }

        private static string[] BuildSafeRuntimeArgs(string[] args)
        {
            var result = new List<string>(args);

            static bool HasSwitch(List<string> values, string name)
            {
                return values.Any(a => a.Equals(name, StringComparison.OrdinalIgnoreCase) || a.StartsWith(name + "=", StringComparison.OrdinalIgnoreCase));
            }

            var disableGpuMitigation = IsEnabled("ECOOP_DISABLE_GPU_MITIGATION", false);
            if (disableGpuMitigation)
            {
                Log("GPU mitigation disabled via ECOOP_DISABLE_GPU_MITIGATION.");
                return result.ToArray();
            }

            var useSwiftShader = IsEnabled("ECOOP_USE_SWIFTSHADER", OperatingSystem.IsLinux());
            var disableSoftwareRasterizer = IsEnabled("ECOOP_DISABLE_SOFTWARE_RASTERIZER", false);
            var forceInProcessGpu = IsEnabled("ECOOP_FORCE_IN_PROCESS_GPU", OperatingSystem.IsWindows());

            var gpuMitigationSwitches = new List<string>
            {
                "--disable-gpu",
                "--disable-gpu-compositing",
                "--disable-gpu-sandbox",
                "--no-sandbox"
            };

            if (forceInProcessGpu)
            {
                gpuMitigationSwitches.Add("--in-process-gpu");
                gpuMitigationSwitches.Add("--disable-gpu-process-crash-limit");
            }

            if (useSwiftShader)
                gpuMitigationSwitches.Add("--use-angle=swiftshader");

            if (disableSoftwareRasterizer)
                gpuMitigationSwitches.Add("--disable-software-rasterizer");

            foreach (var sw in gpuMitigationSwitches)
            {
                if (!HasSwitch(result, sw))
                {
                    result.Add(sw);
                }
            }

            return result.ToArray();
        }

        private static void ConfigureWebViewRuntime()
        {
            try
            {
                Log("Configuring WebView/CEF command-line switches.");

                var disableGpuMitigation = IsEnabled("ECOOP_DISABLE_GPU_MITIGATION", false);
                if (disableGpuMitigation)
                {
                    Log("Skipping WebView GPU switches due to ECOOP_DISABLE_GPU_MITIGATION.");
                    return;
                }

                var webViewAssembly = typeof(global::WebViewControl.WebView).Assembly;
                var globalSettingsType = webViewAssembly.GetType("WebViewControl.GlobalSettings");
                var webViewType = typeof(global::WebViewControl.WebView);

                if (globalSettingsType != null)
                {
                    object? settingsInstance = null;

                    settingsInstance = globalSettingsType.GetProperty("Instance", BindingFlags.Public | BindingFlags.Static)?.GetValue(null)
                        ?? globalSettingsType.GetProperty("Current", BindingFlags.Public | BindingFlags.Static)?.GetValue(null)
                        ?? webViewType.GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static)
                            .FirstOrDefault(p => p.PropertyType == globalSettingsType)
                            ?.GetValue(null)
                        ?? webViewType.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static)
                            .FirstOrDefault(f => f.FieldType == globalSettingsType)
                            ?.GetValue(null)
                        ?? Activator.CreateInstance(globalSettingsType);

                    Log($"GlobalSettings resolved: instanceFound={settingsInstance != null}");

                    var addCommandLineSwitchMethod = globalSettingsType.GetMethod(
                        "AddCommandLineSwitch",
                        BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance,
                        binder: null,
                        types: [typeof(string), typeof(string)],
                        modifiers: null);

                    if (addCommandLineSwitchMethod != null)
                    {
                        var useSwiftShader = IsEnabled("ECOOP_USE_SWIFTSHADER", OperatingSystem.IsLinux());
                        var disableSoftwareRasterizer = IsEnabled("ECOOP_DISABLE_SOFTWARE_RASTERIZER", false);
                        var forceInProcessGpu = IsEnabled("ECOOP_FORCE_IN_PROCESS_GPU", OperatingSystem.IsWindows());

                        var switches = new List<(string Name, string Value)>
                        {
                            ("disable-gpu", ""),
                            ("disable-gpu-compositing", ""),
                            ("no-sandbox", ""),
                            ("disable-gpu-sandbox", "")
                        };

                        if (forceInProcessGpu)
                        {
                            switches.Add(("in-process-gpu", ""));
                            switches.Add(("disable-gpu-process-crash-limit", ""));
                        }

                        if (useSwiftShader)
                        {
                            switches.Add(("use-angle", "swiftshader"));
                            switches.Add(("use-gl", "swiftshader"));
                            switches.Add(("enable-unsafe-swiftshader", ""));
                        }

                        if (disableSoftwareRasterizer)
                        {
                            switches.Add(("disable-software-rasterizer", ""));
                        }

                        foreach (var (name, value) in switches)
                        {
                            addCommandLineSwitchMethod.Invoke(
                                addCommandLineSwitchMethod.IsStatic ? null : settingsInstance,
                                [name, value]);
                        }

                        Log($"Applied {switches.Count} WebView/CEF command-line switches.");
                    }
                    else
                    {
                        Log("GlobalSettings.AddCommandLineSwitch method not found.");
                    }
                }
                else
                {
                    Log("WebViewControl.GlobalSettings type not found.");
                }

                if (IsEnabled("ECOOP_USE_SWIFTSHADER", OperatingSystem.IsLinux()))
                    Environment.SetEnvironmentVariable("ANGLE_DEFAULT_PLATFORM", "swiftshader");

                Log("WebView/CEF switches configured.");
            }
            catch (Exception ex)
            {
                Log($"Failed to configure WebView/CEF runtime switches: {ex}");
            }
        }

        private static void CleanCefCache()
        {
            try
            {
                // On Linux, aggressively kill any orphaned CEF processes first
                if (OperatingSystem.IsLinux())
                {
                    try
                    {
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
                        System.Threading.Thread.Sleep(1000);
                    }
                    catch
                    {
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
            Log($"Main entry. OS={Environment.OSVersion}; Args={string.Join(' ', args)}");

            var isCefSubprocess = IsCefSubprocess(args);
            Log($"Process role: {(isCefSubprocess ? "CEF subprocess" : "Main app process")}");

            args = BuildSafeRuntimeArgs(args);
            Log($"Runtime args after GPU mitigation: {string.Join(' ', args)}");

            AppDomain.CurrentDomain.UnhandledException += (_, eventArgs) =>
            {
                Log($"Unhandled exception: {eventArgs.ExceptionObject}");
            };

            var createdNew = true;
            if (!isCefSubprocess)
            {
                _mutex = new System.Threading.Mutex(true, MutexName, out createdNew);
                Log($"Mutex acquisition result: createdNew={createdNew}");

                if (!createdNew)
                {
                    Log("Another instance appears to be running. Exiting immediately.");
                    return;
                }
            }
            else
            {
                Log("Skipping mutex for CEF subprocess.");
            }

            try
            {
                Log("Startup sequence begins.");

                if (!isCefSubprocess)
                {
                    ValidateCefRuntimeFiles();
                }

                // Clean CEF cache on startup to prevent segmentation faults from corrupted cache
                try
                {
                    if (!isCefSubprocess)
                    {
                        Log("Starting CEF cache cleanup.");
                        CleanCefCache();
                        Log("CEF cache cleanup finished.");
                    }
                    else
                    {
                        Log("Skipping CEF cache cleanup for subprocess.");
                    }
                }
                catch (Exception ex)
                {
                    Log($"CEF cache cleanup failed: {ex}");
                }

                TaskScheduler.UnobservedTaskException += (sender, e) =>
                {
                    Log($"Unobserved task exception: {e.Exception}");
                    e.SetObserved();
                };

                ConfigureWebViewRuntime();

                Log("Building Avalonia app.");
                BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
                Log("Avalonia app exited normally.");
            }
            catch (Exception ex)
            {
                Log($"Fatal exception in Main: {ex}");
                throw;
            }
            finally
            {
                Log("Releasing mutex and finalizing process.");
                if (!isCefSubprocess)
                    _mutex?.ReleaseMutex();
                _mutex?.Dispose();
            }
        }

        public static AppBuilder BuildAvaloniaApp()
        {
            try
            {
                Log("BuildAvaloniaApp: loading configuration and services.");
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
                Log("BuildAvaloniaApp: service provider built.");

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

                return builder.AfterSetup(_ =>
                    {
                        Log("BuildAvaloniaApp: AfterSetup assigning App.Services.");
                        App.Services = provider;
                    })
                    .With(new SkiaOptions { MaxGpuResourceSizeBytes = 0 });
            }
            catch (Exception ex)
            {
                Log($"BuildAvaloniaApp failed: {ex}");
                throw;
            }
        }
    }
}
