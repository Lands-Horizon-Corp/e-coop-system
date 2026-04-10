using System;
using Avalonia.Controls;
using ECoopSystem.ViewModels;
using System.Threading.Tasks;
using WebViewControl;

namespace ECoopSystem.Views;

public partial class MainView : UserControl, IDisposable
{
    private string? _lastValidatedUrl;
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
            Log("InitializeComponent start.");
            InitializeComponent();
            Log("InitializeComponent done.");

            _webView = webView;
            _webViewPropertyChangedHandler = OnWebViewPropertyChanged;
            _webView.PropertyChanged += _webViewPropertyChangedHandler;
            _webView.BeforeNavigate += e => Log($"BeforeNavigate: {DescribeEvent(e)}");
            _webView.BeforeResourceLoad += e => Log($"BeforeResourceLoad: {DescribeEvent(e)}");
            WebViewControl.WebView.GlobalWebViewInitialized += wv => Log($"GlobalWebViewInitialized: {wv.GetType().Name}");
            Log("WebView instance resolved and property listener attached.");

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
                Log("WebView became visible.");
                PrimeNavigation();
                if (DataContext is MainViewModel vm)
                    vm.OnWebViewReady();
            }

            if (args.Property.Name == nameof(webView.Address))
            {
                Log($"WebView address changed to: {_webView?.Address}");
                ValidateWebViewUrl();
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
            Log($"Setting WebView address from view model: {vm.URL}");
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
                Log($"PrimeNavigation: assigning URL {url}");
                _webView.Address = url;
            }

            await Task.Delay(300);

            if (string.Equals(_webView.Address, url, StringComparison.OrdinalIgnoreCase))
            {
                Log("PrimeNavigation: forcing reload after visible state.");
                _webView.Address = "about:blank";
                await Task.Delay(150);
                _webView.Address = url;
            }
        }
        catch (Exception ex)
        {
            Log($"PrimeNavigation error: {ex}");
        }
    }

    private static string DescribeEvent(object? eventArgs)
    {
        if (eventArgs == null)
            return "(null)";

        try
        {
            var type = eventArgs.GetType();
            var urlProperty = type.GetProperty("Url") ?? type.GetProperty("Address") ?? type.GetProperty("Uri");
            var value = urlProperty?.GetValue(eventArgs)?.ToString();
            return string.IsNullOrWhiteSpace(value) ? type.Name : $"{type.Name} url={value}";
        }
        catch
        {
            return eventArgs.GetType().Name;
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
                Log($"Invalid URL detected in WebView: {currentUrl}");
                _lastValidatedUrl = currentUrl;
                return;
            }

            Log($"Validated WebView URL: {currentUrl}");
            _lastValidatedUrl = currentUrl;
        }
        catch (Exception ex)
        {
            Log($"URL validation error: {ex}");
        }
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;
        Log("Disposing MainView and WebView resources.");

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
