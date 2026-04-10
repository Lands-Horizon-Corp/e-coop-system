using System;
using Avalonia.Controls;
using Avalonia.Threading;
using ECoopSystem.Build;
using ECoopSystem.ViewModels;
using WebViewControl;

namespace ECoopSystem.Views;

public partial class MainView : UserControl, IDisposable
{
    private string? _lastValidatedUrl;
    private EventHandler<Avalonia.AvaloniaPropertyChangedEventArgs>? _webViewPropertyChangedHandler;
    private bool _disposed;
    private WebView? _webView;
    private bool _webViewCreated;

    protected override void OnDataContextChanged(EventArgs e)
    {
        base.OnDataContextChanged(e);
        UpdateWebViewAddressFromViewModel();
    }

    public MainView()
    {
        try
        {
            if (OperatingSystem.IsLinux())
                Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] MainView: Constructor called");

            if (OperatingSystem.IsLinux())
                Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] MainView: Calling InitializeComponent...");

            InitializeComponent();

        try
        {
            webView.PropertyChanged += (sender, args) =>
            {
                try
                {
                    if (args.Property.Name == nameof(webView.IsVisible))
                    {
                        if (webView.IsVisible)
                        {
                            if (DataContext is MainViewModel vm)
                            {
                                vm.OnWebViewReady();
                            }
                        }
                    }
                    
                    if (args.Property.Name == nameof(webView.Address))
                    {
                        ValidateWebViewUrl();
                    }
                }
                catch
                {
                    // Ignore
                }
            };
        }
        catch
        {
            // Ignore
        }
    }

            // Add to container
            webViewContainer.Content = _webView;
            _webViewCreated = true;

            if (OperatingSystem.IsLinux())
                Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] MainView: CreateWebView - Complete");
        }
        catch (Exception ex)
        {
            if (OperatingSystem.IsLinux())
            {
                Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] MainView: ERROR creating WebView: {ex.Message}");
                Console.WriteLine(ex.ToString());
            }
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
            var currentUrl = webView.Address;
            
            if (currentUrl == _lastValidatedUrl)
                return;
            
            if (string.IsNullOrWhiteSpace(currentUrl))
                return;

            if (!Uri.TryCreate(currentUrl, UriKind.Absolute, out var uri))
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
                // Navigate to blank page first to release resources
                try
                {
                    _webView.Address = "about:blank";
                }
                catch
                {
                    // Ignore
                }

                // Give WebView time to process the blank navigation
                System.Threading.Thread.Sleep(50);

                // Unsubscribe from events
                if (_webViewPropertyChangedHandler != null)
                {
                    _webView.PropertyChanged -= _webViewPropertyChangedHandler;
                    _webViewPropertyChangedHandler = null;
                }

                // Dispose WebView if it implements IDisposable
                if (_webView is IDisposable disposableWebView)
                {
                    disposableWebView.Dispose();
                }

                _webView = null;

                // Additional cleanup delay for native resources
                System.Threading.Thread.Sleep(50);
            }
        }
        catch
        {
            // Ignore disposal errors
        }
    }
}