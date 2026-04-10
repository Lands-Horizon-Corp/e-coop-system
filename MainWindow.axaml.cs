using Avalonia.Controls;
using ECoopSystem.ViewModels;
using System;
using System.ComponentModel;
using ECoopSystem.Stores;
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
            if (OperatingSystem.IsLinux())
                Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] MainWindow: Starting initialization...");

            InitializeComponent();

            if (OperatingSystem.IsLinux())
                Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] MainWindow: InitializeComponent done");

            try
            {
                if (OperatingSystem.IsLinux())
                    Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] MainWindow: Getting AppStateStore...");

                _stateStore = App.Services.GetRequiredService<AppStateStore>();
                
                if (OperatingSystem.IsLinux())
                    Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] MainWindow: Loading state...");

                // Load state with error recovery
                try
                {
                    _state = _stateStore.Load();
                    
                    if (OperatingSystem.IsLinux())
                        Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] MainWindow: State loaded, saving...");

                    _stateStore.Save(_state);
                    
                    if (OperatingSystem.IsLinux())
                        Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] MainWindow: State saved");
                }
                catch (Exception ex)
                {
                    if (OperatingSystem.IsLinux())
                    {
                        Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] MainWindow: ERROR loading/saving state: {ex.Message}");
                        Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] MainWindow: Stack trace: {ex.StackTrace}");
                    }

                    System.Diagnostics.Debug.WriteLine($"MainWindow: Failed to load/save state: {ex}");
                    
                    // Try to recover by deleting corrupted data
                    try
                    {
                        if (OperatingSystem.IsLinux())
                            Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] MainWindow: Attempting recovery...");

                        var configDir = System.IO.Path.Combine(
                            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                            "ECoopSystem");
                        var stateFile = System.IO.Path.Combine(configDir, "appstate.dat");
                        if (System.IO.File.Exists(stateFile))
                        {
                            if (OperatingSystem.IsLinux())
                                Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] MainWindow: Deleting corrupted state file...");

                            System.IO.File.Delete(stateFile);
                        }
                    }
                    catch (Exception cleanupEx)
                    {
                        if (OperatingSystem.IsLinux())
                            Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] MainWindow: Cleanup failed: {cleanupEx.Message}");
                    }
                    
                    // Load fresh state
                    if (OperatingSystem.IsLinux())
                        Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] MainWindow: Loading fresh state...");

                    _state = _stateStore.Load();
                    _stateStore.Save(_state);
                }

                if (OperatingSystem.IsLinux())
                    Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] MainWindow: Getting SecretKeyStore...");

                _secretStore = App.Services.GetRequiredService<SecretKeyStore>();
                
                if (OperatingSystem.IsLinux())
                    Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] MainWindow: Getting LicenseService...");

                _licenseService = App.Services.GetRequiredService<LicenseService>();

                if (OperatingSystem.IsLinux())
                    Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] MainWindow: Creating ShellViewModel...");

                _shell = new ShellViewModel();
                DataContext = _shell;

        _shell.PropertyChanged += ShellOnPropertyChanged;
        Closing += OnClosing;
        
        AddHandler(KeyDownEvent, OnKeyDown, Avalonia.Interactivity.RoutingStrategies.Tunnel);
        
        StartKeyboardPolling();
        
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

    private async void OnWebViewReady(object? sender, System.EventArgs e)
    {
        // Maximize window only after WebView is fully loaded
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

    private void StartKeyboardPolling()
    {
        _keyboardPollTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(100)
        };
        _keyboardPollTimer.Tick += OnKeyboardPollTick;
        _keyboardPollTimer.Start();
    }

    private void StopKeyboardPolling()
    {
        if (_keyboardPollTimer != null)
        {
            _keyboardPollTimer.Stop();
            _keyboardPollTimer.Tick -= OnKeyboardPollTick;
            _keyboardPollTimer = null;
        }
    }

    private void OnKeyboardPollTick(object? sender, EventArgs e)
    {
        try
        {
            bool isF5Pressed = IsKeyPressed(Key.F5);

            if (isF5Pressed && !_wasF5Pressed)
            {
                TriggerWebViewReload();
            }

            _wasF5Pressed = isF5Pressed;
        }
        catch
        {
            // Ignore
        }
    }

    private bool IsKeyPressed(Key key)
    {
        if (key != Key.F5)
            return false;
            
        try
        {
            if (OperatingSystem.IsWindows())
            {
                return IsKeyPressedWindows();
            }
            else if (OperatingSystem.IsLinux())
            {
                return IsKeyPressedLinux();
            }
            else if (OperatingSystem.IsMacOS())
            {
                return IsKeyPressedMacOS();
            }
        }
        catch
        {
            // Ignore
        }
        
        return false;
    }
    
    private bool IsKeyPressedWindows()
    {
        try
        {
            short keyState = GetAsyncKeyState(VK_F5);
            return (keyState & 0x8000) != 0;
        }
        catch
        {
            return false;
        }
    }
    
    private bool IsKeyPressedLinux()
    {
        try
        {
            IntPtr display = XOpenDisplay(IntPtr.Zero);
            if (display == IntPtr.Zero)
            {
                return false;
            }
            
            byte[] keys = new byte[32];
            XQueryKeymap(display, keys);
            XCloseDisplay(display);
            
            int byteIndex = X11_F5_KEYCODE / 8;
            int bitIndex = X11_F5_KEYCODE % 8;
            return (keys[byteIndex] & (1 << bitIndex)) != 0;
        }
        catch
        {
            return false;
        }
    }
    
    private bool IsKeyPressedMacOS()
    {
        try
        {
            byte[] keyMap = new byte[16];
            GetKeys(keyMap);
            
            int byteIndex = MAC_F5_KEYCODE / 8;
            int bitIndex = MAC_F5_KEYCODE % 8;
            return (keyMap[byteIndex] & (1 << bitIndex)) != 0;
        }
        catch
        {
            return false;
        }
    }

    private void TriggerWebViewReload()
    {
        if (_shell.Current is MainViewModel)
        {
            var mainView = FindMainView(this);
            if (mainView != null)
            {
                mainView.ReloadWebView();
            }
        }
    }

    private void OnClosing(object? sender, WindowClosingEventArgs e)
    {
        // Unsubscribe from events
        _shell.PropertyChanged -= ShellOnPropertyChanged;
        RemoveHandler(KeyDownEvent, OnKeyDown);
        StopKeyboardPolling();
        
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