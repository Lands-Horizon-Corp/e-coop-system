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
        set { _mode = value; OnPropertyChanged(); }
    }

    public bool IsLocked => Mode == WindowMode.Locked;

    public void Navigate(ViewModelBase viewModel, WindowMode mode)
    {
        try
        {
            if (OperatingSystem.IsLinux())
                Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] ShellViewModel: Navigate called for {viewModel.GetType().Name}");

            // Dispose the previous ViewModel before navigating
            var previous = Current;
            
            if (OperatingSystem.IsLinux())
                Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] ShellViewModel: Setting Current property...");

            Current = viewModel;
            
            if (OperatingSystem.IsLinux())
                Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] ShellViewModel: Current property set, triggering view creation...");

            Mode = mode;
            OnPropertyChanged(nameof(IsLocked));
            
            if (OperatingSystem.IsLinux())
                Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] ShellViewModel: Disposing previous ViewModel...");

            // Dispose after navigation to avoid issues
            previous?.Dispose();

            if (OperatingSystem.IsLinux())
                Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] ShellViewModel: Navigate complete");
        }
        catch (Exception ex)
        {
            if (OperatingSystem.IsLinux())
            {
                Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] ShellViewModel: ERROR in Navigate: {ex.Message}");
                Console.WriteLine(ex.ToString());
            }
            throw;
        }
    }
}
