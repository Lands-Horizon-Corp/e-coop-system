using Avalonia.Controls;
using ECoopSystem.ViewModels;
using System.ComponentModel;
using ECoopSystem.Stores;
using ECoopSystem.Services;
using System.Net.Http;
using Avalonia.Threading;
using System.Threading.Tasks;
using System.Threading;
using System;

namespace ECoopSystem;

public partial class MainWindow : Window
{
    private readonly ShellViewModel _shell;
    private readonly AppStateStore _stateStore;
    private readonly AppState _state;
    private readonly SecretKeyStore _secretStore;
    private readonly LicenseService _licenseService;

    public MainWindow()
    {
        InitializeComponent();

        _stateStore = new AppStateStore();
        _state = _stateStore.Load();
        _stateStore.Save(_state);

        _secretStore = new SecretKeyStore(_state);
        var http = new HttpClient();
        _licenseService = new LicenseService(http);

        _shell = new ShellViewModel();
        DataContext = _shell;

        _shell.PropertyChanged += ShellOnPropertyChanged;
        Opened += async (_, _) =>
        {
            await StartupRouteAsync();
            ApplyWindowMode();
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

            MinWidth = 900;
            MinHeight = 600;

            MaxWidth = double.PositiveInfinity;
            MaxHeight = double.PositiveInfinity;

            CanResize = true;
            SystemDecorations = SystemDecorations.Full;
        }
    }

    private async Task StartupRouteAsync()
    {
        var secret = _secretStore.Load();

        if (string.IsNullOrEmpty(secret))
        {
            _shell.Navigate(new ActivationViewModel(_shell, _stateStore, _state, _secretStore, _licenseService), WindowMode.Locked);
            return;
        }

        var fingerprint = FingerprintService.ComputeFingerprint(_state);

        try
        {
            var verify = await _licenseService.VerifyAsync(secret, fingerprint, CancellationToken.None);

            if (verify.IsOk)
            {
                _state.LastVerifiedUtc = DateTimeOffset.UtcNow; 
                _stateStore.Save(_state);

                if (_state.WelcomeShown)
                    _shell.Navigate(new MainViewModel(_shell, _stateStore, _state, _secretStore, _licenseService), WindowMode.Normal);
                else
                    _shell.Navigate(new WelcomeViewModel(_shell, _stateStore, _state, _secretStore, _licenseService), WindowMode.Locked);

                return;
            }

            if (verify.IsInvalid)
            {
                // License revoked/invalid for this machine
                _secretStore.Delete();
                _shell.Navigate(new ActivationViewModel(_shell, _stateStore, _state, _secretStore, _licenseService), WindowMode.Locked);
                return;
            }

            // Server returns 500
            _secretStore.Delete();
            _shell.Navigate(new BlockingViewModel("Verification Error", 
                "License verification failed due to a server error, Please try again later."), WindowMode.Locked);

            // Close after short delay
            await Task.Delay(1500);
            Close();
        }
        catch (TaskCanceledException)
        {
            RouteWithGrace();
        }
        catch (Exception)
        {
            RouteWithGrace();
        }
    }

    private bool IsWithinGrace()
    {
        var grace = TimeSpan.FromDays(7);

        if (_state.LastVerifiedUtc is null)
            return true;

        return (DateTimeOffset.UtcNow - _state.LastVerifiedUtc.Value) <= grace;
    }

    private void RouteWithGrace()
    {
        if (IsWithinGrace())
        {
            if (IsWithinGrace())
            {
                if (_state.WelcomeShown)
                    _shell.Navigate(new MainViewModel(_shell, _stateStore, _state, _secretStore, _licenseService), WindowMode.Normal);
                else
                    _shell.Navigate(new WelcomeViewModel(_shell, _stateStore, _state, _secretStore, _licenseService), WindowMode.Locked);
            }
            else
            {
                _shell.Navigate(new BlockingViewModel("Verification Error",
                    "We couldn't verify your license. Please connect to the internet and restart."), WindowMode.Locked);
            }
        }
    }
}