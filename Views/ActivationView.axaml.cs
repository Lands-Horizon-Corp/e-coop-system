using Avalonia.Controls;
using Avalonia.Interactivity;
using ECoopSystem.ViewModels;
using System;
using System.Diagnostics;
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
        catch
        {
            // Silently ignore if unable to open URL
        }
    }
}