using Avalonia.Controls;
using ECoopSystem.ViewModels;
using System.ComponentModel;
using ECoopSystem.Stores;
using ECoopSystem.Services;
using System.Net.Http;
using System.Threading.Tasks;
using System.Threading;
using System;
using Avalonia.Threading;
using Avalonia.Platform;
using Microsoft.Extensions.DependencyInjection;

namespace ECoopSystem;

public partial class MainWindow : Window
{
    private readonly ShellViewModel _shell;
    private readonly AppStateStore _stateStore;
    private readonly AppState _state;
    private readonly SecretKeyStore _secretStore;
    private readonly LicenseService _licenseService;

    private bool _booted;

    private sealed record RouteResult(ViewModelBase ViewModel, WindowMode Mode);

    public MainWindow()
    {
        InitializeComponent();

        _stateStore = new AppStateStore();
        _state = _stateStore.Load();
        _stateStore.Save(_state);

        _secretStore = App.Services.GetRequiredService<SecretKeyStore>();
        var http = new HttpClient();
        _licenseService = new LicenseService(http);

        _shell = new ShellViewModel();
        DataContext = _shell;

        _shell.Navigate(new LoadingViewModel(), WindowMode.Locked);
        ApplyWindowMode();

        _shell.PropertyChanged += ShellOnPropertyChanged;
        Opened += async (_, _) =>
        {
            if (_booted) return;
            _booted = true;

            var loadingVm = new LoadingViewModel("Initializing...");
            _shell.Navigate(loadingVm, WindowMode.Locked);
            ApplyWindowMode();
            
            await Dispatcher.UIThread.InvokeAsync(() => { }, DispatcherPriority.Background);

            var minDelay = Task.Delay(TimeSpan.FromSeconds(5));
            var routeTask = DecideRouteAsync();

            await Task.WhenAll(minDelay, routeTask);

            var route = await routeTask;

            _shell.Navigate(route.ViewModel, route.Mode);
            ApplyWindowMode();

            if (route.ViewModel is BlockingViewModel)
            {
                await Task.Delay(1500);
                Close();
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
            const int w = 1280;
            const int h = 720;

            Width = w;
            Height = h;

            MinWidth = MaxWidth = w;
            MinHeight = MaxHeight = h;

            CanResize = false;
            SystemDecorations = SystemDecorations.None;
        }
        else
        {
            // Reset size constraints
            const int w = 1280;
            const int h = 720;

            Width = w;
            Height = h;

            MinWidth = w;
            MinHeight = h;

            MaxWidth = double.PositiveInfinity;
            MaxHeight = double.PositiveInfinity;

            CanResize = true;
            SystemDecorations = SystemDecorations.Full;
            ExtendClientAreaToDecorationsHint = false;
            ExtendClientAreaChromeHints = ExtendClientAreaChromeHints.PreferSystemChrome;
            ExtendClientAreaTitleBarHeightHint = -1;

            if (_shell.Current is MainViewModel mv)
            {
                WindowState = WindowState.Maximized;
            }
        }
    }

    private async Task<RouteResult> DecideRouteAsync()
    {
        var secret = _secretStore.Load();

        if (string.IsNullOrEmpty(secret))
            return new RouteResult(
                new ActivationViewModel(_shell, _stateStore, _state, _secretStore, _licenseService),
                WindowMode.Locked);

        var fingerprint = FingerprintService.ComputeFingerprint(_state);

        try
        {
            var verify = await _licenseService.VerifyAsync(secret, fingerprint, CancellationToken.None);

            if (verify.IsOk)
            {
                _state.LastVerifiedUtc = DateTimeOffset.UtcNow; 
                _stateStore.Save(_state);

                if (_state.WelcomeShown)
                    return new RouteResult(new MainViewModel(_shell, _stateStore, _state, _secretStore, _licenseService), 
                                           WindowMode.Normal);

                return new RouteResult(new WelcomeViewModel(_shell, _stateStore, _state, _secretStore, _licenseService), 
                                       WindowMode.Locked);
            }

            if (verify.IsInvalid)
            {
                // License revoked/invalid for this machine
                _secretStore.Delete();
                return new RouteResult(new ActivationViewModel(_shell, _stateStore, _state, _secretStore, _licenseService),
                                       WindowMode.Normal);
            }

            // Server returns 500
            return new RouteResult(
                new BlockingViewModel("Verification Error",
                                      "License verification failed due to a server error, Please try again later."), 
                                       WindowMode.Locked);
        }
        catch (TaskCanceledException)
        {
            return DecideRouteWithGrace();
        }
        catch
        {
            return DecideRouteWithGrace();
        }
    }

    private bool IsWithinGrace()
    {
        var grace = TimeSpan.FromDays(7);

        if (_state.LastVerifiedUtc is null)
            return true;

        return (DateTimeOffset.UtcNow - _state.LastVerifiedUtc.Value) <= grace;
    }

    private RouteResult DecideRouteWithGrace()
    {
        if (IsWithinGrace())
        {
            if (_state.WelcomeShown)
                return new RouteResult(
                     new MainViewModel(_shell, _stateStore, _state, _secretStore, _licenseService),
                     WindowMode.Normal);

            return new RouteResult(
                 new WelcomeViewModel(_shell, _stateStore, _state, _secretStore, _licenseService),
                 WindowMode.Locked);
        }

        return new RouteResult(
            new BlockingViewModel("Verification Error",
                                  "We couldn't verify your license. Please connect to the internet and restart."),
                                  WindowMode.Locked);
    }
}