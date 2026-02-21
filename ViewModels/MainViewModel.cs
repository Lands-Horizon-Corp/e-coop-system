using ECoopSystem.Build;
using ECoopSystem.Configuration;
using ECoopSystem.Services;
using ECoopSystem.Stores;
using Microsoft.Extensions.Logging;
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
    private readonly ILogger<MainViewModel> _logger;
    private readonly ILoggerFactory _loggerFactory;
    private readonly Stopwatch _loadingStopwatch = new();

    private bool _isLoading = true;
    private bool _isVerified;

    public event EventHandler? WebViewReady;

    public string URL { get; } = 
#if DEBUG
        ConfigurationLoader.Current.WebViewSettings.BaseUrl;
#else
        GetProductionUrl();
#endif

    private static string GetProductionUrl()
    {
        var buildUrl = BuildConfiguration.IFrameUrl;
        if (!string.IsNullOrEmpty(buildUrl) && 
            !buildUrl.Contains("$(") && // Not a placeholder
            buildUrl != "https://e-coop-client-development.up.railway.app/")
        {
            return buildUrl;
        }
        return ConfigurationLoader.Current.WebViewSettings.BaseUrl;
    }

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

    public MainViewModel(ShellViewModel shell, AppStateStore store, AppState state, SecretKeyStore secretStore, LicenseService licenseService, ILogger<MainViewModel> logger, ILoggerFactory loggerFactory)
    {
        _shell = shell;
        _store = store;
        _state = state;
        _secretStore = secretStore;
        _licenseService = licenseService;
        _logger = logger;
        _loggerFactory = loggerFactory;
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
                _logger.LogInformation("License verification successful");
            }
            else if (verify.IsInvalid)
            {
                _logger.LogWarning("License verification failed: Invalid license");
                _secretStore.Delete();
                NavigateToActivation();
                return;
            }
            else
            {
                // Server error - check grace period
                _logger.LogWarning("License verification server error, checking grace period");
                if (IsWithinGrace())
                {
                    IsVerified = true;
                    _logger.LogInformation("Within grace period, allowing access");
                }
                else
                {
                    _logger.LogWarning("Grace period expired, navigating to activation");
                    NavigateToActivation();
                    return;
                }
            }
        }
        catch (TaskCanceledException ex)
        {
            // Timeout - check grace period
            _logger.LogWarning(ex, "License verification timed out");
            if (IsWithinGrace())
            {
                IsVerified = true;
                _logger.LogInformation("Timeout within grace period, allowing access");
            }
            else
            {
                _logger.LogWarning("Timeout and grace period expired");
                NavigateToActivation();
                return;
            }
        }
        catch (Exception ex)
        {
            // Network error - check grace period
            _logger.LogError(ex, "License verification failed with unexpected error");
            if (IsWithinGrace())
            {
                IsVerified = true;
                _logger.LogInformation("Error within grace period, allowing access");
            }
            else
            {
                _logger.LogWarning("Error and grace period expired");
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
        var activationViewModel = new ActivationViewModel(
            _shell, 
            _store, 
            _state, 
            _secretStore, 
            _licenseService, 
            _loggerFactory.CreateLogger<ActivationViewModel>(),
            _loggerFactory);
        _shell.Navigate(activationViewModel, WindowMode.Locked);
    }

    public void Logout()
    {
        _logger.LogInformation("User logged out");
        NavigateToActivation();
    }

    public void OnWebViewReady()
    {
        _logger.LogDebug("WebView ready event triggered");
        WebViewReady?.Invoke(this, EventArgs.Empty);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _logger.LogDebug("MainViewModel disposed");
        }
        base.Dispose(disposing);
    }
}
