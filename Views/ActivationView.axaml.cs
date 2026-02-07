using Avalonia.Controls;
using Avalonia.Interactivity;

namespace ECoopSystem.Views;

public partial class ActivationView : UserControl
{
    public ActivationView()
    {
        InitializeComponent();
    }

    private void OnNextClicked(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
    }

    private void OnCloseClicked(object? sender, RoutedEventArgs e)
    {
        (TopLevel.GetTopLevel(this) as Window)?.Close();
    }
}