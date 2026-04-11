using System;
using Avalonia.Controls;
using ECoopSystem.ViewModels;
using System.Threading.Tasks;
using WebViewControl;

namespace ECoopSystem.Views;

public partial class MainView : UserControl, IDisposable
{
    private EventHandler<Avalonia.AvaloniaPropertyChangedEventArgs>? _webViewPropertyChangedHandler;
    private bool _disposed;
    private WebViewControl.WebView? _webView;
    private bool _navigationPrimed;

    private static void Log(string message)
    {
        Console.WriteLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] [MainView] {message}");
    }

    protected override void OnDataContextChanged(EventArgs e)
    {
        base.OnDataContextChanged(e);
        UpdateWebViewAddressFromViewModel();
    }

    public MainView()
    {
        try
        {
            InitializeComponent();

            _webView = webView;
            _webViewPropertyChangedHandler = OnWebViewPropertyChanged;
            _webView.PropertyChanged += _webViewPropertyChangedHandler;

            UpdateWebViewAddressFromViewModel();
        }
        catch (Exception ex)
        {
            Log($"Error initializing WebView: {ex}");
        }
    }

    public void ReloadWebView()
    {
        if (_webView == null)
            return;

        try
        {
            var address = _webView.Address;
            if (!string.IsNullOrWhiteSpace(address))
                _webView.Address = address;
        }
        catch
        {
            // Ignore
        }
    }

    private void OnWebViewPropertyChanged(object? sender, Avalonia.AvaloniaPropertyChangedEventArgs args)
    {
        try
        {
            if (args.Property.Name == nameof(webView.IsVisible) && _webView?.IsVisible == true)
            {
                PrimeNavigation();
                if (DataContext is MainViewModel vm)
                    vm.OnWebViewReady();
            }

            if (args.Property.Name == nameof(webView.Address))
            {
                _navigationPrimed = false;
            }
        }
        catch (Exception ex)
        {
            Log($"Error in webview property handler: {ex}");
        }
    }

    private void UpdateWebViewAddressFromViewModel()
    {
        if (_webView == null)
            return;

        if (DataContext is MainViewModel vm &&
            !string.IsNullOrWhiteSpace(vm.URL) &&
            !string.Equals(_webView.Address, vm.URL, StringComparison.OrdinalIgnoreCase))
        {
            _webView.Address = vm.URL;
            _navigationPrimed = false;
        }
    }

    private async void PrimeNavigation()
    {
        if (_webView == null || _navigationPrimed)
            return;

        _navigationPrimed = true;

        try
        {
            var url = (DataContext as MainViewModel)?.URL;
            if (string.IsNullOrWhiteSpace(url))
                return;

            if (!string.Equals(_webView.Address, url, StringComparison.OrdinalIgnoreCase))
            {
                _webView.Address = url;
            }

            await Task.Delay(300);
        }
        catch (Exception ex)
        {
            Log($"PrimeNavigation error: {ex}");
        }
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;

        try
        {
            if (_webView != null)
            {
                try
                {
                    _webView.Address = "about:blank";
                }
                catch
                {
                    // Ignore
                }

                if (_webViewPropertyChangedHandler != null)
                {
                    _webView.PropertyChanged -= _webViewPropertyChangedHandler;
                    _webViewPropertyChangedHandler = null;
                }

                if (_webView is IDisposable disposableWebView)
                    disposableWebView.Dispose();

                _webView = null;
            }
        }
        catch (Exception ex)
        {
            Log($"Disposal error: {ex}");
        }
    }
}
