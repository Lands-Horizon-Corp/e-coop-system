using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace ECoopSystem.ViewModels;

public class ShellViewModel : INotifyPropertyChanged
{
    private object? _current;
    private WindowMode _mode = WindowMode.Locked;

    public event PropertyChangedEventHandler? PropertyChanged;

    public object? Current
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

    public void Navigate(object viewModel, WindowMode mode)
    {
        Current = viewModel;
        Mode = mode;
        OnPropertyChanged(nameof(IsLocked));
    }

    private void OnPropertyChanged([CallerMemberName] string? name=null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
