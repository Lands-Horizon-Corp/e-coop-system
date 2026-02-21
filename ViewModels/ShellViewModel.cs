using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace ECoopSystem.ViewModels;

public class ShellViewModel : ViewModelBase
{
    private ViewModelBase? _current;
    private WindowMode _mode = WindowMode.Locked;

    public ViewModelBase? Current
    {
        get => _current;
        private set { _current = value; OnPropertyChanged(); }
    }

    public WindowMode Mode
    {
        get => _mode;
        private set { _mode = value; OnPropertyChanged(); }
    }

    public bool IsLocked => Mode == WindowMode.Locked;

    public void Navigate(ViewModelBase viewModel, WindowMode mode)
    {
        // Dispose the previous ViewModel before navigating
        var previous = Current;
        
        Current = viewModel;
        Mode = mode;
        OnPropertyChanged(nameof(IsLocked));
        
        // Dispose after navigation to avoid issues
        previous?.Dispose();
    }
}
