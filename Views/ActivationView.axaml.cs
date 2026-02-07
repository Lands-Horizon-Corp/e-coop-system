using Avalonia.Controls;
using Avalonia.Interactivity;
using ECoopSystem.ViewModels;

namespace ECoopSystem.Views;

public partial class ActivationView : UserControl
{
    public ActivationView()
    {
        InitializeComponent();
    }

    private async void OnActivateClicked(object? sender, RoutedEventArgs e)
    {
        if (DataContext is ActivationViewModel vm)
            await vm.ActivateAsync();
    }

    private void OnCloseClicked(object? sender, RoutedEventArgs e)
    {
        (TopLevel.GetTopLevel(this) as Window)?.Close();
    }
}