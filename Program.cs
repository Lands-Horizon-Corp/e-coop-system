using System;
using System.IO;
using Avalonia;
using ECoopSystem.Stores;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.DependencyInjection;

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

            services.AddSingleton<SecretKeyStore>();

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
