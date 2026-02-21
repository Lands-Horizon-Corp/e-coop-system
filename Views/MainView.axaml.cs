using Avalonia.Controls;
using ECoopSystem.ViewModels;
using System;
using System.ComponentModel;
using System.Diagnostics;

namespace ECoopSystem.Views;

public partial class MainView : UserControl
{
    private string? _lastValidatedUrl;

    public MainView()
    {
        InitializeComponent();
        
        Loaded += async (_, _) =>
        {
            if (DataContext is MainViewModel vm)
            {
                await vm.VerifyLicenseAsync();
            }
        };

        // Monitor WebView visibility - when it becomes visible, it's ready
        webView.PropertyChanged += (sender, args) =>
        {
            if (args.Property.Name == nameof(webView.IsVisible) && webView.IsVisible)
            {
                if (DataContext is MainViewModel vm)
                {
                    vm.OnWebViewReady();
                }
            }
            
            // Monitor Address property changes for security validation
            if (args.Property.Name == nameof(webView.Address))
            {
                ValidateWebViewUrl();
            }
        };
    }

    private void ValidateWebViewUrl()
    {
        var currentUrl = webView.Address;
        
        // Don't validate the same URL multiple times
        if (currentUrl == _lastValidatedUrl)
            return;
        
        if (string.IsNullOrWhiteSpace(currentUrl))
            return;

        // Validate the URL
        if (!Uri.TryCreate(currentUrl, UriKind.Absolute, out var uri))
        {
            Debug.WriteLine($"WebView: Invalid URL format detected: {currentUrl}");
            _lastValidatedUrl = currentUrl;
            return;
        }

        // Only allow HTTPS (no HTTP in production)
#if !DEBUG
        if (uri.Scheme != "https")
        {
            Debug.WriteLine($"WebView Security Warning: Non-HTTPS URL in production: {uri.Scheme}://{uri.Host}");
        }
#endif

        // Only allow navigation to trusted domains
        var trustedDomains = new[]
        {
            "e-coop-client-development.up.railway.app",
            "e-coop-client-production.up.railway.app",
            "e-coop-suite.com"
        };

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

        if (!isTrusted)
        {
            Debug.WriteLine($"WebView Security Warning: Navigation to untrusted domain: {uri.Host}");
        }
        else
        {
            Debug.WriteLine($"WebView: Validated navigation to trusted domain: {uri.Host}");
        }

        _lastValidatedUrl = currentUrl;
    }
}