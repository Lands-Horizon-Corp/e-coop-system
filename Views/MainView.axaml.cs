using System;
using Avalonia.Controls;
using ECoopSystem.ViewModels;
using WebViewControl;

namespace ECoopSystem.Views;

public partial class MainView : UserControl, IDisposable
{
    private string? _lastValidatedUrl;
    private EventHandler<Avalonia.AvaloniaPropertyChangedEventArgs>? _webViewPropertyChangedHandler;
    private bool _disposed;
    private WebViewControl.WebView? _webView;

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
            if (OperatingSystem.IsLinux())
                Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] MainView: ERROR initializing WebView: {ex.Message}");
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
                if (DataContext is MainViewModel vm)
                    vm.OnWebViewReady();
            }

            if (args.Property.Name == nameof(webView.Address))
                ValidateWebViewUrl();
        }
        catch
        {
            // Ignore
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
        }
    }

    private void ValidateWebViewUrl()
    {
        if (_webView == null)
            return;

        try
        {
            var currentUrl = _webView.Address;

            if (currentUrl == _lastValidatedUrl)
                return;

            if (string.IsNullOrWhiteSpace(currentUrl))
                return;

            if (!Uri.TryCreate(currentUrl, UriKind.Absolute, out _))
            {
                _lastValidatedUrl = currentUrl;
                return;
            }

            _lastValidatedUrl = currentUrl;
        }
        catch
        {
            // Ignore
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
        catch
        {
            // Ignore disposal errors
        }
    }
}
