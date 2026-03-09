using Avalonia.Controls;
using ECoopSystem.ViewModels;
using System;
using System.ComponentModel;
using ECoopSystem.Stores;
using ECoopSystem.Services;
using Microsoft.Extensions.DependencyInjection;
using System.Threading.Tasks;

namespace ECoopSystem;

public partial class MainWindow : Window
{
    private readonly ShellViewModel _shell;
    private readonly AppStateStore _stateStore;
    private readonly AppState _state;
    private readonly SecretKeyStore _secretStore;
    private readonly LicenseService _licenseService;

    private bool _hasOpened;

    private sealed record RouteResult(ViewModelBase ViewModel, WindowMode Mode);

    public MainWindow()
    {
        try
        {
            if (OperatingSystem.IsLinux())
                Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] MainWindow: Starting initialization...");

            InitializeComponent();

            if (OperatingSystem.IsLinux())
                Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] MainWindow: InitializeComponent done");

            try
            {
                if (OperatingSystem.IsLinux())
                    Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] MainWindow: Getting AppStateStore...");

                _stateStore = App.Services.GetRequiredService<AppStateStore>();
                
                if (OperatingSystem.IsLinux())
                    Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] MainWindow: Loading state...");

                // Load state with error recovery
                try
                {
                    _state = _stateStore.Load();
                    
                    if (OperatingSystem.IsLinux())
                        Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] MainWindow: State loaded, saving...");

                    _stateStore.Save(_state);
                    
                    if (OperatingSystem.IsLinux())
                        Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] MainWindow: State saved");
                }
                catch (Exception ex)
                {
                    if (OperatingSystem.IsLinux())
                    {
                        Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] MainWindow: ERROR loading/saving state: {ex.Message}");
                        Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] MainWindow: Stack trace: {ex.StackTrace}");
                    }

                    System.Diagnostics.Debug.WriteLine($"MainWindow: Failed to load/save state: {ex}");
                    
                    // Try to recover by deleting corrupted data
                    try
                    {
                        if (OperatingSystem.IsLinux())
                            Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] MainWindow: Attempting recovery...");

                        var configDir = System.IO.Path.Combine(
                            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                            "ECoopSystem");
                        var stateFile = System.IO.Path.Combine(configDir, "appstate.dat");
                        if (System.IO.File.Exists(stateFile))
                        {
                            if (OperatingSystem.IsLinux())
                                Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] MainWindow: Deleting corrupted state file...");

                            System.IO.File.Delete(stateFile);
                        }
                    }
                    catch (Exception cleanupEx)
                    {
                        if (OperatingSystem.IsLinux())
                            Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] MainWindow: Cleanup failed: {cleanupEx.Message}");
                    }
                    
                    // Load fresh state
                    if (OperatingSystem.IsLinux())
                        Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] MainWindow: Loading fresh state...");

                    _state = _stateStore.Load();
                    _stateStore.Save(_state);
                }

                if (OperatingSystem.IsLinux())
                    Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] MainWindow: Getting SecretKeyStore...");

                _secretStore = App.Services.GetRequiredService<SecretKeyStore>();
                
                if (OperatingSystem.IsLinux())
                    Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] MainWindow: Getting LicenseService...");

                _licenseService = App.Services.GetRequiredService<LicenseService>();

                if (OperatingSystem.IsLinux())
                    Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] MainWindow: Creating ShellViewModel...");

                _shell = new ShellViewModel();
                DataContext = _shell;

                var initialRoute = DecideInitialRoute();
                _shell.Mode = initialRoute.Mode;
                ApplyWindowMode();

                _shell.PropertyChanged += ShellOnPropertyChanged;
                Closing += OnClosing;

                Opened += async (_, _) =>
                {
                    if (_hasOpened) return;
                    _hasOpened = true;

                    if (OperatingSystem.IsLinux())
                        Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] MainWindow: Window opened, proceeding with route...");

                    // On Linux with MainViewModel, wait BEFORE creating the view to let any previous CEF cleanup complete
                    if (OperatingSystem.IsLinux() && initialRoute.ViewModel is MainViewModel)
                    {
                        Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] MainWindow: Pre-navigation CEF cleanup delay...");
                        await Task.Delay(1000);
                        Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] MainWindow: CEF cleanup delay complete");
                    }
                    else
                    {
                        await Task.Delay(100);
                    }

                    if (OperatingSystem.IsLinux())
                        Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] MainWindow: Navigating...");

                    _shell.Navigate(initialRoute.ViewModel, initialRoute.Mode);

                    if (OperatingSystem.IsLinux())
                        Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] MainWindow: Navigation complete");

                    // On Linux with MainViewModel, wait after navigation for CEF render process to initialize
                    if (OperatingSystem.IsLinux() && initialRoute.ViewModel is MainViewModel)
                    {
                        Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] MainWindow: Waiting for CEF render process...");
                        await Task.Delay(2000);
                        Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] MainWindow: CEF render process ready");
                    }
                    else
                    {
                        await Task.Delay(100);
                    }

                    if (initialRoute.ViewModel is MainViewModel mainVm)
                    {
                        if (OperatingSystem.IsLinux())
                            Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] MainWindow: Verifying license...");

                        await mainVm.VerifyLicenseAsync();
                    }

                    if (OperatingSystem.IsLinux())
                        Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] MainWindow: Initialization complete");
                };
            }
            catch (Exception ex)
            {
                if (OperatingSystem.IsLinux())
                {
                    Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] MainWindow: FATAL ERROR during initialization:");
                    Console.WriteLine(ex.ToString());
                }

                System.Diagnostics.Debug.WriteLine($"MainWindow: Fatal initialization error: {ex}");
                throw;
            }
        }
        catch (Exception ex)
        {
            if (OperatingSystem.IsLinux())
            {
                Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] MainWindow: OUTER FATAL ERROR:");
                Console.WriteLine(ex.ToString());
            }
            throw;
        }
    }

    private void ShellOnPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(ShellViewModel.Mode))
            ApplyWindowMode();
    }

    private void ApplyWindowMode()
    {
        if (_shell.Mode == WindowMode.Locked)
        {
            Width = Constants.WindowWidth;
            Height = Constants.WindowHeight;

            MinWidth = MaxWidth = Constants.WindowWidth;
            MinHeight = MaxHeight = Constants.WindowHeight;

            CanResize = false;
            SystemDecorations = SystemDecorations.None;
        }
        else
        {
            Width = Constants.WindowWidth;
            Height = Constants.WindowHeight;

            MinWidth = Constants.WindowWidth;
            MinHeight = Constants.WindowHeight;

            MaxWidth = double.PositiveInfinity;
            MaxHeight = double.PositiveInfinity;

            CanResize = true;
            SystemDecorations = SystemDecorations.Full;
            ExtendClientAreaToDecorationsHint = false;
            ExtendClientAreaChromeHints = Avalonia.Platform.ExtendClientAreaChromeHints.PreferSystemChrome;
            ExtendClientAreaTitleBarHeightHint = -1;

            // Subscribe to WebViewReady event if MainViewModel
            if (_shell.Current is MainViewModel mainVm)
            {
                mainVm.WebViewReady -= OnWebViewReady;
                mainVm.WebViewReady += OnWebViewReady;
            }
        }
    }

    private async void OnWebViewReady(object? sender, System.EventArgs e)
    {
        // Maximize window only after WebView is fully loaded
        await Task.Delay(500);
        if (this.IsVisible)
        {
            WindowState = WindowState.Maximized;
        }
    }

    private void OnClosing(object? sender, WindowClosingEventArgs e)
    {
        // Unsubscribe from events
        _shell.PropertyChanged -= ShellOnPropertyChanged;
        
        // Dispose current ViewModel (which disposes MainView)
        if (_shell.Current is MainViewModel mainVm)
        {
            mainVm.WebViewReady -= OnWebViewReady;
            mainVm.Dispose();
        }
        else if (_shell.Current is ActivationViewModel activationVm)
        {
            activationVm.Dispose();
        }

        // Give time for all disposal operations to complete
        System.Threading.Thread.Sleep(100);
    }

    private RouteResult DecideInitialRoute()
    {
        var secret = _secretStore.Load();

        if (string.IsNullOrWhiteSpace(secret))
        {
            return new RouteResult(
                new ActivationViewModel(
                    _shell, 
                    _stateStore, 
                    _state, 
                    _secretStore, 
                    _licenseService),
                WindowMode.Locked);
        }

        return new RouteResult(
            new MainViewModel(
                _shell, 
                _stateStore, 
                _state, 
                _secretStore, 
                _licenseService),
            WindowMode.Normal);
    }
}