using Avalonia.Controls;
using Avalonia.Interactivity;
using ECoopSystem.ViewModels;
using System;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;

namespace ECoopSystem.Views;

public partial class ActivationView : UserControl
{
    public ActivationView()
    {
        InitializeComponent();
        
        Unloaded += OnUnloaded;
    }

    private async void OnActivateClicked(object? sender, RoutedEventArgs e)
    {
        if (DataContext is ActivationViewModel vm)
            await vm.ActivateAsync();
    }

    private void OnGoToDashboardClicked(object? sender, RoutedEventArgs e)
    {
        if (DataContext is ActivationViewModel vm)
            vm.GoToDashboard();
    }

    private void OnCloseClicked(object? sender, RoutedEventArgs e)
    {
        (TopLevel.GetTopLevel(this) as Window)?.Close();
    }

    private void OnUnloaded(object? sender, RoutedEventArgs e)
    {
        if (DataContext is ActivationViewModel vm)
            vm.StopTimer();
    }

    private void OnSocialMediaClicked(object? sender, RoutedEventArgs e)
    {
        if (sender is Button button && button.Tag is string url)
        {
            OpenUrl(url);
        }
    }

    private void OnContactSupportClicked(object? sender, RoutedEventArgs e)
    {
        if (sender is Button button && button.Tag is string url)
        {
            OpenUrl(url);
        }
    }

    private static void OpenUrl(string url)
    {
        // Validate URL before opening
        if (string.IsNullOrWhiteSpace(url))
            return;

        // Try to parse as a valid URI
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
            return;

        // Allow only safe URL schemes (prevent javascript:, file:, etc.)
        var allowedSchemes = new[] { "https", "http", "mailto", "tel" };
        if (!allowedSchemes.Contains(uri.Scheme.ToLowerInvariant()))
        {
            Debug.WriteLine($"Blocked unsafe URL scheme: {uri.Scheme}");
            return;
        }

        // Additional validation: Ensure domain is reasonable (optional but recommended)
        if (uri.Scheme is "https" or "http")
        {
            // Prevent localhost/internal network access in production
#if !DEBUG
            if (uri.Host.Equals("localhost", StringComparison.OrdinalIgnoreCase) ||
                uri.Host.StartsWith("127.", StringComparison.OrdinalIgnoreCase) ||
                uri.Host.StartsWith("192.168.", StringComparison.OrdinalIgnoreCase) ||
                uri.Host.StartsWith("10.", StringComparison.OrdinalIgnoreCase))
            {
                Debug.WriteLine($"Blocked internal network URL: {uri.Host}");
                return;
            }
#endif
        }

        try
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                Process.Start("xdg-open", url);
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                Process.Start("open", url);
            }
        }
        catch (Exception ex)
        {
            // Log error for debugging
            Debug.WriteLine($"Failed to open URL: {ex.Message}");
        }
    }
}