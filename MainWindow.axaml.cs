using Avalonia.Controls;
using ECoopSystem.ViewModels;
using System.ComponentModel;
using ECoopSystem.Stores;
using ECoopSystem.Services;
using Microsoft.Extensions.DependencyInjection;

namespace ECoopSystem;

public partial class MainWindow : Window
{
    private readonly ShellViewModel _shell;
    private readonly AppStateStore _stateStore;
    private readonly AppState _state;
    private readonly SecretKeyStore _secretStore;
    private readonly LicenseService _licenseService;

    private sealed record RouteResult(ViewModelBase ViewModel, WindowMode Mode);

    public MainWindow()
    {
        InitializeComponent();

        _stateStore = App.Services.GetRequiredService<AppStateStore>();
        _state = _stateStore.Load();
        _stateStore.Save(_state);

        _secretStore = App.Services.GetRequiredService<SecretKeyStore>();
        _licenseService = App.Services.GetRequiredService<LicenseService>();

        _shell = new ShellViewModel();
        DataContext = _shell;

        _shell.PropertyChanged += ShellOnPropertyChanged;
        Closing += OnClosing;
        
        Opened += async (_, _) =>
        {
            var route = DecideInitialRoute();
            _shell.Navigate(route.ViewModel, route.Mode);
            ApplyWindowMode();
            
            if (route.ViewModel is MainViewModel mainVm)
            {
                await mainVm.VerifyLicenseAsync();
            }
        };
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

    private void OnWebViewReady(object? sender, System.EventArgs e)
    {
        // Maximize window only after WebView is fully loaded
        WindowState = WindowState.Maximized;
    }
    
    private void OnClosing(object? sender, WindowClosingEventArgs e)
    {
        // Unsubscribe from events
        _shell.PropertyChanged -= ShellOnPropertyChanged;
        
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