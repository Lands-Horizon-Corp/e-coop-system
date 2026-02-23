using ECoopSystem.Build;
using ECoopSystem.Configuration;
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
    private PeriodicTimer? _backgroundVerificationTimer;
    private CancellationTokenSource? _backgroundVerificationCts;

    private bool _isLoading = true;
    private bool _isVerified;

    public event EventHandler? WebViewReady;

    public string URL { get; } = BuildConfiguration.IFrameUrl;

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
                StartBackgroundVerification();
            }
            else if (verify.IsInvalid)
            {
                _secretStore.Delete();
                NavigateToActivation();
                return;
            }
            else
            {
                if (IsWithinGrace())
                {
                    IsVerified = true;
                    StartBackgroundVerification();
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
            if (IsWithinGrace())
            {
                IsVerified = true;
                StartBackgroundVerification();
            }
            else
            {
                NavigateToActivation();
                return;
            }
        }
        catch
        {
            if (IsWithinGrace())
            {
                IsVerified = true;
                StartBackgroundVerification();
            }
            else
            {
                NavigateToActivation();
                return;
            }
        }
        finally
        {
            await EnsureMinimumLoadingTime();
            IsLoading = false;
        }
    }

    private async Task EnsureMinimumLoadingTime()
    {
        _loadingStopwatch.Stop();
        var elapsed = _loadingStopwatch.Elapsed;
        var minimumTime = TimeSpan.FromSeconds(Constants.MinimumLoadingTimeSeconds);
        
        if (elapsed < minimumTime)
        {
            await Task.Delay(minimumTime - elapsed);
        }
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
        var activationViewModel = new ActivationViewModel(
            _shell, 
            _store, 
            _state, 
            _secretStore, 
            _licenseService);
        _shell.Navigate(activationViewModel, WindowMode.Locked);
    }

    private void StartBackgroundVerification()
    {
        StopBackgroundVerification();

        var intervalMinutes = Constants.BackgroundVerificationIntervalMinutes;
        if (intervalMinutes <= 0)
        {
            return;
        }

        _backgroundVerificationCts = new CancellationTokenSource();
        _backgroundVerificationTimer = new PeriodicTimer(TimeSpan.FromMinutes(intervalMinutes));
        
        _ = RunBackgroundVerificationAsync(_backgroundVerificationCts.Token);
    }

    private void StopBackgroundVerification()
    {
        _backgroundVerificationCts?.Cancel();
        _backgroundVerificationCts?.Dispose();
        _backgroundVerificationCts = null;
        
        _backgroundVerificationTimer?.Dispose();
        _backgroundVerificationTimer = null;
    }

    private async Task RunBackgroundVerificationAsync(CancellationToken ct)
    {
        try
        {
            while (await _backgroundVerificationTimer!.WaitForNextTickAsync(ct))
            {
                await PerformBackgroundVerificationAsync(ct);
            }
        }
        catch (OperationCanceledException)
        {
            // Cancelled
        }
        catch
        {
            // Ignore
        }
    }

    private async Task PerformBackgroundVerificationAsync(CancellationToken ct)
    {
        try
        {
            var secret = _secretStore.Load();

            if (string.IsNullOrWhiteSpace(secret))
            {
                StopBackgroundVerification();
                Logout();
                return;
            }

            var fingerprint = FingerprintService.ComputeFingerprint(_state);
            var verify = await _licenseService.VerifyAsync(secret, fingerprint, _state.Counter, ct);

            if (verify.IsOk)
            {
                _state.LastVerifiedUtc = DateTimeOffset.UtcNow;
                _state.Counter++;
                _store.Save(_state);
            }
            else if (verify.IsInvalid)
            {
                _secretStore.Delete();
                StopBackgroundVerification();
                Logout();
            }
            else
            {
                if (!IsWithinGrace())
                {
                    StopBackgroundVerification();
                    Logout();
                }
            }
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch
        {
            if (!IsWithinGrace())
            {
                StopBackgroundVerification();
                Logout();
            }
        }
    }

    public void Logout()
    {
        StopBackgroundVerification();
        NavigateToActivation();
    }

    public void OnWebViewReady()
    {
        WebViewReady?.Invoke(this, EventArgs.Empty);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            StopBackgroundVerification();
        }
        base.Dispose(disposing);
    }
}
