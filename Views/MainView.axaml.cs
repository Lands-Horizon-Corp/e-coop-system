using Avalonia.Controls;
using ECoopSystem.Build;
using ECoopSystem.ViewModels;
using System;

namespace ECoopSystem.Views;

public partial class MainView : UserControl
{
    private string? _lastValidatedUrl;

    public MainView()
    {
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