using ECoopSystem.Services;
using ECoopSystem.Stores;
using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace ECoopSystem.ViewModels;

public class MainViewModel : ViewModelBase
{
    private readonly ShellViewModel _shell;
    private readonly AppStateStore _store;
    private readonly AppState _state;
    private readonly SecretKeyStore _secretStore;
    private readonly LicenseService _licenseService;
    private readonly Stopwatch _loadingStopwatch = new();

    private bool _isLoading = true;
    private bool _isVerified;

    public event EventHandler? WebViewReady;

    public string URL { get; } = 
#if DEBUG
        "https://e-coop-client-development.up.railway.app/";
#else
        "https://e-coop-client-production.up.railway.app/"; // TODO: Replace with actual production URL
#endif

    public bool IsLoading
    {
        get => _isLoading;
        private set { _isLoading = value; OnPropertyChanged(); }
    }

    public bool IsVerified
    {
        get => _isVerified;
        private set { _isVerified = value; OnPropertyChanged(); }
    }

    public MainViewModel(ShellViewModel shell, AppStateStore store, AppState state, SecretKeyStore secretStore, LicenseService licenseService)
    {
        _shell = shell;
        _store = store;
        _state = state;
        _secretStore = secretStore;
        _licenseService = licenseService;
    }

    public async Task VerifyLicenseAsync()
    {
        IsLoading = true;
        IsVerified = false;
        _loadingStopwatch.Restart();

        try
        {
            var secret = _secretStore.Load();

            if (string.IsNullOrWhiteSpace(secret))
            {
                NavigateToActivation();
                return;
            }

            var fingerprint = FingerprintService.ComputeFingerprint(_state);
            var verify = await _licenseService.VerifyAsync(secret, fingerprint, _state.Counter, CancellationToken.None);

            if (verify.IsOk)
            {
                _state.LastVerifiedUtc = DateTimeOffset.UtcNow;
                _state.Counter++;
                _store.Save(_state);
                IsVerified = true;
            }
            else if (verify.IsInvalid)
            {
                _secretStore.Delete();
                NavigateToActivation();
                return;
            }
            else
            {
                // Server error - check grace period
                if (IsWithinGrace())
                {
                    IsVerified = true;
                }
                else
                {
                    NavigateToActivation();
                    return;
                }
            }
        }
        catch (TaskCanceledException)
        {
            // Timeout - check grace period
            if (IsWithinGrace())
            {
                IsVerified = true;
            }
            else
            {
                NavigateToActivation();
                return;
            }
        }
        catch (Exception)
        {
            // Network error - check grace period
            if (IsWithinGrace())
            {
                IsVerified = true;
            }
            else
            {
                NavigateToActivation();
                return;
            }
        }
        finally
        {
            // Ensure minimum loading time to hide website's white-black-content transitions
            await EnsureMinimumLoadingTime();
            IsLoading = false;
        }
    }

    private async Task EnsureMinimumLoadingTime()
    {
        var elapsed = _loadingStopwatch.Elapsed;
        var minimumTime = TimeSpan.FromSeconds(Constants.MinimumLoadingTimeSeconds);
        
        if (elapsed < minimumTime)
        {
            var remainingTime = minimumTime - elapsed;
            await Task.Delay(remainingTime);
        }
        
        _loadingStopwatch.Stop();
    }

    private bool IsWithinGrace()
    {
        var grace = TimeSpan.FromDays(Constants.GracePeriodDays);

        if (_state.LastVerifiedUtc is null)
            return true;

        return (DateTimeOffset.UtcNow - _state.LastVerifiedUtc.Value) <= grace;
    }

    private void NavigateToActivation()
    {
        _shell.Navigate(
            new ActivationViewModel(_shell, _store, _state, _secretStore, _licenseService),
            WindowMode.Locked
        );
    }

    public void Logout()
    {
        NavigateToActivation();
    }

    public void OnWebViewReady()
    {
        WebViewReady?.Invoke(this, EventArgs.Empty);
    }
}
