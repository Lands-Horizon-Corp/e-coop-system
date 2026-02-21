using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace ECoopSystem.ViewModels;

public abstract class ViewModelBase : INotifyPropertyChanged, IDisposable
{
    private bool _disposed;

    public event PropertyChangedEventHandler? PropertyChanged;

    protected void OnPropertyChanged([CallerMemberName] string? name = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

    protected virtual void Dispose(bool disposing)
    {
        if (_disposed)
            return;

        if (disposing)
        {
            // Derived classes override this to dispose managed resources
        }

        _disposed = true;
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }
}
