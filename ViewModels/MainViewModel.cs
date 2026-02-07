using ECoopSystem.Stores;

namespace ECoopSystem.ViewModels;

public class MainViewModel
{
    public readonly ShellViewModel _shell;
    public readonly AppStateStore _store;
    public readonly AppState _state;

    public MainViewModel(ShellViewModel shell, AppStateStore store, AppState state)
    {
        _shell = shell;
        _store = store;
        _state = state;
    }

    public void Logout()
    {
        _shell.Navigate(
            new ActivationViewModel(_shell, _store, _state),
            WindowMode.Locked
        );
    }
}
