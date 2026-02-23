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

namespace ECoopSystem;

public partial class MainWindow : Window
{
    [DllImport("user32.dll")]
    private static extern short GetAsyncKeyState(int vKey);
    
    [DllImport("libX11.so.6", EntryPoint = "XOpenDisplay")]
    private static extern IntPtr XOpenDisplay(IntPtr display);
    
    [DllImport("libX11.so.6", EntryPoint = "XCloseDisplay")]
    private static extern int XCloseDisplay(IntPtr display);
    
    [DllImport("libX11.so.6", EntryPoint = "XQueryKeymap")]
    private static extern void XQueryKeymap(IntPtr display, byte[] keys);
    
    [DllImport("/System/Library/Frameworks/Carbon.framework/Carbon")]
    private static extern byte GetKeys(byte[] keyMap);
    
    private const int VK_F5 = 0x74;
    private const int X11_F5_KEYCODE = 71;
    private const int MAC_F5_KEYCODE = 96;
    
    private readonly ShellViewModel _shell;
    private readonly AppStateStore _stateStore;
    private readonly AppState _state;
    private readonly SecretKeyStore _secretStore;
    private readonly LicenseService _licenseService;
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

    private void OnWebViewReady(object? sender, System.EventArgs e)
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