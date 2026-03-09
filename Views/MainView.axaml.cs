using System;
using Avalonia.Controls;
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

    public MainView()
    {
        try
        {
            if (OperatingSystem.IsLinux())
                Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] MainView: Constructor called");

            if (OperatingSystem.IsLinux())
                Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] MainView: Calling InitializeComponent...");

            InitializeComponent();

            if (OperatingSystem.IsLinux())
                Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] MainView: InitializeComponent done");

            // On Linux, delay WebView creation until after CEF is ready
            if (OperatingSystem.IsLinux())
            {
                Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] MainView: Scheduling lazy WebView creation...");
                
                Avalonia.Threading.Dispatcher.UIThread.Post(async () =>
                {
                    Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] MainView: Waiting before creating WebView...");
                    await System.Threading.Tasks.Task.Delay(3000); // Wait 3 seconds after MainView is created
                    
                    if (!_disposed)
                    {
                        Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] MainView: Creating WebView now...");
                        CreateWebView();
                    }
                }, Avalonia.Threading.DispatcherPriority.Background);
            }
            else
            {
                // On Windows/macOS, create immediately
                CreateWebView();
            }

            if (OperatingSystem.IsLinux())
                Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] MainView: Constructor complete");
        }
        catch (Exception ex)
        {
            if (OperatingSystem.IsLinux())
            {
                Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] MainView: FATAL ERROR in constructor: {ex.Message}");
                Console.WriteLine(ex.ToString());
            }
            throw;
        }
    }

    private void CreateWebView()
    {
        if (_webViewCreated || _disposed)
            return;

        try
        {
            if (OperatingSystem.IsLinux())
                Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] MainView: CreateWebView - Starting...");

            _webView = new WebView
            {
                Focusable = true
            };

            // Bind to ViewModel URL
            if (DataContext is MainViewModel vm)
            {
                _webView.Address = vm.URL;
            }

            if (OperatingSystem.IsLinux())
                Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] MainView: CreateWebView - Setting up event handlers...");

            try
            {
                _webViewPropertyChangedHandler = (sender, args) =>
                {
                    try
                    {
                        if (args.Property.Name == nameof(_webView.IsVisible))
                        {
                            if (_webView.IsVisible)
                            {
                                if (DataContext is MainViewModel vm)
                                {
                                    vm.OnWebViewReady();
                                }
                            }
                        }
                        
                        if (args.Property.Name == nameof(_webView.Address))
                        {
                            ValidateWebViewUrl();
                        }
                    }
                    catch
                    {
                        // Ignore
                    }
                };
                
                _webView.PropertyChanged += _webViewPropertyChangedHandler;
            }
            catch (Exception ex)
            {
                if (OperatingSystem.IsLinux())
                {
                    Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] MainView: Error in event setup: {ex.Message}");
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

            if (!Uri.TryCreate(currentUrl, UriKind.Absolute, out var uri))
            {
                _lastValidatedUrl = currentUrl;
                return;
            }

            var allowHttp = BuildConfiguration.WebViewAllowHttp;
            var trustedDomains = BuildConfiguration.WebViewTrustedDomains;

            var isTrusted = false;
            foreach (var domain in trustedDomains)
            {
                if (uri.Host.Equals(domain, StringComparison.OrdinalIgnoreCase) ||
                    uri.Host.EndsWith($".{domain}", StringComparison.OrdinalIgnoreCase))
                {
                    isTrusted = true;
                    break;
                }
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