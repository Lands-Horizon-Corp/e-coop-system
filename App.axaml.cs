using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using System;

namespace ECoopSystem
{
    public partial class App : Application
    {
        public static IServiceProvider Services { get; set; } = null!;

        private static void Log(string message)
        {
            Console.WriteLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] [App] {message}");
        }

        public override void Initialize()
        {
            Log("Initialize start.");
            AvaloniaXamlLoader.Load(this);
            Log("Initialize complete.");
        }

        public override void OnFrameworkInitializationCompleted()
        {
            Log("OnFrameworkInitializationCompleted start.");
            if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                desktop.MainWindow = new MainWindow();
                Log("MainWindow created and assigned.");
                
                // Ensure proper cleanup on shutdown
                desktop.ShutdownRequested += OnShutdownRequested;
            }

            base.OnFrameworkInitializationCompleted();
            Log("OnFrameworkInitializationCompleted complete.");
        }

        private void OnShutdownRequested(object? sender, ShutdownRequestedEventArgs e)
        {
            Log("Shutdown requested.");
            // Give time for proper disposal
            System.Threading.Thread.Sleep(100);
            Log("Shutdown delay complete.");
        }

        public override void RegisterServices()
        {
            base.RegisterServices();
        }
    }
}