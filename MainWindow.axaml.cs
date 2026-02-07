using Avalonia.Controls;
using ECoopSystem.ViewModels;
using System.ComponentModel;

namespace ECoopSystem;

public partial class MainWindow : Window
{
    private readonly ShellViewModel _shell;

    public MainWindow()
    {
        InitializeComponent();

        _shell = new ShellViewModel();
        DataContext = _shell;

        _shell.Navigate(new ActivationViewModel(_shell), WindowMode.Locked);

        _shell.PropertyChanged += ShellOnPropertyChanged;

        ApplyWindowMode();
    }

    private void ShellOnPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(ShellViewModel.Mode))
            ApplyWindowMode();
    }

    private void ApplyWindowMode()
    {
        if (_shell.Mode == WindowMode.Locked)
        {
            const int w = 1280;
            const int h = 720;

            Width = w;
            Height = h;

            MinWidth = MaxWidth = w;
            MinHeight = MaxHeight = h;

            CanResize = false;
            SystemDecorations = SystemDecorations.None;
        }
        else
        {
            // Reset size constraints
            const int w = 1280;
            const int h = 720;

            Width = w;
            Height = h;

            MinWidth = 900;
            MinHeight = 600;

            MaxWidth = double.PositiveInfinity;
            MaxHeight = double.PositiveInfinity;

            CanResize = true;
            SystemDecorations = SystemDecorations.Full;
        }
    }
}