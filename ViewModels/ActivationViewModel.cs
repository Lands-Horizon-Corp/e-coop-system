using ECoopSystem.Services;
using ECoopSystem.Stores;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Threading;

namespace ECoopSystem.ViewModels;

public class ActivationViewModel : ViewModelBase
{
    private readonly ShellViewModel _shell;
    private readonly AppStateStore _store;
    private readonly AppState _state;
    private readonly SecretKeyStore _secretStore;
    private readonly LicenseService _licenseService;
    private readonly DispatcherTimer _lockoutTimer;

    private string _licenseKey = "";
    private string? _error;
    private bool _isBusy;
    private bool _isActivationSuccess;

    public ActivationViewModel(
        ShellViewModel shell, 
        AppStateStore store, 
        AppState state,
        SecretKeyStore secretStore,
        LicenseService licenseService)
    {
        _shell = shell;
        _store = store;
        _state = state;
        _secretStore = secretStore;
        _licenseService = licenseService;

        _lockoutTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(1)
        };
        _lockoutTimer.Tick += OnLockoutTimerTick;
        
        if (IsLockedOut())
        {
            _lockoutTimer.Start();
        }
    }

    public string LicenseKey
    {
        get => _licenseKey;
        set { _licenseKey = value; OnPropertyChanged(); }
    }

    public string? Error
    {
        get => _error;
        private set { _error = value; OnPropertyChanged(); }
    }

    public bool IsBusy
    {
        get => _isBusy;
        private set 
        { 
            _isBusy = value; 
            OnPropertyChanged();
            OnPropertyChanged(nameof(CanActivate));
        }
    }

    public bool IsActivationSuccess
    {
        get => _isActivationSuccess;
        private set
        {
            _isActivationSuccess = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(ShowActivationForm));
            OnPropertyChanged(nameof(ShowSuccessScreen));
        }
    }

    public bool ShowActivationForm => !IsActivationSuccess;
    public bool ShowSuccessScreen => IsActivationSuccess;

    public bool CanActivate => !IsBusy && !IsLockedOut();

    public string? LockoutMessage => IsLockedOut()
        ? $"Too many failed attempts. Try again in {GetRemainingLockoutSeconds()}s."
        : null;

    public async Task ActivateAsync()
    {
        Error = null;

        if (IsLockedOut())
        {
            OnPropertyChanged(nameof(LockoutMessage));
            return;
        }

        var key = (LicenseKey ?? "").Trim();
        if (key.Length != Constants.LicenseKeyLength)
        {
            Error = "Invalid License Key";
            return;
        }

        IsBusy = true;
        
        try
        {
            var fingerprint = FingerprintService.ComputeFingerprint(_state);
            var result = await _licenseService.ActivateAsync(key, fingerprint, CancellationToken.None);

            if (result.IsSuccess && !string.IsNullOrWhiteSpace(result.SecretKey))
            {
                _secretStore.Save(result.SecretKey);
                _state.Counter = 1;
                _store.Save(_state);
                
                IsActivationSuccess = true;
                return;
            }

            if (result.IsInvalidKey)
            {
                RegisterFailedAttempt();
                Error = "Invalid license key";
                return;
            }

            Error = "Activation server is unavailable. Please try again later.";
        }
        catch (TaskCanceledException)
        {
            Error = "Request timed out. Check your internet and try again.";
        }
        catch (System.Net.Http.HttpRequestException)
        {
            Error = "Network error occurred. Please check your internet connection and try again.";
        }
        catch (System.Security.Cryptography.CryptographicException)
        {
            Error = "Security error occurred. Please restart the application and try again.";
        }
        catch (System.IO.IOException)
        {
            Error = "Cannot save activation data. Check file permissions and disk space.";
        }
        catch (UnauthorizedAccessException)
        {
            Error = "Access denied. Please run the application with appropriate permissions.";
        }
        catch (System.Text.Json.JsonException)
        {
            Error = "Invalid server response. The activation server may be experiencing issues.";
        }
        catch
        {
            Error = "An unexpected error occurred. Please check your internet connection and try again.";
        }
        finally
        {
            IsBusy = false;
            OnPropertyChanged(nameof(CanActivate));
            OnPropertyChanged(nameof(LockoutMessage));
        }
    }

    private void RegisterFailedAttempt()
    {
        var now = DateTimeOffset.UtcNow;

        _state.FailedActivationsUtc = _state.FailedActivationsUtc
            .Where(t => (now - t) <= TimeSpan.FromMinutes(Constants.ActivationLookbackMinutes))
            .ToList();

        _state.FailedActivationsUtc.Add(now);

        if (_state.FailedActivationsUtc.Count >= Constants.MaxActivationAttempts)
        {
            _state.LockedUntilUtc = now.AddMinutes(Constants.LockoutMinutes);
            _state.FailedActivationsUtc.Clear();
            
            // Start timer when lockout begins
            _lockoutTimer.Start();
        }

        _store.Save(_state);

        OnPropertyChanged(nameof(CanActivate));
        OnPropertyChanged(nameof(LockoutMessage));
    }

    private bool IsLockedOut() => _state.LockedUntilUtc is not null && DateTimeOffset.UtcNow < _state.LockedUntilUtc.Value;

    private int GetRemainingLockoutSeconds()
    {
        if (_state.LockedUntilUtc is null)
            return 0;

        var remaining = _state.LockedUntilUtc.Value - DateTimeOffset.UtcNow;
        return remaining.TotalSeconds <= 0 ? 0 : (int)Math.Ceiling(remaining.TotalSeconds);
    }

    private void OnLockoutTimerTick(object? sender, EventArgs e)
    {
        if (!IsLockedOut())
        {
            _state.LockedUntilUtc = null;
            _store.Save(_state);
            _lockoutTimer?.Stop();
        }

        OnPropertyChanged(nameof(CanActivate));
        OnPropertyChanged(nameof(LockoutMessage));
    }

    public void StopTimer()
    {
        _lockoutTimer?.Stop();
    }

    public async Task GoToDashboard()
    {
        try
        {
            StopTimer();
            
            var mainViewModel = new MainViewModel(
                _shell, 
                _store, 
                _state, 
                _secretStore, 
                _licenseService);
            
            _shell.Navigate(mainViewModel, WindowMode.Normal);
            
            await Task.Delay(100);
            
            await mainViewModel.VerifyLicenseAsync();
        }
        catch
        {
            Error = "Failed to load dashboard. Please restart the application.";
            IsActivationSuccess = false;
        }
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            if (_lockoutTimer != null)
            {
                _lockoutTimer.Stop();
                _lockoutTimer.Tick -= OnLockoutTimerTick;
            }
        }
        base.Dispose(disposing);
    }
}
