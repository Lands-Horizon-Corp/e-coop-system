using Avalonia.Controls;
using Avalonia.Threading;
using ECoopSystem.Build;
using ECoopSystem.ViewModels;
using System;
using System.Threading.Tasks;

namespace ECoopSystem.Views;

public partial class MainView : UserControl
{
    private string? _lastValidatedUrl;

    public MainView()
    {
        InitializeComponent();

        webView.NavigationCompleted += (s, e) =>
        {
            Dispatcher.UIThread.Post(async () => {
                if (DataContext is MainViewModel vm)
                {
                    await Task.Delay(100);
                    vm.OnWebViewReady();
                }
            }, DispatcherPriority.Render);
        };

        webView.NavigationFailed += (s, e) =>
        {
            Dispatcher.UIThread.Post(() => {
                if (DataContext is MainViewModel vm)
                {
                    Console.WriteLine($"Navigation failed: {e}");
                    vm.OnWebViewReady(); // Proceed anyway so the user can see the error page instead of infinitely loading
                }
            }, DispatcherPriority.Render);
        };
    }

    public void ReloadWebView()
    {
        try
        {
            if (webView != null)
            {
                webView.Reload();
            }
        }
        catch
        {
            // Ignore
        }
    }

    private void ValidateWebViewUrl()
    {
        try
        {
            var currentUrl = webView.Url;
            
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
}