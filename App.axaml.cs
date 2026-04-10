using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using System;

namespace ECoopSystem
{
    public partial class App : Application
    {
        public static IServiceProvider Services { get; set; } = null!;

        public override void Initialize()
        {
            AvaloniaXamlLoader.Load(this);
        }

        public override void OnFrameworkInitializationCompleted()
        {
            if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                desktop.MainWindow = new MainWindow();
                
                // Ensure proper cleanup on shutdown
                desktop.ShutdownRequested += OnShutdownRequested;
            }

            base.OnFrameworkInitializationCompleted();
        }

        private void OnShutdownRequested(object? sender, ShutdownRequestedEventArgs e)
        {
            // Give time for proper disposal
            System.Threading.Thread.Sleep(100);
        }

        public override void RegisterServices()
        {
            base.RegisterServices();
        }
    }
}