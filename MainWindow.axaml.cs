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

    public MainWindow()
    {
        try
        {
            InitializeComponent();

            _stateStore = App.Services.GetRequiredService<AppStateStore>();

            try
            {
                _state = _stateStore.Load();
                _stateStore.Save(_state);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] [MainWindow] Error loading/saving state. Attempting reset. Details: {ex}");

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

            _shell = new ShellViewModel();
            DataContext = _shell;

            _shell.PropertyChanged += ShellOnPropertyChanged;
            Closing += OnClosing;
            AddHandler(KeyDownEvent, OnKeyDown, Avalonia.Interactivity.RoutingStrategies.Tunnel);

            Opened += async (_, _) =>
            {
                if (_hasOpened)
                    return;

                _hasOpened = true;

                var route = DecideInitialRoute();
                _shell.Navigate(route.ViewModel, route.Mode);
                ApplyWindowMode();

                if (route.ViewModel is MainViewModel mainVm)
                {
                    await mainVm.VerifyLicenseAsync();
                }
            };
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] [MainWindow] Fatal initialization error: {ex}");

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

    private void OnWebViewReady(object? sender, EventArgs e)
    {
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