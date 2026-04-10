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
    private bool _webViewReadySignaled;

    private static void Log(string message)
    {
        Console.WriteLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] [MainViewModel] {message}");
    }

    public event EventHandler? WebViewReady;

    public string URL { get; } = BuildConfiguration.IFrameUrl;

    public bool IsLoading
    {
        get => _isLoading;
        private set 
        { 
            _isLoading = value; 
            OnPropertyChanged();
            OnPropertyChanged(nameof(IsWebViewVisible));
        }
    }

    public bool IsWebViewVisible => !IsLoading;

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
        Log("VerifyLicenseAsync started.");
        _webViewReadySignaled = false;
        IsLoading = true;
        IsVerified = false;
        _loadingStopwatch.Restart();

        try
        {
            var secret = _secretStore.Load();
            if (string.IsNullOrWhiteSpace(secret))
            {
                Log("No secret found. Navigating to activation.");
                NavigateToActivation();
                return;
            }

            var fingerprint = FingerprintService.ComputeFingerprint(_state);
            var verify = await _licenseService.VerifyAsync(secret, fingerprint, _state.Counter, CancellationToken.None);

            if (verify.IsOk)
            {
                Log("License verify result: OK.");
                _state.LastVerifiedUtc = DateTimeOffset.UtcNow;
                _state.Counter++;
                _store.Save(_state);
                IsVerified = true;
                StartBackgroundVerification();
            }
            else if (verify.IsInvalid)
            {
                Log("License verify result: INVALID. Deleting secret and navigating to activation.");
                _secretStore.Delete();
                NavigateToActivation();
                return;
            }
            else
            {
                Log("License verify result: transient failure.");
                if (IsWithinGrace())
                {
                    Log("Within grace period. Continuing to main view.");
                    IsVerified = true;
                    StartBackgroundVerification();
                }
                else
                {
                    Log("Outside grace period. Navigating to activation.");
                    NavigateToActivation();
                    return;
                }
            }
        }
        catch (Exception ex)
        {
            Log($"VerifyLicenseAsync exception: {ex}");
            if (IsWithinGrace())
            {
                Log("Exception tolerated due to grace period.");
                IsVerified = true;
                StartBackgroundVerification();
            }
            else
            {
                Log("Exception outside grace period. Navigating to activation.");
                NavigateToActivation();
                return;
            }
        }
        finally
        {
            if (IsVerified)
            {
                Log("VerifyLicenseAsync: verified, signaling WebView ready.");
                OnWebViewReady();
            }

            Log($"VerifyLicenseAsync finished. IsVerified={IsVerified}, IsLoading={IsLoading}");
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
        
        // On Linux, add extra delay to ensure CEF subprocess is fully ready
        if (OperatingSystem.IsLinux())
        {
            Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] MainViewModel: Adding Linux CEF stabilization delay...");
            
            await Task.Delay(2000); // Extra 2 seconds for CEF to stabilize on Linux
            
            Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] MainViewModel: CEF stabilization complete, showing WebView...");
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

    public async void OnWebViewReady()
    {
        if (_webViewReadySignaled)
            return;

        _webViewReadySignaled = true;
        Log("OnWebViewReady invoked.");
        await EnsureMinimumLoadingTime();

        IsLoading = false;
        Log("Loading complete. Raising WebViewReady event.");
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
