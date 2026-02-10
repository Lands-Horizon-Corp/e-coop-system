using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Interactivity;
using ECoopSystem.ViewModels;

namespace ECoopSystem.Views;

public partial class WelcomeView : UserControl
{
    public WelcomeView()
    {
        InitializeComponent();
    }

    private void OnStartClicked(object? sender, RoutedEventArgs e)
    {
        if (DataContext is WelcomeViewModel vm)
            vm.Continue();
    }
}