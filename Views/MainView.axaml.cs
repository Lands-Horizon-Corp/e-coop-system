using Avalonia.Controls;
using ECoopSystem.ViewModels;
using System;
using System.ComponentModel;

namespace ECoopSystem.Views;

public partial class MainView : UserControl
{
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
        };
    }
}