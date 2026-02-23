using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Threading;
using Avalonia.VisualTree;
using ECoopSystem.ViewModels;
using System;
using System.ComponentModel;
using System.Runtime.InteropServices;
using ECoopSystem.Stores;
using ECoopSystem.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace ECoopSystem;

public partial class MainWindow : Window
{
    // P/Invoke for Windows to check keyboard state at OS level
    [DllImport("user32.dll")]
    private static extern short GetAsyncKeyState(int vKey);
    
    // P/Invoke for X11 (Linux) to check keyboard state
    [DllImport("libX11.so.6", EntryPoint = "XOpenDisplay")]
    private static extern IntPtr XOpenDisplay(IntPtr display);
    
    [DllImport("libX11.so.6", EntryPoint = "XCloseDisplay")]
    private static extern int XCloseDisplay(IntPtr display);
    
    [DllImport("libX11.so.6", EntryPoint = "XQueryKeymap")]
    private static extern void XQueryKeymap(IntPtr display, byte[] keys);
    
    // P/Invoke for macOS to check keyboard state
    [DllImport("/System/Library/Frameworks/Carbon.framework/Carbon")]
    private static extern byte GetKeys(byte[] keyMap);
    
    private const int VK_F5 = 0x74; // Virtual key code for F5 (Windows)
    private const int X11_F5_KEYCODE = 71; // F5 keycode for X11 (may vary by keyboard layout)
    private const int MAC_F5_KEYCODE = 96; // F5 keycode for macOS
    
    private readonly ShellViewModel _shell;
    private readonly AppStateStore _stateStore;
    private readonly AppState _state;
    private readonly SecretKeyStore _secretStore;
    private readonly LicenseService _licenseService;
    private readonly ILoggerFactory _loggerFactory;
    private DispatcherTimer? _keyboardPollTimer;
    private bool _wasF5Pressed;

    private sealed record RouteResult(ViewModelBase ViewModel, WindowMode Mode);

    public MainWindow()
    {
        InitializeComponent();

        _stateStore = App.Services.GetRequiredService<AppStateStore>();
        _state = _stateStore.Load();
        _stateStore.Save(_state);

        _secretStore = App.Services.GetRequiredService<SecretKeyStore>();
        _licenseService = App.Services.GetRequiredService<LicenseService>();
        _loggerFactory = App.Services.GetRequiredService<ILoggerFactory>();

        _shell = new ShellViewModel();
        DataContext = _shell;

        _shell.PropertyChanged += ShellOnPropertyChanged;
        Closing += OnClosing;
        
        // Use AddHandler with tunneling to intercept F5 before it reaches WebView
        AddHandler(KeyDownEvent, OnKeyDown, Avalonia.Interactivity.RoutingStrategies.Tunnel);
        
        // Also use a polling timer as fallback for when WebView consumes all keyboard input
        StartKeyboardPolling();
        
        Opened += async (_, _) =>
        {
            var route = DecideInitialRoute();
            _shell.Navigate(route.ViewModel, route.Mode);
            ApplyWindowMode();
            
            // If navigating to MainView, trigger license verification
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

    private void OnKeyDown(object? sender, Avalonia.Input.KeyEventArgs e)
    {
        // Intercept F5 using tunneling route - this catches it BEFORE WebView receives it
        if (e.Key == Avalonia.Input.Key.F5)
        {
            System.Diagnostics.Debug.WriteLine($"F5 detected at Window level (Route: {e.Route}) -> Attempting reload");
            e.Handled = true;
            
            // Navigate through Shell -> MainViewModel -> MainView
            if (_shell.Current is MainViewModel)
            {
                System.Diagnostics.Debug.WriteLine("Current view is MainViewModel, searching for MainView...");
                
                // Find MainView through visual tree
                var mainView = FindMainView(this);
                
                if (mainView != null)
                {
                    System.Diagnostics.Debug.WriteLine("MainView found in visual tree, calling ReloadWebView()");
                    mainView.ReloadWebView();
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("ERROR: MainView not found in visual tree");
                }
            }
            else
            {
                System.Diagnostics.Debug.WriteLine($"Not in MainView context. Current ViewModel: {_shell.Current?.GetType().Name}");
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
        // Poll keyboard state every 100ms to catch F5 even when WebView has focus
        _keyboardPollTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(100)
        };
        _keyboardPollTimer.Tick += OnKeyboardPollTick;
        _keyboardPollTimer.Start();
        
        string platform = "Unknown";
        if (OperatingSystem.IsWindows()) platform = "Windows";
        else if (OperatingSystem.IsLinux()) platform = "Linux";
        else if (OperatingSystem.IsMacOS()) platform = "macOS";
        
        System.Diagnostics.Debug.WriteLine($"Keyboard polling started. Platform: {platform}");
    }

    private void StopKeyboardPolling()
    {
        if (_keyboardPollTimer != null)
        {
            _keyboardPollTimer.Stop();
            _keyboardPollTimer.Tick -= OnKeyboardPollTick;
            _keyboardPollTimer = null;
            System.Diagnostics.Debug.WriteLine("Keyboard polling stopped");
        }
    }

    private void OnKeyboardPollTick(object? sender, EventArgs e)
    {
        try
        {
            // Check if F5 is currently pressed using platform-specific API
            bool isF5Pressed = IsKeyPressed(Key.F5);

            // Detect key press edge (transition from not pressed to pressed)
            if (isF5Pressed && !_wasF5Pressed)
            {
                System.Diagnostics.Debug.WriteLine("F5 detected via keyboard polling -> Attempting reload");
                TriggerWebViewReload();
            }

            _wasF5Pressed = isF5Pressed;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error in keyboard poll: {ex.Message}");
        }
    }

    private bool IsKeyPressed(Key key)
    {
        if (key != Key.F5)
            return false;
            
        try
        {
            // Windows: Use GetAsyncKeyState
            if (OperatingSystem.IsWindows())
            {
                return IsKeyPressedWindows();
            }
            // Linux: Use X11 XQueryKeymap
            else if (OperatingSystem.IsLinux())
            {
                return IsKeyPressedLinux();
            }
            // macOS: Use Carbon GetKeys
            else if (OperatingSystem.IsMacOS())
            {
                return IsKeyPressedMacOS();
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error checking key state: {ex.Message}");
        }
        
        return false;
    }
    
    private bool IsKeyPressedWindows()
    {
        try
        {
            short keyState = GetAsyncKeyState(VK_F5);
            // Check if the high-order bit is set (key is currently pressed)
            bool isPressed = (keyState & 0x8000) != 0;
            
            if (isPressed)
            {
                System.Diagnostics.Debug.WriteLine($"[Windows] GetAsyncKeyState detected F5 pressed (state: 0x{keyState:X4})");
            }
            
            return isPressed;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[Windows] Error: {ex.Message}");
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
                System.Diagnostics.Debug.WriteLine("[Linux] Failed to open X11 display");
                return false;
            }
            
            byte[] keys = new byte[32];
            XQueryKeymap(display, keys);
            XCloseDisplay(display);
            
            // Check if F5 is pressed (keycode 71, byte index = 71/8, bit = 71%8)
            int byteIndex = X11_F5_KEYCODE / 8;
            int bitIndex = X11_F5_KEYCODE % 8;
            bool isPressed = (keys[byteIndex] & (1 << bitIndex)) != 0;
            
            if (isPressed)
            {
                System.Diagnostics.Debug.WriteLine($"[Linux] XQueryKeymap detected F5 pressed");
            }
            
            return isPressed;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[Linux] Error: {ex.Message}");
            return false;
        }
    }
    
    private bool IsKeyPressedMacOS()
    {
        try
        {
            byte[] keyMap = new byte[16];
            GetKeys(keyMap);
            
            // Check if F5 is pressed (keycode 96, byte index = 96/8, bit = 96%8)
            int byteIndex = MAC_F5_KEYCODE / 8;
            int bitIndex = MAC_F5_KEYCODE % 8;
            bool isPressed = (keyMap[byteIndex] & (1 << bitIndex)) != 0;
            
            if (isPressed)
            {
                System.Diagnostics.Debug.WriteLine($"[macOS] GetKeys detected F5 pressed");
            }
            
            return isPressed;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[macOS] Error: {ex.Message}");
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
                System.Diagnostics.Debug.WriteLine("Triggering WebView reload from keyboard poll");
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
                    _licenseService,
                    _loggerFactory.CreateLogger<ActivationViewModel>(),
                    _loggerFactory),
                WindowMode.Locked);
        }

        // Has secret - go to MainView with loading overlay
        return new RouteResult(
            new MainViewModel(
                _shell, 
                _stateStore, 
                _state, 
                _secretStore, 
                _licenseService,
                _loggerFactory.CreateLogger<MainViewModel>(),
                _loggerFactory),
            WindowMode.Normal);
    }
}