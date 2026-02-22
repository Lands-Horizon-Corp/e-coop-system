using Avalonia.Controls;
using ECoopSystem.Build;
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

        // WebView security settings from BuildConfiguration (compiled at build time)
        var allowHttp = BuildConfiguration.WebViewAllowHttp;
        var trustedDomains = BuildConfiguration.WebViewTrustedDomains;

        // Only allow HTTPS (no HTTP in production) unless configured at build time
#if !DEBUG
        if (uri.Scheme != "https" && !allowHttp)
        {
            Debug.WriteLine($"WebView Security Warning: Non-HTTPS URL in production: {uri.Scheme}://{uri.Host}");
        }
#else
        if (uri.Scheme != "https" && !allowHttp)
        {
            Debug.WriteLine($"WebView Warning: Non-HTTPS URL: {uri.Scheme}://{uri.Host}");
        }
#endif

        // Only allow navigation to trusted domains from BuildConfiguration (compiled at build time)
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
            Debug.WriteLine($"  Trusted domains (from build): {string.Join(", ", trustedDomains)}");
        }
        else
        {
            Debug.WriteLine($"WebView: Validated navigation to trusted domain: {uri.Host}");
        }

        _lastValidatedUrl = currentUrl;
    }
}