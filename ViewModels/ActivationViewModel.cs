using ECoopSystem.Services;
using ECoopSystem.Stores;
using Microsoft.Extensions.Logging;
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
    private readonly ILogger<ActivationViewModel> _logger;
    private readonly ILoggerFactory _loggerFactory;
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
        LicenseService licenseService,
        ILogger<ActivationViewModel> logger,
        ILoggerFactory loggerFactory)
    {
        _shell = shell;
        _store = store;
        _state = state;
        _secretStore = secretStore;
        _licenseService = licenseService;
        _logger = logger;
        _loggerFactory = loggerFactory;

        // Setup timer to update lockout countdown every second
        _lockoutTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(1)
        };
        _lockoutTimer.Tick += OnLockoutTimerTick;
        
        // Start timer if currently locked out
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
        
        // Add diagnostic logging
        _logger.LogInformation("Starting activation with API URL: {ApiUrl}", ApiService.BaseUrl);
        
        try
        {
            var fingerprint = FingerprintService.ComputeFingerprint(_state);

            _logger.LogInformation("Sending activation request for key: {KeyPrefix}...", key[..4]);
            var result = await _licenseService.ActivateAsync(key, fingerprint, CancellationToken.None);

            if (result.IsSuccess && !string.IsNullOrWhiteSpace(result.SecretKey))
            {
                _secretStore.Save(result.SecretKey);
                _state.Counter = 1;
                _store.Save(_state);
                
                // Show success screen instead of navigating immediately
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
        catch (TaskCanceledException ex)
        {
            _logger.LogWarning(ex, "Activation request timed out");
            Error = "Request timed out. Check your internet and try again.";
        }
        catch (System.Net.Http.HttpRequestException ex)
        {
            // Network/HTTP errors
            _logger.LogError(ex, "HTTP request failed during activation. Inner exception: {InnerException}", 
                ex.InnerException?.GetType().Name ?? "None");
            
            if (ex.InnerException is System.Net.Sockets.SocketException socketEx)
            {
                _logger.LogError("Socket error code: {ErrorCode}, Message: {Message}", 
                    socketEx.SocketErrorCode, socketEx.Message);
                Error = $"Cannot connect to activation server. Check your internet connection. (Error: {socketEx.SocketErrorCode})";
            }
            else if (ex.Message.Contains("SSL") || ex.Message.Contains("certificate"))
            {
                Error = "SSL certificate validation failed. Check your system date/time settings.";
            }
            else
            {
                _logger.LogError("HTTP error: {Message}", ex.Message);
                Error = "Network error occurred. Please check your internet connection and try again.";
            }
        }
        catch (System.Security.Cryptography.CryptographicException ex)
        {
            // Data protection / encryption errors
            _logger.LogError(ex, "Cryptographic error during activation");
            Error = "Security error occurred. Please restart the application and try again.";
        }
        catch (System.IO.IOException ex)
        {
            // File system errors (saving secret key or state)
            _logger.LogError(ex, "File system error during activation");
            Error = "Cannot save activation data. Check file permissions and disk space.";
        }
        catch (UnauthorizedAccessException ex)
        {
            // Permission errors
            _logger.LogError(ex, "Access denied during activation");
            Error = "Access denied. Please run the application with appropriate permissions.";
        }
        catch (System.Text.Json.JsonException ex)
        {
            // JSON parsing errors
            _logger.LogError(ex, "Invalid response format from activation server");
            Error = "Invalid server response. The activation server may be experiencing issues.";
        }
        catch (Exception ex)
        {
            // Log detailed error for debugging with full exception details
            _logger.LogError(ex, "Activation failed unexpectedly. Type: {ExceptionType}, Message: {Message}", 
                ex.GetType().FullName, ex.Message);
            
            // More helpful generic error with exception type hint
#if DEBUG
            Error = $"Error: {ex.GetType().Name} - {ex.Message}";
#else
            Error = "An unexpected error occurred. Please check your internet connection and try again, or contact support if the issue persists.";
#endif
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
            // Lockout expired - clear it and stop timer
            _state.LockedUntilUtc = null;
            _store.Save(_state);
            _lockoutTimer.Stop();
        }

        // Update UI properties
        OnPropertyChanged(nameof(CanActivate));
        OnPropertyChanged(nameof(LockoutMessage));
    }

    public void StopTimer()
    {
        _lockoutTimer?.Stop();
    }

    public void GoToDashboard()
    {
        var mainViewModel = new MainViewModel(
            _shell, 
            _store, 
            _state, 
            _secretStore, 
            _licenseService, 
            _loggerFactory.CreateLogger<MainViewModel>(),
            _loggerFactory);
        _shell.Navigate(mainViewModel, WindowMode.Normal);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _lockoutTimer?.Stop();
            if (_lockoutTimer != null)
            {
                _lockoutTimer.Tick -= OnLockoutTimerTick;
            }
            _logger.LogDebug("ActivationViewModel disposed");
        }
        base.Dispose(disposing);
    }
}
