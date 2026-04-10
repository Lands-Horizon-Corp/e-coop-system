using Avalonia.Controls;
using System;
using System.ComponentModel;
using Avalonia.Input;
using Avalonia.VisualTree;
using ECoopSystem.Build;
using ECoopSystem.Services;
using ECoopSystem.Stores;
using ECoopSystem.ViewModels;
using Microsoft.Extensions.DependencyInjection;

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

    private static void Log(string message)
    {
        Console.WriteLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] [MainWindow] {message}");
    }

    public MainWindow()
    {
        try
        {
            Log("Starting initialization.");

            InitializeComponent();

            Log("InitializeComponent done.");

            Log("Resolving AppStateStore.");

            _stateStore = App.Services.GetRequiredService<AppStateStore>();

            Log("Loading persisted app state.");

            try
            {
                _state = _stateStore.Load();
                _stateStore.Save(_state);
            }
            catch (Exception ex)
            {
                Log($"Error loading/saving state. Attempting reset. Details: {ex}");

                try
                {
                    var configDir = System.IO.Path.Combine(
                        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                        "ECoopSystem");
                    var stateFile = System.IO.Path.Combine(configDir, "appstate.dat");
                    if (System.IO.File.Exists(stateFile))
                        System.IO.File.Delete(stateFile);
                }
                catch
                {
                    // Ignore cleanup errors
                }

                _state = _stateStore.Load();
                _stateStore.Save(_state);
            }

            _secretStore = App.Services.GetRequiredService<SecretKeyStore>();
            _licenseService = App.Services.GetRequiredService<LicenseService>();
            Log("Resolved SecretKeyStore and LicenseService.");

            _shell = new ShellViewModel();
            DataContext = _shell;
            Log("Shell view model created and assigned.");

            _shell.PropertyChanged += ShellOnPropertyChanged;
            Closing += OnClosing;
            AddHandler(KeyDownEvent, OnKeyDown, Avalonia.Interactivity.RoutingStrategies.Tunnel);

            Opened += async (_, _) =>
            {
                if (_hasOpened)
                    return;

                _hasOpened = true;
                Log("Window opened event fired.");

                var route = DecideInitialRoute();
                Log($"Initial route decided: {route.ViewModel.GetType().Name}, mode={route.Mode}.");
                _shell.Navigate(route.ViewModel, route.Mode);
                ApplyWindowMode();

                if (route.ViewModel is MainViewModel mainVm)
                {
                    Log("Triggering MainViewModel.VerifyLicenseAsync.");
                    await mainVm.VerifyLicenseAsync();
                    Log("MainViewModel.VerifyLicenseAsync completed.");
                }
            };
        }
        catch (Exception ex)
        {
            Log($"Fatal initialization error: {ex}");

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
        Log($"Applying window mode: {_shell.Mode}.");

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

    private void OnWebViewReady(object? sender, EventArgs e)
    {
        Log("WebView reported ready. Maximizing window.");
        WindowState = WindowState.Maximized;
    }

    private void OnKeyDown(object? sender, Avalonia.Input.KeyEventArgs e)
    {
        if (e.Key == Avalonia.Input.Key.F5)
        {
            e.Handled = true;
            
            if (_shell.Current is MainViewModel)
            {
                var mainView = FindMainView(this);
                
                if (mainView != null)
                {
                    mainView.ReloadWebView();
                }
            }
        }
    }

    private Views.MainView? FindMainView(Avalonia.Controls.Control control)
    {
        // Direct check
        if (control is Views.MainView mainView)
            return mainView;

        // Recursively search children
        foreach (var child in control.GetVisualChildren())
        {
            if (child is Avalonia.Controls.Control childControl)
            {
                var found = FindMainView(childControl);
                if (found != null)
                    return found;
            }
        }

        return null;
    }

    private void OnClosing(object? sender, WindowClosingEventArgs e)
    {
        Log("Window closing. Disposing current view model.");

        // Unsubscribe from events
        _shell.PropertyChanged -= ShellOnPropertyChanged;
        RemoveHandler(KeyDownEvent, OnKeyDown);
        
        // Dispose current ViewModel
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
        Log("Window close cleanup finished.");
    }

    private RouteResult DecideInitialRoute()
    {
        var secret = _secretStore.Load();
        Log($"Secret loaded. IsEmpty={string.IsNullOrWhiteSpace(secret)}");

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